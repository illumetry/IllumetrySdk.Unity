using UnityEngine;
using System;
using System.Collections;

#if UNITY_2021_2_OR_NEWER
using System.Collections.Generic;
#endif

namespace Illumetry.Unity {
    public class DefaultScreenResolution : MonoBehaviour {

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static int _firstReceivedWidth;
        private static int _firstReceivedHeight;
        private static FullScreenMode _firstReceivedMode;

#if UNITY_2021_2_OR_NEWER
        private static DisplayInfo _firstReceivedMainDisplay;
#endif
        private static Vector2Int _firstReceivedMainDisplayPosition;

        [HideInInspector] public int Width = 1920;
        [HideInInspector] public int Height = 2760;
        [HideInInspector] public int TargetRefreshRate = 60;
        public bool AllowReturnToFirstParamsOnDisabled = true;
        private Coroutine _updateResolutionProcess;

#if UNITY_2021_2_OR_NEWER
        private List<DisplayInfo> _cachedConnectedDisplays = new List<DisplayInfo>();
#endif
        private WaitForSecondsRealtime _delayUpdate;
        private WaitForEndOfFrame _waitEndFrame;

        private void Awake() {
            _delayUpdate = new WaitForSecondsRealtime(1f);
            _waitEndFrame = new WaitForEndOfFrame();

            if (_firstReceivedWidth == 0 || _firstReceivedHeight == 0) {
                _firstReceivedWidth = Screen.width;
                _firstReceivedHeight = Screen.height;
                _firstReceivedMode = Screen.fullScreenMode;

#if UNITY_2021_2_OR_NEWER
                _firstReceivedMainDisplay = Screen.mainWindowDisplayInfo;
                _firstReceivedMainDisplayPosition = Screen.mainWindowPosition;
#endif
            }
        }

        private void OnEnable() {
            StartUpdateResolution();
        }

        private void OnDisable() {
            StopUpdateResolutionProcess();
            ReturnToFirstReceivedParams();
        }

        private void StartUpdateResolution() {
            StopUpdateResolutionProcess();

#if !UNITY_EDITOR
            _updateResolutionProcess = StartCoroutine(UpdateResolution());
#endif
        }

        private void StopUpdateResolutionProcess() {
            if (_updateResolutionProcess != null) {
                StopCoroutine(_updateResolutionProcess);
                _updateResolutionProcess = null;
            }
        }

        private void ReturnToFirstReceivedParams() {

            if (!AllowReturnToFirstParamsOnDisabled) {
                return;
            }

#if !UNITY_EDITOR
            Screen.SetResolution(_firstReceivedWidth, _firstReceivedHeight, _firstReceivedMode);

#if UNITY_2021_2_OR_NEWER
            if (HasDisplay(_firstReceivedMainDisplay)) {
              Screen.MoveMainWindowTo(_firstReceivedMainDisplay, _firstReceivedMainDisplayPosition);
            }
#endif
#endif
        }

        private IEnumerator UpdateResolution() {

            while (enabled) {
                if (Application.isFocused) {

#if UNITY_2021_2_OR_NEWER
                    Screen.GetDisplayLayout(_cachedConnectedDisplays);
                    DisplayInfo? illumetryDisplay = FindIllumetryDisplay();

                    if (IsAllowMoveAppToDisplay(illumetryDisplay)) {
                        AsyncOperation asyncOperation = Screen.MoveMainWindowTo(illumetryDisplay.Value, Vector2Int.zero);
                        while(!asyncOperation.isDone) yield return null;
                    }
#endif

                    if (Screen.currentResolution.height != Height || Screen.currentResolution.width != Width ||
                        Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen) {

                        if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen) {
                            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                        }

                        yield return _waitEndFrame;
                        Screen.SetResolution(Width, Height, FullScreenMode.ExclusiveFullScreen, TargetRefreshRate);
                    }
                }
                yield return _delayUpdate;
            }
        }

#if UNITY_2021_2_OR_NEWER
        private DisplayInfo? FindIllumetryDisplay() {

            Display displayComponent = Display.ActiveDisplay;

            if (displayComponent == null || displayComponent.DisplayProperties == null) {
                return null;
            }

            foreach (var display in _cachedConnectedDisplays) {
                if (display.name.Equals(displayComponent.DisplayProperties.HardwareName)) {
                    return display;
                }
            }

            return null;
        }

        private bool CurrentDisplayIsIllumetry(DisplayInfo? illumetryDisplay) {

            Display displayComponent = Display.ActiveDisplay;

            if (displayComponent == null) {
                return false;
            }

            if (displayComponent.DisplayProperties == null) {
                return false;
            }

            if (illumetryDisplay.HasValue) {
                return illumetryDisplay.Value.name.Equals(Screen.mainWindowDisplayInfo.name);
            }

            return false;
        }

        private bool IsAllowMoveAppToDisplay(DisplayInfo? illumetryDisplay) {

            if (!illumetryDisplay.HasValue) {
                return false;
            }

            if (CurrentDisplayIsIllumetry(illumetryDisplay)) {
                return false;
            }

            return true;
        }

        private bool HasDisplay(DisplayInfo display) {

            foreach(var d in _cachedConnectedDisplays) {
                if (d.name.Equals(display.name)) {
                    return true;
                }
            }

            return false;
        }
#endif
    }
}
