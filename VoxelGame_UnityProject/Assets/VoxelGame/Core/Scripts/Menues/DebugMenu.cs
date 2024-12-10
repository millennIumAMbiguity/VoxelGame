using UnityEngine;

using Tayx.Graphy;

namespace VoxelGame.Core
{
    public class DebugMenu : MonoBehaviour
    {
        public void ShowGraphy(bool isEnable)
        {
            if (GraphyManager.Instance == null)
                return;

            if (isEnable)
                GraphyManager.Instance.Enable();
            else
                GraphyManager.Instance.Disable();
        }
    }
}