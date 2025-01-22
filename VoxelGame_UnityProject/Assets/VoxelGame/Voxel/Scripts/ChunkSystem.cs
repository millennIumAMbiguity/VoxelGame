using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

using VoxelGame.Core;
using UnityEngine.Events;

namespace VoxelGame.Voxel
{
    public class ChunkSystem : MonoBehaviour
    {
        public UnityEvent<Vector3Int, byte> OnVoxelDestroyed = new();

        [SerializeField] [Range(0.25f, 4f)] private float addChunksDelay = 2f;
        [SerializeField] [Range(1, 8)] private int maxLoadedChunksPerUpdate = 2;
        [SerializeField] [Range(1, 8)] private int maxUpdatedChunksPerUpdate = 3;

        private const int SaveAfterFrames = 120;
        private const int MaxSavedChunksPerFrame = 4;

        private Transform cam;

        private readonly Dictionary<Vector2Int, Chunk> chunks = new();
        private Queue<Vector2Int> loadQueue;
        private Queue<Vector2Int> updateQueue;
        private List<Vector2Int> updatingChunks;

        private float timerAddChunks = 3f;


        private VoxelSettingsSO voxelSettings;
        private ISaveLoadChunk saveLoad;
        private Chunk.Factory chunkFactory;

        [Inject]
        private void Contstruct(
            VoxelSettingsSO voxelSettings,
            ISaveLoadChunk saveLoad,
            Chunk.Factory chunkFactory)
        {
            this.voxelSettings = voxelSettings;
            this.saveLoad = saveLoad;
            this.chunkFactory = chunkFactory;
        }

        private void Start()
        {
            loadQueue = new Queue<Vector2Int>();
            updateQueue = new Queue<Vector2Int>();
            updatingChunks = new List<Vector2Int>();

            if (cam == null)
            {
                cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
            }
        }
        
        private void Update()
        {
            AddChunkControl(addChunksDelay, voxelSettings.RenderDistance);
            AutoSaveControl(SaveAfterFrames, MaxSavedChunksPerFrame);

            if (Time.frameCount % 6 == 0)
            {
                LoadChunkControl(maxLoadedChunksPerUpdate);
                SortChunkUpdateQueue();
            }
            else
            {
                UpdateChunkControl(maxUpdatedChunksPerUpdate);
            }
        }

        #region Chunk control
        private void AddChunkControl(float delaySeconds = 2f, int chunkDistance = 12)
        {
            timerAddChunks += Time.deltaTime;
            if (timerAddChunks > delaySeconds)
            {
                timerAddChunks = 0f;
                Vector2Int playerChunk = VoxelUtils.GetChunkCoords(Vector3Int.FloorToInt(cam.position));
                AddChunks(playerChunk, chunkDistance, voxelSettings.GenerateOnlyInFrustum, cam.forward);
                DeleteChunks(chunkDistance + 2);
            }
        }

        private void AutoSaveControl(int frameCountDelay = 60, int maxSavedChunks = 4)
        {
            if (Time.frameCount % frameCountDelay != 0)
                return;

            int savedChunks = 0;

            foreach (var chunk in chunks.Values)
            {
                if (chunk != null && chunk.IsLoaded)
                {
                    if (chunk.Save())
                    {
                        savedChunks++;
                    }

                    if (savedChunks >= maxSavedChunks)
                        break;
                }
            }

            saveLoad.Save(2);
        }

        private void LoadChunkControl(int maxLoadedChunk = 2)
        {
            Vector2Int playerChunk = VoxelUtils.GetChunkCoords(Vector3Int.FloorToInt(cam.position));
            loadQueue = new Queue<Vector2Int>(loadQueue.OrderBy(c => (c - playerChunk).sqrMagnitude));

            int counter = 0;
            while (loadQueue.Count > 0 && counter < maxLoadedChunk)
            {
                var coords = loadQueue.Dequeue();
                if (chunks.TryGetValue(coords, out var chunk))
                {
                    if (chunk == null)
                        continue;

                    if (chunk.IsLoaded)
                        continue;

                    chunk.Load((c) => AddChunkToUpdateQueue(c));

                    counter++;
                }
            }
        }

        private void UpdateChunkControl(int maxCount = 4)
        {
            List<Vector2Int> chunksList = new List<Vector2Int>();
            List<Vector2Int> chunksReturnedList = new List<Vector2Int>();

            int counter = 0;
            int tries = 32;
            while (updateQueue.Count > 0 && counter < maxCount && tries > 0)
            {
                var coords = updateQueue.Dequeue();
                if (chunks.TryGetValue(coords, out var chunk))
                {
                    if (chunk == null)
                        continue;

                    if (chunk.IsLoaded &&
                        CanUpdateChunk(coords) &&
                        !chunksList.Contains(coords) &&
                        !updatingChunks.Contains(coords))
                    {
                        chunksList.Add(coords);
                        counter++;
                    }
                    else
                    {
                        chunksReturnedList.Add(coords);
                    }

                    tries--;
                }
            }

            foreach (var coords in chunksList)
            {
                chunks[coords].CalculateLight();
            }

            foreach (var coords in chunksList)
            {
                updatingChunks.Add(coords);
                chunks[coords].Update((c) => RemoveFromUpdatingList(c));
            }

            foreach (var coords in chunksReturnedList)
            {
                updateQueue.Enqueue(coords);
            }
        }
        #endregion

        #region Chunk add, load, update, delete
        private void AddChunks(Vector2Int center, int size, bool addInFrustumOnly, Vector3 frustumDir)
        {
            for (int x = -size; x <= size; x++)
                for (int z = -size; z <= size; z++)
                {
                    Vector2Int offset = new Vector2Int(x, z);

                    float dot = Vector2.Dot(
                        new Vector2(frustumDir.x, frustumDir.z),
                        ((Vector2)offset).normalized
                        );

                    float dotSide = Vector2.Dot(
                        new Vector2(frustumDir.z, frustumDir.x),
                        ((Vector2)offset).normalized
                        );

                    if (addInFrustumOnly)
                    {
                        if ((offset.magnitude <= size / 4 + 1) || (dot > -0.25f && offset.magnitude <= size && Mathf.Abs(dotSide) < Mathf.Abs(dot) + 0.5f))
                        {
                            AddChunk(center + offset);
                        }
                    }
                    else
                    {
                        if (offset.magnitude <= size)
                        {
                            AddChunk(center + offset);
                        }
                    }
                }
        }

        private void AddChunk(Vector2Int coords)
        {
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector2Int currentCoords = new Vector2Int(x, z) + coords;

                    if (!chunks.ContainsKey(currentCoords))
                    {
                        var chunk = chunkFactory.Create(new(currentCoords));
                        chunks.Add(currentCoords, chunk);

                        if (loadQueue.Contains(currentCoords))
                            continue;

                        if (updatingChunks.Contains(currentCoords))
                            continue;

                        loadQueue.Enqueue(currentCoords);
                    }
                }
        }

        private void DeleteChunks(float chunkDistance)
        {
            List<Vector2Int> chunksToDelete = new List<Vector2Int>();

            foreach (var coords in chunks.Keys)
            {
                Vector2Int playerChunk = VoxelUtils.GetChunkCoords(Vector3Int.FloorToInt(cam.position));

                if (updatingChunks.Contains(coords))
                    continue;

                if (Vector2Int.Distance(playerChunk, coords) > chunkDistance)
                    chunksToDelete.Add(coords);
            }

            foreach (var coords in chunksToDelete)
            {
                chunks[coords].Delete();
                chunks[coords] = null;
                chunks.Remove(coords);
            }
        }

        private void AddChunkToUpdateQueue(Vector2Int coords)
        {
            if (updatingChunks.Contains(coords))
                return;

            if (!updateQueue.Contains(coords))
                updateQueue.Enqueue(coords);
        }

        private void SortChunkUpdateQueue()
        {
            Vector2Int playerChunk = VoxelUtils.GetChunkCoords(Vector3Int.FloorToInt(cam.position));
            updateQueue = new Queue<Vector2Int>(updateQueue.OrderBy(c => (c - playerChunk).sqrMagnitude));
        }

        private void RemoveFromUpdatingList(Vector2Int coords)
        {
            if (updatingChunks.Contains(coords))
                updatingChunks.Remove(coords);
        }

        private bool CanUpdateChunk(Vector2Int coords)
        {
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector2Int c = new Vector2Int(x, z) + coords;

                    if (!chunks.ContainsKey(c))
                        return false;

                    if (!chunks[c].IsLoaded)
                        return false;
                }

            return true;
        }

        public Chunk GetChunk(Vector2Int coords)
        {
            if (!chunks.ContainsKey(coords))
                return null;

            if (!chunks[coords].IsLoaded)
                return null;

            return chunks[coords];
        }
        #endregion

        #region Chunk voxel operations
        public byte GetVoxel(Vector3Int worldPos)
        {
            Vector2Int coords = VoxelUtils.GetChunkCoords(worldPos);

            if (chunks.TryGetValue(coords, out var chunk))
            {
                if (chunk != null)
                {
                    Vector3Int localPos = VoxelUtils.GetLocalPos(worldPos, coords);
                    if (VoxelUtils.IsVoxelInChunk(localPos))
                    {
                        return chunk.GetVoxel(localPos);
                    }
                }
            }

            return 0;
        }

        public void SetVoxel(Vector3Int worldPos, byte voxel, bool updateNearChunks, bool setSaveFlag, bool destroyDecor = false)
        {
            Vector2Int coords = VoxelUtils.GetChunkCoords(worldPos);

            if (chunks.TryGetValue(coords, out var chunk))
            {
                if (chunk == null)
                    return;

                if (updatingChunks.Contains(coords))
                    return;

                Vector3Int localPos = VoxelUtils.GetLocalPos(worldPos, coords);

                byte changedVoxel = GetVoxel(worldPos);

                if (chunk.SetVoxel(localPos, voxel, true))
                {
                    if (changedVoxel != Voxels.AIR)    
                        OnVoxelDestroyed.Invoke(worldPos, changedVoxel);

                    if (destroyDecor && voxel == Voxels.AIR)
                    {
                        changedVoxel = GetVoxel(worldPos + Vector3Int.up);

                        if (Voxels.voxels[changedVoxel].isDecor)
                        {
                            chunk.SetVoxel(localPos + Vector3Int.up, voxel);

                            OnVoxelDestroyed.Invoke(worldPos + Vector3Int.up, changedVoxel);
                        }
                    }

                    AddChunkToUpdateQueue(coords);

                    if (updateNearChunks)
                    {
                        if (localPos.x == 0)
                            AddChunkToUpdateQueue(coords - Vector2Int.right);
                        else if (localPos.x == VoxelData.chunkWidth - 1)
                            AddChunkToUpdateQueue(coords + Vector2Int.right);

                        if (localPos.z == 0)
                            AddChunkToUpdateQueue(coords - Vector2Int.up);
                        else if (localPos.z == VoxelData.chunkWidth - 1)
                            AddChunkToUpdateQueue(coords + Vector2Int.up);
                    }
                }
            }
        }

        public void SetVoxelsRadius(Vector3Int pos, byte voxel, int radius)
        {
            for (int x = pos.x - radius; x <= pos.x + radius; x++)
                for (int y = pos.y - radius; y <= pos.y + radius; y++)
                    for (int z = pos.z - radius; z <= pos.z + radius; z++)
                    {
                        Vector3Int p = new Vector3Int(x, y, z);

                        if (Vector3Int.Distance(pos, p) <= radius)
                        {
                            SetVoxel(p, voxel, true, true, destroyDecor: true);

                        }
                    }

            SortChunkUpdateQueue();
        }

        public byte GetLight(Vector3Int worldPos)
        {
            Vector2Int coords = VoxelUtils.GetChunkCoords(worldPos);

            if (chunks.TryGetValue(coords, out var chunk))
            {
                if (chunk != null)
                {
                    Vector3Int localPos = VoxelUtils.GetLocalPos(worldPos, coords);
                    if (VoxelUtils.IsVoxelInChunk(localPos))
                    {
                        return chunk.GetLight(localPos);
                    }
                }
            }

            return byte.MaxValue;
        }
        #endregion
    }
}