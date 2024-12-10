using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

namespace VoxelGame.Voxel
{
    [CreateAssetMenu(fileName = "Voxel Settings", menuName = "Voxel/Voxel Settings Scriptable Object")]
    public class VoxelSettingsSO : ScriptableObject
    {
        [Header("Main")]
        public bool generateOnlyInFrustum;
        public int seed;
        public int targetFrameRate;
        public int renderDistance;
        public float renderScale;
        public Material chunksMaterial;
        public Material chunksTranspMaterial;
        public UniversalRenderPipelineAsset urpAsset;

        [Header("Voxels and Structures")]
        public VoxelStructureSO[] structuresSO;
        public VoxelsPresetSO voxelPresetSO;
        public VoxelStructures structures;

        public UnityEvent OnSettingsLoaded = new();

        public enum GeneratorType
        {
            DEFAULT,
            FLAT,
            GENERATOR
        }

        public void Init()
        {
            Load();

            structures = new VoxelStructures();
            foreach (var structure in structuresSO)
                structures.RegistryStructure(structure);

            Voxels.Init(voxelPresetSO.Voxels);

            if (seed == -1)
                seed = Random.Range(0, 1000000);

            UpdateRenderSettings();
        }

        public void UpdateRenderSettings()
        {
            RenderSettings.fogStartDistance = (renderDistance - 2) * VoxelData.chunkWidth;
            RenderSettings.fogEndDistance = RenderSettings.fogStartDistance + VoxelData.chunkWidth * 2f;

            if (Application.platform == RuntimePlatform.Android)
            {
                Application.targetFrameRate = targetFrameRate;
            }
            else
            {
                Application.targetFrameRate = -1;
            }

            urpAsset.renderScale = renderScale;

            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetInt("voxelSeed", seed);
            PlayerPrefs.SetInt("voxelRenderDistance", renderDistance);
            PlayerPrefs.SetInt("voxelTargetFrameRate", targetFrameRate);
            PlayerPrefs.SetFloat("voxelRenderScale", renderScale);

            PlayerPrefs.Save();
        }

        public void Load()
        {
            seed = PlayerPrefs.GetInt("voxelSeed", -1);
            renderDistance = PlayerPrefs.GetInt("voxelRenderDistance", 12);
            targetFrameRate = PlayerPrefs.GetInt("voxelTargetFrameRate", 60);
            renderScale = PlayerPrefs.GetFloat("voxelRenderScale", 1f);
        }

        public void Reset()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
