using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelGame.Core;
using VoxelGame.Voxel;
using Zenject;

namespace VoxelGame.Player
{
    public class PlayerAction : MonoBehaviour
    {
        
        const int LAYER = 1 << 9;
        
        [SerializeField] private int voxel;

        [Inject]
        private readonly InventoryMenu inventoryMenu;

        private Transform cam;
        private HandController hand;

        [Inject]
        private readonly ChunkSystem chunkSystem;

        private void Start()
        {
            inventoryMenu.OnItemSelect.AddListener(SetVoxel);
        }

        public void SetCamera(Transform cam)
        {
            this.cam = cam;
        }

        public void SetHand(HandController hand)
        {
            this.hand = hand;
        }

        public void SetVoxel(int voxel)
        {
            this.voxel = voxel;
        }

        public void Dig()
        {
            hand.Use();
            if (VoxelRaycast.Raycast(cam.position, cam.forward, 10f, out var pos, true, LAYER))
            {
                SoundManager.Instance.PlaySound("block_dig", pos);
                chunkSystem.SetVoxel(pos, Voxels.AIR, true, true, true);
            }
        }

        public void Build()
        {
            hand.Use();
            if (VoxelRaycast.Raycast(cam.position, cam.forward, 10f, out var pos, false, LAYER))
            {
                if (IsVoxelInPlayer(pos))
                    return;

                SoundManager.Instance.PlaySound("block_build", pos);
                chunkSystem.SetVoxel(pos, (byte)voxel, true, true, true);
            }
        }

        private bool IsVoxelInPlayer(Vector3Int pos)
        {
            Vector3 center = pos + Vector3.one * 0.5f;
            return Vector3.Distance(center, transform.position) < 1.2f;
        }
    }
}
