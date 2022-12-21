using UnityEngine;

namespace Illumetry.Unity
{
    public class RequiredSettingsApplyer : MonoBehaviour
    {
        [SerializeField] private bool _disableApplyingSettings = false;
        [SerializeField] private int _targetFrameRate = -1;
        [SerializeField] private int _vSyncCount = 1;
        [SerializeField] private int _maxQueuedFrames = 1;

        private void OnEnable()
        {
            if (_disableApplyingSettings)
            {
                if (Application.isEditor || Debug.isDebugBuild)
                {
                    ShowUsingSettings("Required settings applyer was ignored by component settings. Using");
                }

                return;
            }

            Application.targetFrameRate = _targetFrameRate;
            QualitySettings.vSyncCount = _vSyncCount;
            QualitySettings.maxQueuedFrames = _maxQueuedFrames;

            if (Application.isEditor || Debug.isDebugBuild)
            {
                ShowUsingSettings("Install settings by component RequiredSettingsApplyer (OnEnable)");
            }
        }

        private void ShowUsingSettings(string message)
        {
            if (Application.isEditor || Debug.isDebugBuild)
            {
                Debug.Log($"{message}:" +
                        $" Application.targetFrameRate = {Application.targetFrameRate}," +
                        $" QualitySettings.vSyncCount = {QualitySettings.vSyncCount}, " +
                        $" QualitySettings.maxQueuedFrames = {QualitySettings.maxQueuedFrames}");
            }
        }    
    }
}
