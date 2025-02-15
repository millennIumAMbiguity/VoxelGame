using UnityEngine;
using Zenject;

namespace VoxelGame.Input.MobileInput
{
    public class MobileInput : MonoBehaviour
    {
        [SerializeField] private GameObject mobileInputPanel;
        [SerializeField] private bool showOnlyOnAndroid;

        [Inject]
        private readonly Inputs inputs;

        public void Start()
        {
            if (showOnlyOnAndroid)
            {
                mobileInputPanel.SetActive(Application.platform == RuntimePlatform.Android);
            }
        }

        public void OnMove(Vector2 move)
        {
            inputs.PlayerInputs.Move.SetValue(Vector2.ClampMagnitude(move, 1f));
        }

        public void OnLook(Vector2 move)
        {
            inputs.PlayerInputs.Look.SetValue(Vector2.ClampMagnitude(move, 100f));
        }

        public void OnJump(bool jump)
        {
            inputs.PlayerInputs.Jump.SetValue(jump ? 1f : 0);
        }

        public void OnRun(bool jump)
        {
            inputs.PlayerInputs.Run.SetValue(jump ? 1f : 0);
        }

        public void OnFire(bool fire)
        {
            inputs.PlayerInputs.Fire.SetValue(fire ? 1f : 0);
        }

        public void OnFireAlt(bool fire)
        {
            inputs.PlayerInputs.FireAlt.SetValue(fire ? 1f : 0);
        }

        public void SetTouchpadPointer(bool isEnabled)
        {
            inputs.IsCurrentPointerTouchpad = isEnabled;
        }
    }
}