using System.Collections.Generic;
using UnityEngine;

namespace VoxelGame.Voxel
{
    public interface IGenerator
    {
        public void GenerateMap(Vector3Int pos, byte[,,] map);
    }
}
