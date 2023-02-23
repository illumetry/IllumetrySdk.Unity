using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity.Demo {

    using Illumetry.Unity.Stylus;

    public class StylusGrabber : MonoBehaviour {
        public Stylus Stylus => _stylus;
        public Rigidbody PhysicComponent => physicComponent;
        public IStylusGrabbable GrabObject => _grabObject;


        [SerializeField] private Stylus _stylus;
        [SerializeField] private Rigidbody physicComponent;
        [SerializeField] private Vector3 _triggerSize = Vector3.one;
        [SerializeField] private Vector3 _triggerOffset;
        [SerializeField, Header("If no, will using BoxCast")] private bool _useOverlapBox;

        private Dictionary<int, IStylusGrabbable> _grabbableObjects = new Dictionary<int, IStylusGrabbable>();
        private List<Collider> _enteredColliders;
        private IStylusGrabbable _grabObject;
        private bool _lastStylusButtonPhaseState;

        private void OnEnable() {
            ResetGrabber();
            SubscribeStylusEvents();
        }

        private void OnDisable() {
            ResetGrabber();
        }

        private void OnDestroy() {
            ResetGrabber();
        }

        private void SubscribeStylusEvents() {
            if (_stylus != null) {
                _stylus.OnUpdatedButtonPhase += OnUpdatedStylusButtonPhase;
                _stylus.OnUpdatedPose += OnUpdatedPose;
            }
            else {
                Debug.LogError("Stylus is null! Need stylus! Use method SetStylus(); or set to public field.");
            }
        }

        private void UnSubscribeStylusEvents() {
            if (_stylus != null) {
                _stylus.OnUpdatedButtonPhase -= OnUpdatedStylusButtonPhase;
                _stylus.OnUpdatedPose -= OnUpdatedPose;
            }
            else {
                Debug.LogError("Stylus is null! Need stylus! Use method SetStylus(); or set to public field.");
            }
        }

        internal void SetStylus(Stylus stylus) {
            if (_stylus != null) {
                ResetGrabber();
            }

            _stylus = stylus;
            SubscribeStylusEvents();
        }

        private void FixedUpdate() {
            UpdateColliders(_useOverlapBox);
        }

        private void UpdateColliders(bool useOverlapBox) {
            Vector3 posTrigger = transform.position;
            Vector3 triggerSize = _triggerSize;
            triggerSize.Scale(transform.lossyScale);

            RaycastHit[] hits = useOverlapBox ? new RaycastHit[0] : Physics.BoxCastAll(posTrigger, triggerSize * 0.5f, transform.forward, transform.rotation, 0f);
            Collider[] currentColliders = useOverlapBox ? Physics.OverlapBox(posTrigger, triggerSize * 0.5f, transform.rotation) : new Collider[hits.Length];

            List<Collider> noFoundColliders = new List<Collider>();
            noFoundColliders.AddRange(_enteredColliders);
            _enteredColliders.Clear();

            for (int i = currentColliders.Length - 1; i >= 0; i--) {
                Collider tryCollider = currentColliders[i];
                Collider currentCollider = tryCollider == null ? hits[i].collider : tryCollider; //For case if using BoxCast. This implementation allows you not to fill the array of colliders and avoids an unnecessary pass through the array.

                bool isFound = false;

                for (int j = noFoundColliders.Count - 1; j >= 0; j--) {
                    Collider pCollider = noFoundColliders[j];

                    if (pCollider == currentCollider) {
                        isFound = true;
                        noFoundColliders.RemoveAt(j);
                        break;
                    }
                }

                if (!isFound) {
                    _enteredColliders.Add(currentCollider);
                    ColliderWasEnter(currentCollider);
                }
                else {
                    _enteredColliders.Add(currentCollider);
                    ColliderStay(currentCollider);
                }
            }


            for (int i = noFoundColliders.Count - 1; i >= 0; i--) {
                Collider col = noFoundColliders[i];
                ColliderWasExit(col);
            }
        }

        private void ColliderWasEnter(Collider col) {
            AddOrUpdateGrabbableObject(col);
        }

        private void ColliderStay(Collider col) {
            AddOrUpdateGrabbableObject(col);
        }

        private void ColliderWasExit(Collider col) {
            RemoveGrabbableObject(col);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
            Vector3 posTrigger = transform.InverseTransformPoint(transform.position);
            Gizmos.DrawWireCube(posTrigger + _triggerOffset, _triggerSize);
            Gizmos.matrix = prevMatrix;
        }
#endif

        private void AddOrUpdateGrabbableObject(Collider col) {
            IStylusGrabbable grabbableObject = col.GetComponent<IStylusGrabbable>();

            if (grabbableObject != null) {
                if (!_grabbableObjects.ContainsKey(grabbableObject.InstanceId)) {
                    grabbableObject.OnDestroing += OnDestroingGrabbableObject;
                }

                //Debug.Log($"Was entered to col -> {col.name}");
                _grabbableObjects[grabbableObject.InstanceId] = grabbableObject;
            }
        }

        private void OnDestroingGrabbableObject(IStylusGrabbable grabbableObject) {
            RemoveGrabbableObject(grabbableObject);
        }

        private void RemoveGrabbableObject(Collider col) {
            IStylusGrabbable grabbableObject = col.GetComponent<IStylusGrabbable>();
            RemoveGrabbableObject(grabbableObject);
            //Debug.Log($"Remove col -> {col.name}");
        }

        private void RemoveGrabbableObject(IStylusGrabbable grabbableObject) {
            if (grabbableObject != null) {
                grabbableObject.OnDestroing -= OnDestroingGrabbableObject;
                _grabbableObjects.Remove(grabbableObject.InstanceId);
            }
        }

        private void OnUpdatedStylusButtonPhase(Stylus stylus, bool isPressed) {
            if (_lastStylusButtonPhaseState != isPressed) {
                if (isPressed) {
                    //Button is down.
                    _grabObject = null;
                    foreach (var kVp in _grabbableObjects) {
                        if (kVp.Value == null) {
                            Debug.LogError("Grab object in _grabbableObjects is null!");
                            continue;
                        }

                        _grabObject = kVp.Value;
                        break;
                    }

                    if (_grabObject != null) {
                        _grabObject.OnStartGrab(this);
                    }
                }
                else {
                    //Click completed.
                    EndGrabObject();
                }
            }

            _lastStylusButtonPhaseState = isPressed;
        }

        private void OnUpdatedPose(Pose stylusPose, Vector3 worldVelocity, Vector3 angularVelocity) {
            if (_grabObject != null) {
                _grabObject.OnGrabProcess(this);
            }
        }

        private void EndGrabObject() {
            if (_grabObject != null) {
                _grabObject.OnEndGrab(this);
                _grabObject = null;
            }
        }

        private void ResetGrabber() {
            UnSubscribeStylusEvents();
            EndGrabObject();
            _lastStylusButtonPhaseState = false;

            if (_grabbableObjects == null) {
                _grabbableObjects = new Dictionary<int, IStylusGrabbable>();
            }

            if (_enteredColliders == null) {
                _enteredColliders = new List<Collider>();
            }

            _grabbableObjects.Clear();
            _enteredColliders.Clear();
        }
    }
}