using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity.Demo
{
    using Illumetry.Unity.Stylus;
    public class StylusGrabber : MonoBehaviour
    {
        public Stylus Stylus => _stylus;
        public Rigidbody PhysicComponent => physicComponent;
        public IStylusGrabbable DragObject => _dragObject;


        [SerializeField] private Stylus _stylus;
        [SerializeField] private Rigidbody physicComponent;
        [SerializeField] private Vector3 _triggerSize = Vector3.one;
        [SerializeField] private Vector3 _triggerOffset;
        [SerializeField, Header("If no, will using BoxCast")] private bool _useOverlapBox;

        private Dictionary<int, IStylusGrabbable> _dragableObjects = new Dictionary<int, IStylusGrabbable>();
        private List<Collider> _enteredColliders;
        private IStylusGrabbable _dragObject;
        private bool _lastStylusButtonPhaseState;

        private void OnEnable()
        {
            ResetGrabber();
            SubscribeStylusEvents();
        }

        private void OnDisable()
        {
            ResetGrabber();
        }

        private void OnDestroy()
        {
            ResetGrabber();
        }

        private void SubscribeStylusEvents()
        {
            if (_stylus != null)
            {
                _stylus.OnUpdatedButtonPhase += OnUpdatedStylusButtonPhase;
                _stylus.OnUpdatedPose += OnUpdatedPose;
            }
            else
            {
                Debug.LogError("Stylus is null! Need stylus! Use method SetStylus(); or set to public field.");
            }
        }

        private void UnSubscribeStylusEvents()
        {
            if (_stylus != null)
            {
                _stylus.OnUpdatedButtonPhase -= OnUpdatedStylusButtonPhase;
                _stylus.OnUpdatedPose -= OnUpdatedPose;
            }
            else
            {
                Debug.LogError("Stylus is null! Need stylus! Use method SetStylus(); or set to public field.");
            }
        }

        internal void SetStylus(Stylus stylus)
        {
            if (_stylus != null)
            {
                ResetGrabber();
            }

            _stylus = stylus;
            SubscribeStylusEvents();
        }

        private void FixedUpdate()
        {
            UpdateColliders(_useOverlapBox);
        }

        private void UpdateColliders(bool useOverlapBox)
        {
            Vector3 posTrigger = transform.position;
            Vector3 triggerSize = _triggerSize;
            triggerSize.Scale(transform.lossyScale);

            RaycastHit[] hits = useOverlapBox ? new RaycastHit[0] : Physics.BoxCastAll(posTrigger, triggerSize * 0.5f, transform.forward, transform.rotation, 0f);
            Collider[] currentColliders = useOverlapBox ? Physics.OverlapBox(posTrigger, triggerSize * 0.5f, transform.rotation) : new Collider[hits.Length];

            List<Collider> noFoundColliders = new List<Collider>();
            noFoundColliders.AddRange(_enteredColliders);
            _enteredColliders.Clear();

            for (int i = currentColliders.Length - 1; i >= 0; i--)
            {
                Collider tryCollider = currentColliders[i];
                Collider currentCollider = tryCollider == null ? hits[i].collider : tryCollider;

                bool isFound = false;

                for (int j = noFoundColliders.Count - 1; j >= 0; j--)
                {
                    Collider pCollider = noFoundColliders[j];

                    if (pCollider == currentCollider)
                    {
                        isFound = true;
                        noFoundColliders.RemoveAt(j);
                        break;
                    }
                }

                if (!isFound)
                {
                    _enteredColliders.Add(currentCollider);
                    ColliderWasEnter(currentCollider);
                }
                else
                {
                    _enteredColliders.Add(currentCollider);
                    ColliderStay(currentCollider);
                }
            }


            for (int i = noFoundColliders.Count - 1; i >= 0; i--)
            {
                Collider col = noFoundColliders[i];
                ColliderWasExit(col);
            }
        }

        private void ColliderWasEnter(Collider col)
        {
            AddOrUpdateGrabbableObject(col);
        }

        private void ColliderStay(Collider col)
        {
            AddOrUpdateGrabbableObject(col);
        }

        private void ColliderWasExit(Collider col)
        {
            RemoveGrabbableObject(col);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Matrix4x4 prevMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;
            Vector3 posTrigger = transform.InverseTransformPoint(transform.position);
            Gizmos.DrawWireCube(posTrigger + _triggerOffset, _triggerSize);
            Gizmos.matrix = prevMatrix;
        }
#endif

        private void AddOrUpdateGrabbableObject(Collider col)
        {
            IStylusGrabbable stylusDragable = col.GetComponent<IStylusGrabbable>();

            if (stylusDragable != null)
            {
                if (!_dragableObjects.ContainsKey(stylusDragable.InstanceId))
                {
                    stylusDragable.OnDestroing += OnDestroingGrabbableObject;
                }

                //Debug.Log($"Was entered to col -> {col.name}");
                _dragableObjects[stylusDragable.InstanceId] = stylusDragable;
            }
        }

        private void OnDestroingGrabbableObject(IStylusGrabbable stylusDragable)
        {
            RemoveGrabbableObject(stylusDragable);
        }

        private void RemoveGrabbableObject(Collider col)
        {
            IStylusGrabbable stylusDragable = col.GetComponent<IStylusGrabbable>();
            RemoveGrabbableObject(stylusDragable);
            //Debug.Log($"Remove col -> {col.name}");
        }

        private void RemoveGrabbableObject(IStylusGrabbable stylusDragable)
        {
            if (stylusDragable != null)
            {
                stylusDragable.OnDestroing -= OnDestroingGrabbableObject;
                _dragableObjects.Remove(stylusDragable.InstanceId);
            }
        }

        private void OnUpdatedStylusButtonPhase(Stylus stylus, bool isPressed)
        {
            if (_lastStylusButtonPhaseState != isPressed)
            {
                if (isPressed)
                {
                    //Button is down.
                    _dragObject = null;
                    foreach (var kVp in _dragableObjects)
                    {
                        if (kVp.Value == null)
                        {
                            Debug.LogError("Drag object in _dragableObjects is null!");
                            continue;
                        }

                        _dragObject = kVp.Value;
                        break;
                    }

                    if (_dragObject != null)
                    {
                        _dragObject.OnStartGrab(this);
                    }
                }
                else
                {
                    //Click completed.
                    EndDragObject();
                }
            }

            _lastStylusButtonPhaseState = isPressed;
        }

        private void OnUpdatedPose(Pose stylusPose, Vector3 worldVelocity, Vector3 angularVelocity)
        {
            if (_dragObject != null)
            {
                _dragObject.OnGrabProcess(this);
            }
        }

        private void EndDragObject()
        {
            if (_dragObject != null)
            {
                _dragObject.OnEndGrab(this);
                _dragObject = null;
            }
        }

        private void ResetGrabber()
        {
            UnSubscribeStylusEvents();
            EndDragObject();
            _lastStylusButtonPhaseState = false;

            if (_dragableObjects == null)
            {
                _dragableObjects = new Dictionary<int, IStylusGrabbable>();
            }

            if (_enteredColliders == null)
            {
                _enteredColliders = new List<Collider>();
            }

            _dragableObjects.Clear();
            _enteredColliders.Clear();
        }
    }
}