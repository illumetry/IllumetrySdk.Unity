using System.Collections;
using System.Collections.Generic;
using Illumetry;
using Illumetry.Unity;
using UnityEngine;
using UnityEditor;
using Display = Illumetry.Unity.Display;

namespace Illumetry.Unity.Editor {
    public class IllumetryEditorMenu : MonoBehaviour {
        [MenuItem("Illumetry/Add Illumetry Display to Scene")]
        static void GenerateIllumetryDisplayStructure() {
            var handle = GenerateIllumetryDisplayHandle();
            var display = GenerateIllumetryDisplay();
            var camera = GenerateIllumetryCamera();

            camera.parent = display;

            camera.localPosition = new Vector3(0.0f, 0.15f, -0.255f);
            camera.localRotation = Quaternion.identity;
            camera.localScale = Vector3.one;

            display.parent = handle;

            display.localPosition = Vector3.zero;
            display.localRotation = Quaternion.identity;
            display.localScale = Vector3.one;
        }

        static Transform GenerateIllumetryDisplayHandle() {
            var displayHandleGO = new GameObject("IllumetryDisplayHandle");

            displayHandleGO.AddComponent<DisplayHandle>();

            return displayHandleGO.transform;
        }

        static Transform GenerateIllumetryDisplay() {
            var displayGO = new GameObject("IllumetryDisplay");

            displayGO.AddComponent<DeviceNetworkProvider>();
            displayGO.AddComponent<DefaultScreenResolution>();
            displayGO.AddComponent<Glasses>();

            var waveplateCC = displayGO.AddComponent<WaveplateColorCorrection>();
            waveplateCC.Transmittance = new Vector3(0.6558238f, 0.7071067f, 0.8025268f);

            displayGO.AddComponent<RendererLCD>();
            displayGO.AddComponent<MonoRendererLCD>();
            displayGO.AddComponent<MonoRenderingTracker>();
            displayGO.AddComponent<MonoRenderingController>();
            displayGO.AddComponent<Display>();

            return displayGO.transform;
        }

        static Transform GenerateIllumetryCamera() {
            var cameraGO = new GameObject("IllumetryCamera");

            var camera = cameraGO.AddComponent<Camera>();
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100.0f;

            cameraGO.AddComponent<AudioListener>();

            return cameraGO.transform;
        }
    }
}