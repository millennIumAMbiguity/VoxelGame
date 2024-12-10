using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OdinSerializer;
using VoxelGame.Utilities;

namespace VoxelGame.Voxel
{
    public class SaveLoadChunkBinary : ISaveLoadChunk
    {
        private readonly string pathWorld = "world";
        private readonly string pathChunkPrefix = "chunk_";
        private readonly string pathEnd = ".dat";

        private readonly Queue<SavedChunkData> dataQueue = new Queue<SavedChunkData>();

        private SaveLoadChunkBinary()
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
                byte[] bytes = ByteArrayCompressor.Decompress(File.ReadAllBytes(destination));
                SavedChunkData loadedData = SerializationUtility.DeserializeValue<SavedChunkData>(bytes, DataFormat.Binary);
                return loadedData;
            }

            return null;
        }

        private void SaveChunk(SavedChunkData data)
        {
            string destination = GetDestination(new Vector2Int(data.x, data.z));
            byte[] bytes = ByteArrayCompressor.Compress(SerializationUtility.SerializeValue(data, DataFormat.Binary));
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