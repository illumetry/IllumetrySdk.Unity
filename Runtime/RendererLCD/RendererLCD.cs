using System.Collections;
using UnityEngine;
using System;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace Illumetry.Unity {
    public abstract class BaseRenderer : LifeTimeControllerStateMachine { }

    [RequireComponent(typeof(WaveplateColorCorrection))]
    public class RendererLCD : LifeTimeControllerStateMachine {
        public static event Action OnBeforeRendererL;
        public static event Action OnBeforeRendererR;

        /// <summary>
        /// Select R16G16B16A16_SFloat or R32G32B32A32_SFloat format in Inspector if HDR rendering is required
        /// </summary>
        [Tooltip("Select R16G16B16A16_SFloat or R32G32B32A32_SFloat format if HDR rendering is required")]
        public UnityEngine.Experimental.Rendering.GraphicsFormat RenderTargetFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;

        [Tooltip("Select only depth formats [D16.., D24.., D32]")]
        public UnityEngine.Experimental.Rendering.GraphicsFormat DepthTargetFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;

        [Range(0.3f, 3)] public float CommonGamma = 1.0f;

        [HideInInspector] public bool Mono;

        [Header("Revert to default, reset the component.")]
        public Shader _imageCorrectionShader;
        public Shader _copyShader;

        public bool UseOverdriveCorrection { get; private set; } = true;
        public bool UseWaveplateCorrection => _useWaveplateCorrection;

        private Material _copyMaterial;
        private Material _imageCorrectionMaterial;

        private Vector2Int _displayResolution = new Vector2Int(1920, 1080);

        private bool _useGammaCorrection = false;
        private bool _useLimitsCorrection = false;
        private bool _useWaveplateCorrection = true;
        private float _brightness;
        private int _frameN = 0;

        private RenderTexture[] _renderTextures = null;
        private RenderTexture _currentRenderTexture;

        private RenderTexture GetCurrentOverdriveRenderTexture(bool left) => _renderTextures[left ? 0 : 1];
        private RenderTexture GetPreviousOverdriveRenderTexture(bool left) => _renderTextures[left ? 1 : 0];

        protected virtual void OnValidate() {
            ValidateShaderFields(false);
        }

        protected virtual void Reset() {
            ValidateShaderFields(true);
        }

        protected override void Create() {
            base.Create();
            ValidateShaderFields(false);
        }

        protected virtual void ValidateShaderFields(bool forceReturnDefault) {
            Shader[] shaders = null;

            if (_imageCorrectionShader == null || forceReturnDefault) {
                shaders = Resources.FindObjectsOfTypeAll<Shader>();
                _imageCorrectionShader = shaders.FirstOrDefault((m) => m.name == "Hidden/Illumetry/ImageCorrectionLCD");

                if (Application.isEditor || Debug.isDebugBuild) {
                    Debug.Log("Auto load _imageCorrectionShader!");
                }
            }

            if (_copyShader == null || forceReturnDefault) {
                if (shaders == null) {
                    shaders = Resources.FindObjectsOfTypeAll<Shader>();
                }

                _copyShader = shaders.FirstOrDefault((m) => m.name == "Hidden/Illumetry/CopyShader");

                if (Application.isEditor || Debug.isDebugBuild) {
                    Debug.Log("Auto load _copyShader!");
                }
            }
        }

        private void SetCameraPosition(Camera camera, bool left) {
            camera.transform.localRotation = Quaternion.identity;

            var glasses = GetComponent<IGlasses>();
            var display = GetComponent<IDisplay>();
            var waveplateCorrection = GetComponent<WaveplateColorCorrection>();

            if (glasses != null && display != null) {
                if (display.DisplayProperties.ScreenPolarizationAngle != null && glasses.GlassesPolarizationAngle != null && glasses.QuarterWaveplateAngle != null) {
                    waveplateCorrection.ScreenPolarizationDeg = display.DisplayProperties.ScreenPolarizationAngle.Value;
                    waveplateCorrection.EyePolarizationAngleDeg = glasses.GlassesPolarizationAngle.Value;
                    waveplateCorrection.WaveplateAngleDeg = glasses.QuarterWaveplateAngle.Value;
                    _useWaveplateCorrection = true;
                }
                else {
                    _useWaveplateCorrection = false;
                }
            }

            var pose = glasses?.GetEyePose(left, 0.05f);
            if (pose.HasValue) {
                waveplateCorrection.GlassesRotation = pose.Value.rotation;
                camera.transform.localPosition = pose.Value.position;
            }
            else {
                camera.transform.localPosition += new Vector3(left ? -0.03f : 0.03f, 0, 0);
            }

            if (_useWaveplateCorrection) {
                var transmittance = waveplateCorrection.GetTransmittance();
                transmittance.x *= transmittance.x;
                transmittance.y *= transmittance.y;
                transmittance.z *= transmittance.z;
                var minTransmittance = Mathf.Min(Mathf.Min(transmittance.x, transmittance.y), transmittance.z);
                var attenuation = new Vector3(minTransmittance / transmittance.x, minTransmittance / transmittance.y, minTransmittance / transmittance.z);

                _brightness = attenuation.x + attenuation.y + attenuation.z;
                const float targetBrightness = 2.3f;
                if (_brightness > targetBrightness) {
                    attenuation *= targetBrightness / _brightness;
                }
                _imageCorrectionMaterial.SetVector("Attenuation", attenuation);
            }
            else {
                _imageCorrectionMaterial.SetVector("Attenuation", Vector3.one);
            }
        }

        private void LateUpdate() {
            /*
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1)) { 
                UseOverdriveCorrection = true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F2)) { 
                UseOverdriveCorrection = false;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F3)) { 
                UseGammaCorrection = true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F4)) { 
                UseGammaCorrection = false;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F5)) { 
                UseLimitsCorrection = true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F6)) { 
                UseLimitsCorrection = false;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.F7)) { 
                UseWaveplateCorrection = !UseWaveplateCorrection;
            }*/
        }

        private void SetProjectionMatrix(Camera camera) {
            var display = GetComponent<IDisplay>();
            camera.projectionMatrix = display.GetProjectionMatrix(camera.transform.localPosition, camera.nearClipPlane, camera.farClipPlane);
        }

        private void DrawGlQuad() {
            GL.Begin(GL.QUADS);
            GL.Vertex3(-1, -1, 0);
            GL.Vertex3(-1, 1, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(1, -1, 0);
            GL.End();
        }

        private void RenderQuad(bool left) {
            var camera = GetComponentInChildren<Camera>();
            SetCameraPosition(camera, left);
            SetProjectionMatrix(camera);

            camera.targetTexture = _currentRenderTexture;
            camera.Render();

            var cameraPositionRelativeToFrame = GetComponent<IDisplay>().GetCameraPositionRelativeToFrame(camera.transform.localPosition);

            _imageCorrectionMaterial.SetVector("CameraPositionRelativeToFrame", cameraPositionRelativeToFrame);
            _imageCorrectionMaterial.SetTexture("CurrentFrame", _currentRenderTexture);
            _imageCorrectionMaterial.SetTexture("PreviousOverdriveFrame", GetPreviousOverdriveRenderTexture(left));
            _imageCorrectionMaterial.SetInt("FrameN", _frameN);
            _imageCorrectionMaterial.SetInt("UseOverdriveCorrection", UseOverdriveCorrection ? 1 : 0);
            _imageCorrectionMaterial.SetInt("UseGammaCorrection", _useGammaCorrection ? 1 : 0);
            _imageCorrectionMaterial.SetInt("UseLimitsCorrection", _useLimitsCorrection ? 1 : 0);
            _imageCorrectionMaterial.SetFloat("CommonGamma", CommonGamma);
            _frameN = (++_frameN) % 50;

            RenderTexture.active = GetCurrentOverdriveRenderTexture(left);
            _imageCorrectionMaterial.SetPass(0);
            DrawGlQuad();


            RenderTexture.active = null;
            var scale = (float)Screen.width / _displayResolution.x;
            float scaledResolutionY = _displayResolution.y * scale;
            GL.Viewport(new Rect(0, left ? (Screen.height - scaledResolutionY) : 0, Screen.width, scaledResolutionY));

            _copyMaterial.SetTexture("Source", GetCurrentOverdriveRenderTexture(left));
            _copyMaterial.SetPass(QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
            DrawGlQuad();
        }

       /* Stopwatch debugStopWatchRenderInfo = new Stopwatch();
        StringBuilder debugTimingRenderInfo = new StringBuilder();*/

        private void OnBeforeRender() {

            var display = GetComponent<IDisplay>();
            display?.DisplayProperties?.SetToShader();

           /* debugTimingRenderInfo.AppendLine("------------------------");
            debugTimingRenderInfo.AppendLine($"Elapsed milliseconds (BEFORE RESTART TIMER): {debugStopWatchRenderInfo.GetElapsedMillisecondsWithFractionalPart()}");
            debugStopWatchRenderInfo.Restart();*/

            var timer = Stopwatch.StartNew();

           // debugTimingRenderInfo.AppendLine($"Start timers!");
            OnBeforeRendererL?.Invoke();
            RenderQuad(true);

          //  debugTimingRenderInfo.AppendLine($"Elapsed milliseconds (AFTER QUAD 1): {timer.GetElapsedMillisecondsWithFractionalPart()}");
            WaitPeriod(display, timer);
         //   debugTimingRenderInfo.AppendLine($"Elapsed milliseconds (BEFORE QUAD 2 | AFTER WAIT PERIOD): {debugStopWatchRenderInfo.GetElapsedMillisecondsWithFractionalPart()}");

            OnBeforeRendererR?.Invoke();
            RenderQuad(false);
        }

       /* private void OnApplicationQuit() {
            Debug.Log(debugTimingRenderInfo.ToString());
        }
       */

        private void WaitPeriod(IDisplay display, Stopwatch timer) {
            var fps = display?.DisplayProperties?.Fps ?? 120;
            var periodMicroseconds = 1000000.0 / fps;


            while (true) {
                var microseconds = 1000000 * timer.ElapsedTicks / (double)Stopwatch.Frequency;
                if (microseconds >= periodMicroseconds)
                    break;
            }
        }

        protected override IEnumerable StateMachine() {
            string status = "";

            goto WaitingForDisplay;
        //Reset:

        WaitingForDisplay:
            if (Destroying) yield break;
            status = "Waiting for Display";


            var display = GetComponent<IDisplay>();

            if (display == null) {
                yield return status;
                goto WaitingForDisplay;
            }

            if (display.DisplayProperties == null) {
                status = "Waiting for DisplayProperties";
                yield return status;
                goto WaitingForDisplay;
            }

            _displayResolution = display.DisplayProperties.Resolution;


        WaitingForCamera:
            if (Destroying) yield break;
            status = "Camera component not found";
            var camera = GetComponentInChildren<Camera>();
            if (!camera) {
                yield return status;
                goto WaitingForCamera;
            }

            camera.enabled = false;

            _imageCorrectionMaterial = new Material(_imageCorrectionShader);
            _copyMaterial = new Material(_copyShader);

            var overdriveRenderTextureDescriptor = new RenderTextureDescriptor(0, 0) {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                width = _displayResolution.x,
                height = _displayResolution.y,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat,
                volumeDepth = 1,
                msaaSamples = 1,
            };

            _renderTextures = new RenderTexture[2];
            for (int i = 0; i < _renderTextures.Length; i++) {
                _renderTextures[i] = new RenderTexture(overdriveRenderTextureDescriptor);
                _renderTextures[i].name = "RenderTexture_" + i;
                _renderTextures[i].filterMode = FilterMode.Point;
            }

            RenderTextureDescriptor currentRenderTextureDescriptor = overdriveRenderTextureDescriptor;
            currentRenderTextureDescriptor.graphicsFormat = RenderTargetFormat;
            currentRenderTextureDescriptor.depthStencilFormat = DepthTargetFormat;
            //currentRenderTextureDescriptor.msaaSamples = 8;

            _currentRenderTexture = new RenderTexture(currentRenderTextureDescriptor);
            _currentRenderTexture.filterMode = FilterMode.Point;

            {
                Application.onBeforeRender += OnBeforeRender;

                using (var _ = new Disposable(() => {
                    Application.onBeforeRender -= OnBeforeRender;

                    if (_renderTextures != null) {
                        foreach (var texture in _renderTextures) {
                            if (texture != null) {
                                texture.Release();
                            }
                        }

                        _renderTextures = null;
                    }

                    if (_currentRenderTexture != null) {
                        _currentRenderTexture.Release();
                        _currentRenderTexture = null;
                    }
                })) {
                    while (!Destroying) {
                        var currentDisplayResolution = display?.DisplayProperties.Resolution;
                        if (currentDisplayResolution != _displayResolution) {
                            goto WaitingForDisplay;
                        }

                        yield return "";
                    }
                }
            }
        }
    }
}