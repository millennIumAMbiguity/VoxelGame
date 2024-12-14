using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Lean.Gui;
using Zenject;

using VoxelGame.Voxel;

namespace VoxelGame.Core
{
    public class SettingsMenu : MonoBehaviour
    {
        public UnityEvent OnOpened = new();
        public UnityEvent OnClosed = new();

        [SerializeField] private GameObject ui;

        [Inject] private readonly VoxelSettingsSO voxelSettings;
        [Inject] private readonly ISaveLoadChunk saveLoad;

        public Slider sliderRenderDistance;
        public Slider sliderRenderScale;
        public LeanToggle toggleFps30;
        public LeanToggle toggleFps60;
        public LeanToggle toggleFps120;
        public LeanButton reset;

        public void Open(bool isOpen)
        {
            ui.SetActive(isOpen);

            if (isOpen)
                OnOpened?.Invoke();
            else
                OnClosed?.Invoke();

            UpdateButtons();
        }

        public void SetRenderDistance(float distance)
        {
            voxelSettings.RenderDistance = Mathf.Clamp(Mathf.FloorToInt(distance), 6, 18);
        }

        public void SetTargetFrameRate(int targetFrameRate)
        {
            voxelSettings.TargetFrameRate = targetFrameRate;
        }

        public void SetRenderScale(float renderScale)
        {
            voxelSettings.RenderScale = Mathf.Clamp(renderScale, 0.3f, 1f);
        }

        private void UpdateButtons()
        {
            sliderRenderDistance.value = voxelSettings.RenderDistance;
            sliderRenderScale.value = voxelSettings.RenderScale;

            toggleFps30.Set(voxelSettings.TargetFrameRate == 30);
            toggleFps60.Set(voxelSettings.TargetFrameRate == 60);
            toggleFps120.Set(voxelSettings.TargetFrameRate == 120);
        }

        public void Reset()
        {
            saveLoad.DeleteAll();
            voxelSettings.Reset();
            voxelSettings.Load();
            UpdateButtons();
        }
    }
}