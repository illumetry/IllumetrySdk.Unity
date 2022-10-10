using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Illumetry.Unity {

    public class MonoRendererLCD : LifeTimeControllerStateMachine {

        private void Reset() {
            this.enabled = false;
        }

        private List<IHeadTracker> _trackers = new List<IHeadTracker>();
        private IHeadTracker GetHeadTracker() {
            _trackers.Clear();
            GetComponents(_trackers);
            return _trackers.FirstOrDefault((t) => (t as MonoBehaviour).enabled);
        }

        private void SetCameraPosition(Camera camera, bool left) {
            camera.transform.localRotation = Quaternion.identity;

            var tracker = GetHeadTracker();

            var pose = tracker?.GetEyePose(left, 0.05f);
            if (pose.HasValue) {
                camera.transform.localPosition = pose.Value.position;
            }
        }

        private void SetProjectionMatrix(Camera camera) {
            var display = GetComponent<IDisplay>();
            camera.projectionMatrix = display.GetProjectionMatrix(camera.transform.localPosition, camera.nearClipPlane, camera.farClipPlane);
        }

        private void OnBeforeRender() {
            var camera = GetComponentInChildren<Camera>();
            SetCameraPosition(camera, true);
            SetProjectionMatrix(camera);
        }


        protected override IEnumerable StateMachine() {
            string status = "";

        WaitingForDisplay:
            if (Destroying) yield break;
            status = "Waiting for Display";
            var display = GetComponent<IDisplay>();

            if (display == null) {
                yield return status;
                goto WaitingForDisplay;
            }

        WaitingForTracker:
            if (Destroying) yield break;
            status = "Waiting for Tracker";
            var tracker = GetComponent<IHeadTracker>();
            if (tracker == null) {
                yield return status;
                goto WaitingForTracker;
            }

        WaitingForCamera:
            if (Destroying) yield break;
            status = "Camera component not found";
            var camera = GetComponentInChildren<Camera>();
            if (!camera) {
                yield return status;
                goto WaitingForCamera;
            }

            camera.enabled = true;
            camera.targetTexture = null;

            {
                Application.onBeforeRender += OnBeforeRender;
                using var _ = new Disposable(() => {
                    Application.onBeforeRender -= OnBeforeRender;
                });

                while (!Destroying) {
                    yield return "";
                }
            }
        }
    }
}