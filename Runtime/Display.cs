#if UNITY_EDITOR
using UnityEditor;
#endif


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
            if(DisplayProperties == null) {
                DisplayProperties = new DisplayProperties();
            }
            DisplayProperties.SetDefaultProperties_IllumetryIo(DisplayProperties);
        }

        public IEnvironment Environment { get; private set; }

        public Illumetry.Display.ICotask Cotask { get; private set; }

        protected override IEnumerable StateMachine() {

            using var displayLibrary = Illumetry.Display.Library.load();
            using var cotaskConstructor = displayLibrary.getCotaskConstructor();
            using var environmenSelectorLibrary = Antilatency.Alt.Environment.Selector.Library.load();
            
            string status = "";

            WaitingForNetwork:
            if (Destroying) yield break;
            status = "Waiting for DeviceNetwork";

            INetwork network = GetComponent<IDeviceNetworkProvider>()?.Network;

            if (network.IsNull()) {
                yield return status;
                goto WaitingForNetwork;
            }

            WaitingForNode:
            if (Destroying) yield break;
            status = "Display is not connected (USB)";

            if (network.IsNull())
                goto WaitingForNetwork;

            var nodes = cotaskConstructor.findSupportedNodes(network);
            nodes = nodes.Where(x => network.nodeGetStatus(x) == Antilatency.DeviceNetwork.NodeStatus.Idle).ToArray();
            if (nodes.Length == 0) {
                yield return status;
                goto WaitingForNode;
            }

            var node = nodes[0];
            {
                DisplayProperties = new DisplayProperties(network, node);

                using var environment = environmenSelectorLibrary.createEnvironment(DisplayProperties.CurrentEnvironment);
                Environment = environment;

                
                using var cotask = cotaskConstructor.startTask(network, node);
                Cotask = cotask;
                if (cotask == null){
                    yield return status;
                    goto WaitingForNode;
                }


                while (!cotask.isTaskFinished()) {
                    yield return null;
                    if (Destroying) yield break;
                }
                goto WaitingForNode;

            }
        }

        public Matrix4x4 GetProjectionMatrix(Vector3 environmentSpaceCameraPosition, float nearClip, float farClip) {
            Vector3 displaySpaceCameraPosition = environmentSpaceCameraPosition - DisplayProperties.ScreenPosition;
            
            return GetProjectionMatrix(DisplayProperties.ScreenX.magnitude, DisplayProperties.ScreenY.magnitude, displaySpaceCameraPosition, nearClip, farClip);
        }


        Matrix4x4 GetProjectionMatrix(float halfScreenWidth, float halfScreenHeight, Vector3 displaySpaceCameraPosition, float nearClip, float farClip) {
            displaySpaceCameraPosition.z = MathF.Min(displaySpaceCameraPosition.z, 0.0000000000000001f);

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





#if UNITY_EDITOR
        private void OnDrawGizmos() {

            //Debug.Log(Node);


            var matrix = Handles.matrix;
            Handles.matrix = transform.localToWorldMatrix;


            if (!Environment.IsNull()) {
                foreach (var i in Environment.getMarkers()) {
                    Handles.SphereHandleCap(0, i, Quaternion.identity, 0.005f, EventType.Repaint);
                }
            }


            Handles.matrix *= GetScreenToEnvironment();
            var halfScreenSize = GetHalfScreenSize();

            Handles.DrawSolidRectangleWithOutline(new Rect(-halfScreenSize, 2* halfScreenSize), new Color(0, 0, 0, 0), Color.white);


            Handles.matrix = matrix;

        }
#endif

    }
}
