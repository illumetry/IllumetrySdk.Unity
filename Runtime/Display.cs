using Antilatency.Alt.Environment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Antilatency.DeviceNetwork;

namespace Illumetry.Unity {

    [RequireComponent(typeof(Glasses))]
    [RequireComponent(typeof(RendererLCD))]
    [RequireComponent(typeof(MonoRendererLCD))]
    [RequireComponent(typeof(MonoRenderingTracker))]
    [RequireComponent(typeof(MonoRenderingController))]

    public class Display : LifeTimeControllerStateMachine, IDisplay, IEnvironmentProvider {

        /*public Vector3 ScreenPosition;
        public Vector3 ScreenX;
        public Vector3 ScreenY;*/

        internal static Display ActiveDisplay => _activeDisplay;
        internal static event Action OnDisplayChanged;
        private static Display _activeDisplay;


        [field: SerializeField]
        public DisplayProperties DisplayProperties { get; private set; }

        public Vector2 GetHalfScreenSize() {
            return new Vector2(DisplayProperties.ScreenX.magnitude, DisplayProperties.ScreenY.magnitude);
        }

        public Matrix4x4 GetScreenToEnvironment() {
            var x = DisplayProperties.ScreenX.normalized;
            var y = DisplayProperties.ScreenY.normalized;
            Vector4 w = DisplayProperties.ScreenPosition;
            w.w = 1;
            return new Matrix4x4(x, y, Vector3.Cross(x, y), w);
        }

        public Vector3 GetCameraPositionRelativeToFrame(Vector3 environmentSpaceCameraPosition) {
            return (environmentSpaceCameraPosition - DisplayProperties.ScreenPosition) / DisplayProperties.ScreenY.magnitude;
        }

        private void Reset() {
            if (DisplayProperties == null) {
                DisplayProperties = new DisplayProperties();
            }
            DisplayProperties.SetDefaultProperties_IllumetryIo(DisplayProperties);
        }

        public IEnvironment Environment {
            get {
                return _environment;
            }
        }

        public Illumetry.Display.ICotask Cotask {
            get {
                return _cotask;
            }
        }

        private Illumetry.Display.ICotask _cotask;
        private IEnvironment _environment;

        private Illumetry.Display.ILibrary _displayLibrary;
        private Illumetry.Display.ICotaskConstructor _cotaskConstructor;
        private Antilatency.Alt.Environment.Selector.ILibrary _environmentSelectorLibrary;

        protected override void Destroy() {
            base.Destroy();

            Antilatency.Utils.SafeDispose(ref _environment);
            Antilatency.Utils.SafeDispose(ref _cotask);
            
            Antilatency.Utils.SafeDispose(ref _environmentSelectorLibrary);
            Antilatency.Utils.SafeDispose(ref _cotaskConstructor);
            Antilatency.Utils.SafeDispose(ref _displayLibrary);
            
            _activeDisplay = null;
            OnDisplayChanged?.Invoke();
        }

        protected override IEnumerable StateMachine() {

            _displayLibrary = Illumetry.Display.Library.load();
            _cotaskConstructor = _displayLibrary.getCotaskConstructor();
            _environmentSelectorLibrary = Antilatency.Alt.Environment.Selector.Library.load();
            bool wasMonitor = false;

            string status = "";

        WaitingForNetwork:
            if (Destroying) yield break;
            status = "Waiting for DeviceNetwork";

            INetwork network = GetComponent<IDeviceNetworkProvider>()?.Network;

            wasMonitor = _activeDisplay != null;
            _activeDisplay = null;

            if (wasMonitor) {
                OnDisplayChanged?.Invoke();
            }

            if (network.IsNull()) {
                yield return status;
                goto WaitingForNetwork;
            }

        WaitingForNode:
            if (Destroying) yield break;
            status = "Display is not connected (USB)";

            wasMonitor = _activeDisplay != null;
            _activeDisplay = null;

            if (wasMonitor) {
                OnDisplayChanged?.Invoke();
            }

            if (network.IsNull()) {
                goto WaitingForNetwork;
            }

            var nodes = _cotaskConstructor.findSupportedNodes(network);
            nodes = nodes.Where(x => network.nodeGetStatus(x) == Antilatency.DeviceNetwork.NodeStatus.Idle).ToArray();
            if (nodes.Length == 0) {
                yield return status;
                goto WaitingForNode;
            }

            var node = nodes[0];
            {
                DisplayProperties = new DisplayProperties(network, node);

                using (_environment = _environmentSelectorLibrary.createEnvironment(DisplayProperties.CurrentEnvironment)) {
                    using (_cotask = _cotaskConstructor.startTask(network, node)) {
                        if (_cotask == null) {
                            yield return status;
                            goto WaitingForNode;
                        }

                        if (!_cotask.isTaskFinished()) {
                            if (_activeDisplay != null && _activeDisplay != this) {
                                if (Application.isEditor || Debug.isDebugBuild) {
                                    Debug.LogError($"Detected other {DisplayProperties.HardwareName} display! Please connect only 1 display.");
                                }
                            }

                            _activeDisplay = this;
                            OnDisplayChanged?.Invoke();
                        }

                        while (!_cotask.isTaskFinished()) {
                            yield return null;
                            if (Destroying) yield break;
                        }
                        goto WaitingForNode;
                    }
                }
            }
        }

        public Matrix4x4 GetProjectionMatrix(Vector3 environmentSpaceCameraPosition, float nearClip, float farClip) {
            Vector3 displaySpaceCameraPosition = environmentSpaceCameraPosition - DisplayProperties.ScreenPosition;

            return GetProjectionMatrix(DisplayProperties.ScreenX.magnitude, DisplayProperties.ScreenY.magnitude, displaySpaceCameraPosition, nearClip, farClip);
        }


        Matrix4x4 GetProjectionMatrix(float halfScreenWidth, float halfScreenHeight, Vector3 displaySpaceCameraPosition, float nearClip, float farClip) {
            displaySpaceCameraPosition.z = Mathf.Min(displaySpaceCameraPosition.z, 0.0000000000000001f);

            float left = -halfScreenWidth - displaySpaceCameraPosition.x;
            float right = halfScreenWidth - displaySpaceCameraPosition.x;
            float bottom = -halfScreenHeight - displaySpaceCameraPosition.y;
            float top = halfScreenHeight - displaySpaceCameraPosition.y;

            float x = 2.0f * displaySpaceCameraPosition.z / (right - left);
            float y = 2.0f * displaySpaceCameraPosition.z / (top - bottom);
            float a = (right + left) / (right - left);
            float b = (top + bottom) / (top - bottom);
            float c = -(farClip + nearClip) / (farClip - nearClip);
            float d = -(2.0f * farClip * nearClip) / (farClip - nearClip);
            float e = -1.0f;
            Matrix4x4 matrix = default;
            matrix[0, 0] = -x;
            matrix[0, 1] = 0;
            matrix[0, 2] = a;
            matrix[0, 3] = 0;
            matrix[1, 0] = 0;
            matrix[1, 1] = -y;
            matrix[1, 2] = b;
            matrix[1, 3] = 0;
            matrix[2, 0] = 0;
            matrix[2, 1] = 0;
            matrix[2, 2] = c;
            matrix[2, 3] = d;
            matrix[3, 0] = 0;
            matrix[3, 1] = 0;
            matrix[3, 2] = e;
            matrix[3, 3] = 0;
            return matrix;
            /*
            x   0   a   0
            0   y   b   0
            0   0   c   d
            0   0   e   0
            */
        }
    }
}
