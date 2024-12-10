using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OdinSerializer;
using VoxelGame.Utilities;

namespace VoxelGame.Voxel
{
    public class SaveLoadChunkJSON : ISaveLoadChunk
    {
        private readonly string pathWorld = "world1";
        private readonly string pathChunkPrefix = "chunk_";
        private readonly string pathEnd = ".txt";

        private readonly Queue<SavedChunkData> dataQueue = new Queue<SavedChunkData>();

        private SaveLoadChunkJSON()
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, pathWorld));
        }

        public void Save(int maxSavedChunks = 1)
        {
            for (int count = 0; count < maxSavedChunks && dataQueue.Count > 0; count++)
            {
                SavedChunkData data = dataQueue.Dequeue();
                SaveChunk(data);
            }
        }

        public void AddToSaveQueue(SavedChunkData data)
        {
            dataQueue.Enqueue(data);
        }

        public SavedChunkData Load(Vector2Int coords)
        {
            string destination = GetDestination(coords);

            if (File.Exists(destination))
            {
                byte[] bytes = File.ReadAllBytes(destination);
                SavedChunkData loadedData = OdinSerializer.SerializationUtility.DeserializeValue<SavedChunkData>(bytes, DataFormat.JSON);
                loadedData.map = ByteArrayCompressor.Decompress(loadedData.map);
                return loadedData;
            }

            return null;
        }

        private void SaveChunk(SavedChunkData data)
        {
            string destination = GetDestination(new Vector2Int(data.x, data.z));
            data.map = ByteArrayCompressor.Compress(data.map);
            byte[] bytes = OdinSerializer.SerializationUtility.SerializeValue(data, DataFormat.JSON);
            File.WriteAllBytes(destination, bytes);
        }

        private string GetDestination(Vector2Int coords)
        {
            return Path.Combine(Application.persistentDataPath, pathWorld, pathChunkPrefix + coords.ToString() + pathEnd);
        }

        public void DeleteAll()
        {
            string path = Path.Combine(Application.persistentDataPath, pathWorld);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}