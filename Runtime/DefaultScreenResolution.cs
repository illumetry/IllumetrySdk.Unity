using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Text;

namespace Illumetry.Unity
{
    public class DefaultScreenResolution : MonoBehaviour
    {
#if !UNITY_WEBGL
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static int _firstReceiveWidth;
        private static int _firstReceiveHeight;
        private static FullScreenMode _firstReceiveMode;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); //ShowWindow needs an IntPtr

        [DllImport("user32.dll")]
        static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string windowName);

        public int Width = 1920;
        public int Height = 2760;

        private Coroutine _updateResolutionProcess;

        // public bool ForceFocus = false;
        // private IntPtr _hwnd;

#if !UNITY_EDITOR
        void Start() {
            // _hwnd = FindHWnd();
        }
#endif
        public static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return String.Empty;
        }

        private void Awake()
        {
            if (_firstReceiveWidth == 0 || _firstReceiveHeight == 0)
            {
                _firstReceiveWidth = Screen.width;
                _firstReceiveHeight = Screen.height;
                _firstReceiveMode = Screen.fullScreenMode;
            }
        }
        
        private void OnEnable()
        {
            StartUpdateResolution();
        }

        private void OnDisable()
        {
            StopUpdateResolutionProcess();
            ReturnToStartResolution();
        }

        // private IntPtr FindHWnd() {
        //     Debug.Log("DefaultScreenResolution::FindHWnd::Start");
        //     IntPtr result = IntPtr.Zero;
        //     EnumWindows(delegate (IntPtr wnd, IntPtr param) {
        //         string wndName = GetWindowText(wnd);
        //         if (wndName == Application.productName) {
        //             uint pid;
        //             GetWindowThreadProcessId(wnd, out pid);
        //             if (Process.GetCurrentProcess().Id == pid) {
        //                 Debug.Log("DefaultScreenResolution::FindHWnd::Founded::" + wndName + "::" + wnd + "::Process=" + Process.GetProcessById((int)pid));
        //                 result = wnd;
        //                 return false;
        //             }
        //         }
        //         return true;
        //     }, IntPtr.Zero);
        //     Debug.Log("DefaultScreenResolution::FindHWnd::End");
        //     return result;
        // }

        private void StartUpdateResolution()
        {
            StopUpdateResolutionProcess();

#if !UNITY_EDITOR
            _updateResolutionProcess = StartCoroutine(UpdateResolution());
#endif
        }

        private void StopUpdateResolutionProcess()
        {
            if (_updateResolutionProcess != null)
            {
                StopCoroutine(_updateResolutionProcess);
                _updateResolutionProcess = null;
            }
        }

        private void ReturnToStartResolution()
        {
#if !UNITY_EDITOR
            Screen.SetResolution(_firstReceiveWidth, _firstReceiveHeight, _firstReceiveMode);
#endif
        }

        private IEnumerator UpdateResolution()
        {
            // Debug.Log("DefaultScreenResolution::StartCoroutine(SetResolution())::"
            // + Screen.currentResolution.width + "x" + Screen.currentResolution.height + "::FullScreenMode=" + Screen.fullScreenMode);
            yield return new WaitForEndOfFrame();
            while (enabled)
            {
                // if (ForceFocus && GetForegroundWindow() != _hwnd) {
                //     SetForegroundWindow(_hwnd);
                //     yield return new WaitForSecondsRealtime(1.01f);
                //     ShowWindow(_hwnd, 9);
                //     yield return new WaitForSecondsRealtime(1.01f);
                // }

                if (Application.isFocused)
                {
                    if (Screen.currentResolution.height != Height || Screen.currentResolution.width != Width ||
                        Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
                    {
                        // Debug.Log("DefaultScreenResolution::Resolution will be changed from " + Screen.currentResolution.width + "x" +
                        // Screen.currentResolution.height + " to " + Width + "x" + Height);
                        if (Screen.fullScreenMode != FullScreenMode.ExclusiveFullScreen)
                        {
                            // Debug.Log("DefaultScreenResolution::Try set ExclusiveFullScreen::Current=" + Screen.fullScreenMode);
                            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                        }

                        yield return new WaitForEndOfFrame();
                        // Debug.Log("DefaultScreenResolution::Change resolution from " + Screen.currentResolution.width + "x" +
                        // Screen.currentResolution.height + " to " + Width + "x" + Height);
                        Screen.SetResolution(Width, Height, FullScreenMode.ExclusiveFullScreen);
                    }
                }

                yield return new WaitForSecondsRealtime(1.01f);
            }
        }
#endif
    }
}
