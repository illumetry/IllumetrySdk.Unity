using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeTimeController : MonoBehaviour {
    void OnEnable() {
        //Debug.Log("OnEnable");
        CreateInternal();
    }
    void OnDisable() {
        //Debug.Log("OnDisable");
        DestroyInternal();
    }
    private bool Created = false;
    private void CreateInternal() {
        if (Created)
            return;
        //Debug.Log("Creating");
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DestroyInternal;
#endif
        Create();
        Created = true;
    }
    private void DestroyInternal() {
        if (!Created)
            return;
        //Debug.Log("Destroing");
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= DestroyInternal;
#endif
        Destroy();
        Created = false;
    }
    protected virtual void Create() {

    }
    protected virtual void Destroy() {

    }
}
