using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity {
    public class LifeTimeController : MonoBehaviour {
        void OnEnable() {
            //Debug.Log("OnEnable");
            CreateInternal();
        }

        void OnDisable() {
            //Debug.Log("OnDisable");
            DestroyInternal();
        }

        private bool _created = false;

        private void CreateInternal() {
            if (_created) {
                return;
            }
            //Debug.Log("Creating");
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += DestroyInternal;
#endif
            Create();
            _created = true;
        }

        private void DestroyInternal() {
            if (!_created) {
                return;
            }
            //Debug.Log("Destroing");
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= DestroyInternal;
#endif
            Destroy();
            _created = false;
        }

        protected virtual void Create() { }
        protected virtual void Destroy() { }
    }
}