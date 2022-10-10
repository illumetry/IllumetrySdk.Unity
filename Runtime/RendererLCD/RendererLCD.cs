using System.Collections;
using UnityEngine;
using System;
using System.Linq;

namespace Illumetry.Unity {

    
    public abstract class BaseRenderer : LifeTimeControllerStateMachine { 
    
    }

    [RequireComponent(typeof(WaveplateColorCorrection))]
    public class RendererLCD : LifeTimeControllerStateMachine {

        public static event Action BeforeRenderingL;
        public static event Action BeforeRenderingR;

        public bool Mono;

        // Color correction, default values for first glasses: R: 1.23, G: 1.1, B: 1.0
        // [Range(0.0f, 2.0f)]
        // public float ColorCorrectionRed = 1.0f;
        // [Range(0.0f, 2.0f)]
        // public float ColorCorrectionGreen = 1.0f;
        // [Range(0.0f, 2.0f)]
        // public float ColorCorrectionBlue = 1.0f;

        public Shader ImageCorrectionShader;
        private Material ImageCorrectionMaterial;

        public Shader CopyShader;
        private Material CopyMaterial;

        public Vector2Int DisplayResolution = new Vector2Int(1920, 1080);

        /// <summary>
        /// Select R16G16B16A16_SFloat or R32G32B32A32_SFloat format in Inspector if HDR rendering is required
        /// </summary>
        public UnityEngine.Experimental.Rendering.GraphicsFormat RenderTargetFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

        public RenderTexture[] renderTextures = null;

        private RenderTexture GetCurrentOverdriveRenderTexture(bool left) => renderTextures[left ? 0 : 1];
        private RenderTexture GetPreviousOverdriveRenderTexture(bool left) => renderTextures[left ? 1 : 0];
        public RenderTexture currentRenderTexture;


        public bool UseGammaCorrection = false;
        public bool UseLimitsCorrection = false;
        public bool UseOverdriveCorrection = true;
        public bool UseWaveplateCorrection = true;
        public bool GetWaveplateCorrectionFromHardware = true;
        public float brightness;
        [Range(0.3f, 3)]
        public float CommonGamma = 1.0f;


        protected virtual void Reset() {
            var shaders = Resources.FindObjectsOfTypeAll(typeof(Shader));
            ImageCorrectionShader = (Shader)shaders.FirstOrDefault((m) => m.name == "Hidden/Illumetry/ImageCorrectionLCD");
            CopyShader = (Shader)shaders.FirstOrDefault((m) => m.name == "Hidden/Illumetry/CopyShader");
        }


        private void SetCameraPosition(Camera camera, bool left) {
            camera.transform.localRotation = Quaternion.identity;
            
            var glasses = GetComponent<IGlasses>();
            var waveplateCorrection = GetComponent<WaveplateColorCorrection>();
            if(GetWaveplateCorrectionFromHardware){
                var display = GetComponent<IDisplay>();
                if(display.DisplayProperties.ScreenPolarizationAngle != null && glasses.GlassesPolarizationAngle != null && glasses.QuarterWaveplateAngle != null){         
                    waveplateCorrection.ScreenPolarizationDeg = display.DisplayProperties.ScreenPolarizationAngle.Value;
                    waveplateCorrection.EyePolarizationAngleDeg = glasses.GlassesPolarizationAngle.Value;
                    waveplateCorrection.WaveplateAngleDeg = glasses.QuarterWaveplateAngle.Value;
                    UseWaveplateCorrection = true;
                } else { 
                    UseWaveplateCorrection = false;    
                } 
            } 
            
            var pose = glasses?.GetEyePose(left, 0.05f);
            if (pose.HasValue){
                waveplateCorrection.GlassesRotation = pose.Value.rotation;
                camera.transform.localPosition = pose.Value.position;
            } else {
                camera.transform.localPosition += new Vector3(left ? -0.03f : 0.03f, 0, 0);   
            }

            if(UseWaveplateCorrection){                            
                var transmittance = waveplateCorrection.GetTransmittance();
                transmittance.x *= transmittance.x;
                transmittance.y *= transmittance.y;
                transmittance.z *= transmittance.z;
                var minTransmittance = Mathf.Min(Mathf.Min(transmittance.x, transmittance.y), transmittance.z);
                var attenuation = new Vector3(minTransmittance/transmittance.x, minTransmittance/transmittance.y, minTransmittance/transmittance.z);

                brightness = attenuation.x + attenuation.y + attenuation.z;
                const float targetBrightness = 2.3f;
                if(brightness > targetBrightness){
                    attenuation *= targetBrightness/brightness;    
                }
                ImageCorrectionMaterial.SetVector("Attenuation", attenuation);
            } else {
                ImageCorrectionMaterial.SetVector("Attenuation", Vector3.one); 
            }
        }

        private void LateUpdate(){
            // debug 
            //var camera = GetComponentInChildren<Camera>();
            //if(camera != null){
            //    SetCameraPosition(camera, true);
            //}
            if(UnityEngine.Input.GetKeyDown(KeyCode.F1)){ 
                UseOverdriveCorrection = true;
            }
            if(UnityEngine.Input.GetKeyDown(KeyCode.F2)){ 
                UseOverdriveCorrection = false;
            }
            if(UnityEngine.Input.GetKeyDown(KeyCode.F3)){ 
                UseGammaCorrection = true;
            }
            if(UnityEngine.Input.GetKeyDown(KeyCode.F4)){ 
                UseGammaCorrection = false;
            }
            if(UnityEngine.Input.GetKeyDown(KeyCode.F5)){ 
                UseLimitsCorrection = true;
            }
            if(UnityEngine.Input.GetKeyDown(KeyCode.F6)){ 
                UseLimitsCorrection = false;
            }
            if(UnityEngine.Input.GetKeyDown(KeyCode.F7)){ 
                UseWaveplateCorrection = !UseWaveplateCorrection;
            }
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
        int frameN = 0;
        private void RenderQuad(bool left) {
            var camera = GetComponentInChildren<Camera>();
            SetCameraPosition(camera, left);
            SetProjectionMatrix(camera);

            camera.targetTexture = currentRenderTexture;
            camera.Render();

            var cameraPositionRelativeToFrame = GetComponent<IDisplay>().GetCameraPositionRelativeToFrame(camera.transform.localPosition);

            ImageCorrectionMaterial.SetVector("CameraPositionRelativeToFrame", cameraPositionRelativeToFrame);
            ImageCorrectionMaterial.SetTexture("CurrentFrame", currentRenderTexture);
            ImageCorrectionMaterial.SetTexture("PreviousOverdriveFrame", GetPreviousOverdriveRenderTexture(left));
            ImageCorrectionMaterial.SetInt("FrameN", frameN);
            ImageCorrectionMaterial.SetInt("UseOverdriveCorrection", UseOverdriveCorrection ? 1 : 0);
            ImageCorrectionMaterial.SetInt("UseGammaCorrection", UseGammaCorrection ? 1 : 0);
            ImageCorrectionMaterial.SetInt("UseLimitsCorrection", UseLimitsCorrection ? 1 : 0);
            ImageCorrectionMaterial.SetFloat("CommonGamma", CommonGamma);
            frameN = (++frameN)%50;

            RenderTexture.active = GetCurrentOverdriveRenderTexture(left);
            ImageCorrectionMaterial.SetPass(0);
            DrawGlQuad();


            RenderTexture.active = null;
            var scale = (float)Screen.width / DisplayResolution.x;
            float scaledResolutionY = DisplayResolution.y * scale;
            GL.Viewport(new Rect(0, left?(Screen.height- scaledResolutionY):0, Screen.width, scaledResolutionY));

            CopyMaterial.SetTexture("Source", GetCurrentOverdriveRenderTexture(left));
            CopyMaterial.SetPass(QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
            DrawGlQuad();
        }

        

        float _prevTime;
        private void OnBeforeRender() {
            var time = UnityEngine.Time.realtimeSinceStartup;
            var dt = time - _prevTime;
            //UnityEngine.Debug.Log($"dt: {dt:0.0000}");
            _prevTime = time;

            var display = GetComponent<IDisplay>();

            display?.DisplayProperties?.SetToShader();

            BeforeRenderingL?.Invoke();
            RenderQuad(true);
            BeforeRenderingR?.Invoke();
            RenderQuad(false);

            //Graphics.Blit(null, ImageCorrectionMaterial);
            //GL.PopMatrix();
            
        }


        protected override IEnumerable StateMachine() {
            string status = "";

            goto WaitingForDisplay;
            //Reset:


            WaitingForDisplay:
            if (Destroying) yield break;
            status = "Waiting for Display";


            var display = GetComponent<IDisplay>();

            if (display==null) {
                yield return status;
                goto WaitingForDisplay;
            }

            if (display.DisplayProperties == null) {
                status = "Waiting for DisplayProperties";
                yield return status;
                goto WaitingForDisplay;
            }

            DisplayResolution = display.DisplayProperties.Resolution;


            WaitingForCamera:
            if (Destroying) yield break;
            status = "Camera component not found";
            var camera = GetComponentInChildren<Camera>();
            if (!camera) {
                yield return status;
                goto WaitingForCamera;
            }

            camera.enabled = false;


            ImageCorrectionMaterial = new Material(ImageCorrectionShader);
            CopyMaterial = new Material(CopyShader);
            //ImageCorrectionMaterial.SetTexture()


            RenderTextureDescriptor overdriveRenderTextureDescriptor = new(0, 0) {
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                width = DisplayResolution.x,
                height = DisplayResolution.y,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                volumeDepth = 1,
                msaaSamples = 1,
            };



            renderTextures = new RenderTexture[2];
            for (int i = 0; i < renderTextures.Length; i++) {
                
                renderTextures[i] = new RenderTexture(overdriveRenderTextureDescriptor);

                renderTextures[i].name = "RenderTexture_"+i;
                renderTextures[i].filterMode = FilterMode.Point;
            }

            

            RenderTextureDescriptor currentRenderTextureDescriptor = overdriveRenderTextureDescriptor;
            currentRenderTextureDescriptor.graphicsFormat = RenderTargetFormat;

            //currentRenderTextureDescriptor.msaaSamples = 8;
            //currentRenderTextureDescriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;

            currentRenderTexture = new RenderTexture(currentRenderTextureDescriptor);
            currentRenderTexture.filterMode = FilterMode.Point;

            {
                Application.onBeforeRender += OnBeforeRender;
                using var _ = new Disposable(() => { 
                    Application.onBeforeRender -= OnBeforeRender; 

                    if(renderTextures != null) {
                        foreach (var texture in renderTextures) {
                            if (texture != null) {
                                texture.Release();
                            }
                        }
                        renderTextures = null;
                    }

                    if (currentRenderTexture != null) {
                        currentRenderTexture.Release();
                        currentRenderTexture = null;
                    }
                });            

                while (!Destroying) {
                    var currentDisplayResolution = display?.DisplayProperties.Resolution;
                    if (currentDisplayResolution != DisplayResolution) {
                        goto WaitingForDisplay;
                    }

                    yield return "";
                }

            }
        }
    }
}