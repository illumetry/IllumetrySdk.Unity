using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity.Demo {
    using Illumetry.Unity.Stylus;
    public class StylusVisualSetter : MonoBehaviour {
        [SerializeField] private Stylus _stylus;
        [SerializeField] private List<StylusVisualContainer> _visuals = new List<StylusVisualContainer>();
        [SerializeField] private float _maxPressTimeForDetectClick = 1.5f;
        private int _currentVisibleIndex = 0;
        private bool _previousPhaseStylusButton;
        private float _startPressButtonTime;

        private void OnEnable() {
            if (_stylus != null) {
                _stylus.OnUpdatedButtonPhase += OnUpdatedButtonPhase;
            }

            foreach (var visual in _visuals) {
                if (!IsValidVisualContainer(visual)) {
                    continue;
                }

                visual.gameObject.SetActive(false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(Vector3.zero);
            }

            UpdateSelectedVisual();
        }

        private void OnDisable() {
            if (_stylus != null) {
                _stylus.OnUpdatedButtonPhase -= OnUpdatedButtonPhase;
            }
        }

        private void OnDestroy() {
            if (_stylus != null) {
                _stylus.OnUpdatedButtonPhase -= OnUpdatedButtonPhase;
            }
        }

        internal void SetStylus(Stylus stylus) {
            if (stylus == null) {
                Debug.LogWarning("Try set null stylus!");
                return;
            }

            if (_stylus != null) {
                Debug.LogError("Stylus not null!");
                return;
            }

            _stylus = stylus;
            _stylus.OnUpdatedButtonPhase += OnUpdatedButtonPhase;

            foreach (var visual in _visuals) {
                if (!IsValidVisualContainer(visual)) {
                    continue;
                }

                visual.Inititalize(stylus);
                visual.gameObject.SetActive(false);
            }
        }

        private void OnUpdatedButtonPhase(Stylus stylus, bool isPressed) {
            if (_previousPhaseStylusButton != isPressed) {
                if (isPressed) {
                    //Button down phase.
                    _startPressButtonTime = Time.time;
                }
                else {
                    //Click completed.
                    if (_maxPressTimeForDetectClick > Time.time - _startPressButtonTime) {
                        StylusVisualContainer visualContainer = GetCurrentVisualContainer();
                        StylusGrabber stylusGrabber = visualContainer == null ? null : visualContainer.StylusGrabber;

                        if (stylusGrabber == null || !stylusGrabber.enabled || stylusGrabber.GrabObject == null) {
                            NextVisual();
                        }
                    }
                }
            }

            _previousPhaseStylusButton = isPressed;
        }

        private void NextVisual() {
            _currentVisibleIndex++;
            if (_currentVisibleIndex >= _visuals.Count) {
                _currentVisibleIndex = 0;
            }

            UpdateSelectedVisual();
        }

        private void UpdateSelectedVisual() {
            for (int i = 0; i < _visuals.Count; i++) {
                StylusVisualContainer stylusVisualContainer = _visuals[i];

                if (!IsValidVisualContainer(stylusVisualContainer)) {
                    continue;
                }

                stylusVisualContainer.gameObject.SetActive(i == _currentVisibleIndex);
            }
        }

        private void TryFixCurrentVisibleIndex() {
            if (_currentVisibleIndex >= _visuals.Count) {
                _currentVisibleIndex = 0;
            }

            if (_currentVisibleIndex < 0) {
                _currentVisibleIndex = _visuals.Count - 1;
            }
        }

        private bool IsValidVisualContainer(StylusVisualContainer visualContainer) {
            if (visualContainer == null) {
                if (Application.isEditor || Debug.isDebugBuild) {
                    Debug.LogError("StylusVisualSetter: visual container, one or more is null!");
                }

                return false;
            }

            return true;
        }

        private StylusVisualContainer GetCurrentVisualContainer() {
            TryFixCurrentVisibleIndex();

            if (_visuals.Count == 0) {
                return null;
            }

            return _visuals[_currentVisibleIndex];
        }
    }
}