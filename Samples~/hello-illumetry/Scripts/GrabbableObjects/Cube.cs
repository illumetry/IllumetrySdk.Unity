using System;
using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity.Demo {
    [RequireComponent(typeof(Rigidbody))]
    public class Cube : MonoBehaviour, IStylusGrabbable {
        public Transform visual;
        public int InstanceId => gameObject.GetInstanceID();
        public event Action<IStylusGrabbable> OnDestroing;

        private Dictionary<int, StylusGrabber> _stylusGrabbers = new Dictionary<int, StylusGrabber>();
        private Rigidbody _rb;
        private FixedJoint _fixedJoint;
        private Vector3 _startPos;
        private Quaternion _startRot;
        private Vector3 _lastGrabVelocity;
        private Vector3 _lastGrabAngularVelocity;

        private Vector3 _visualLocalPos;
        private Quaternion _visualLocalRot;
        private Vector3 _visualLocalScale;

        private void Awake() {
            _startPos = transform.position;
            _startRot = transform.rotation;

            _visualLocalPos = visual.localPosition;
            _visualLocalRot = visual.localRotation;
            _visualLocalScale = visual.localScale;
        }

        private void OnEnable() {
            if (_rb == null) {
                _rb = GetComponent<Rigidbody>();
            }
        }

        public void OnStartGrab(StylusGrabber stylusGrabber) {
            if (stylusGrabber == null || stylusGrabber.Stylus == null) {
                return;
            }

            _stylusGrabbers[stylusGrabber.Stylus.Id] = stylusGrabber;
            UpdateGrabState();
        }

        public void OnGrabProcess(StylusGrabber stylusGrabber) {

        }

        public void OnEndGrab(StylusGrabber stylusGrabber) {
            if (stylusGrabber == null || stylusGrabber.Stylus == null) {
                return;
            }

            _stylusGrabbers.Remove(stylusGrabber.Stylus.Id);
            SaveLastGrabVelocities(stylusGrabber);
            UpdateGrabState();
        }

        private void SaveLastGrabVelocities(StylusGrabber stylusGrabber) {
            if (stylusGrabber == null || stylusGrabber.Stylus == null) {
                _lastGrabVelocity = Vector3.zero;
                _lastGrabAngularVelocity = Vector3.zero;
                return;
            }

            _lastGrabVelocity = stylusGrabber.Stylus.ExtrapolatedVelocity;
            _lastGrabAngularVelocity = stylusGrabber.Stylus.ExtrapolatedAngularVelocity;
        }

        private void UpdateGrabState() {
            StylusGrabber stylusGrabber = GetCurrentGrabber();
            bool isGrab = stylusGrabber != null;

            if (isGrab) {
                if (_fixedJoint == null) {
                    _fixedJoint = gameObject.AddComponent<FixedJoint>();
                }

                SetVisualParentGrabber(stylusGrabber);
                _fixedJoint.connectedBody = stylusGrabber.PhysicComponent;
                _rb.useGravity = false;
            }
            else {
                if (_fixedJoint != null) {
                    Destroy(_fixedJoint);
                }


                SetVisualParentMe();
                _rb.useGravity = true;
                _rb.velocity = _lastGrabVelocity;
                _rb.angularVelocity = _lastGrabAngularVelocity;
            }
        }

        private void OnDestroy() {
            SetVisualParentMe();
            OnDestroing?.Invoke(this);
        }

        private void SetVisualParentGrabber(StylusGrabber grabber) {
            visual.SetParent(grabber.transform);
        }

        private void SetVisualParentMe() {
            visual.SetParent(transform);
            visual.transform.localPosition = _visualLocalPos;
            visual.transform.localRotation = _visualLocalRot;
            visual.transform.localScale = _visualLocalScale;
        }

        private StylusGrabber GetCurrentGrabber() {
            foreach (var kvp in _stylusGrabbers) {
                return kvp.Value;
            }

            return null;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.R)) {
                if (_rb != null) {
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }

                transform.position = _startPos;
                transform.rotation = _startRot;
            }
        }
    }
}