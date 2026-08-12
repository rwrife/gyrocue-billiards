using UnityEngine;

namespace GyroCue.Core
{
    /// <summary>
    /// Early runtime defaults for a mobile-first table session.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField]
        private bool useDualPhoneCue = true;

        public bool UseDualPhoneCue => useDualPhoneCue;

        private void Awake()
        {
            // Mobile baseline: prioritize smooth simulation over variable VSync.
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }
    }
}
