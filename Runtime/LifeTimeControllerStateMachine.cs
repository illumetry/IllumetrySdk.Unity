using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class LifeTimeControllerStateMachine : LifeTimeController {

    [Serializable]
    public class TStatus {
        public enum TKind {
            Unknown,
            Ok,
            Warning,
            Error
        }
        //private static int staticUselessFieldToTriggerUnityEditor;
        //[SerializeField]
        //private int uselessFieldToTriggerUnityEditor = staticUselessFieldToTriggerUnityEditor++;
        public TKind Kind { get; set; }
        public string Description { get; set; }
    }

    //private TStatus status;
    public TStatus Status;
    private void SetStatus(TStatus status) {
        Status = status;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }


    protected IEnumerator enumerator;
    protected override void Create() {
        enumerator = StateMachine().GetEnumerator();
    }

    protected bool Destroying { get; private set; }
    protected override void Destroy() {
        Destroying = true;

        int iterations = 1024;
        while (Tick()) {
            if (iterations <= 0)
                throw new Exception("StateMachine does not exit. Check 'Destroing' field in every loop: if (Destroing) yield break;");
            iterations--;
        }
        Status = default;
        Destroying = false;
    }

    
    /*public virtual bool Tick() {        
        return enumerator.MoveNext();
    }*/


    public bool Tick() {
        if (enumerator == null)
            return false;
        try {
            var result = enumerator.MoveNext();

            var description = enumerator?.Current?.ToString();
            if (!string.IsNullOrEmpty(description)) {
                SetStatus(new TStatus { Kind = TStatus.TKind.Warning, Description = description });
            } else {
                SetStatus(Status = new TStatus { Kind = TStatus.TKind.Ok });
            }
            return result;
        }
        catch (Exception ex) {
            SetStatus(new TStatus { Kind = TStatus.TKind.Error, Description = ex.Message });
            enumerator = null;
            Debug.LogError(ex);
        }        
        return false;
    }

    public void Update() {
        Tick();
    }

    protected abstract IEnumerable StateMachine();


}
