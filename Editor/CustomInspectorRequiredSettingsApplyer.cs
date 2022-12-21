using Illumetry.Unity;
using UnityEditor;

[CustomEditor(typeof(RequiredSettingsApplyer))]
public class CustomInspectorRequiredSettingsApplyer : Editor
{
    SerializedProperty _disableApplyingSettings;
    SerializedProperty _targetFrameRate;
    SerializedProperty _vSyncCount;
    SerializedProperty _maxQueuedFrames;

    private void OnEnable()
    {
        _disableApplyingSettings = serializedObject.FindProperty("_disableApplyingSettings");
        _targetFrameRate = serializedObject.FindProperty("_targetFrameRate");
        _vSyncCount = serializedObject.FindProperty("_vSyncCount");
        _maxQueuedFrames = serializedObject.FindProperty("_maxQueuedFrames");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("This component,will installing quality settings!\nDefault values:\nTargetFrameRate = -1;\nvSyncCount = 1;\nmaxQueuedFrames = 1;", MessageType.Warning);

        EditorGUILayout.PropertyField(_disableApplyingSettings);
        EditorGUILayout.PropertyField(_targetFrameRate);
        EditorGUILayout.PropertyField(_vSyncCount);
        EditorGUILayout.PropertyField(_maxQueuedFrames);

        serializedObject.ApplyModifiedProperties();
    }
}
