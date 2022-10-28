using System.Collections;
using UnityEngine;
using System;
using System.Linq;

namespace Illumetry.Unity {
public abstract class BaseRenderer : LifeTimeControllerStateMachine { }

[RequireComponent(typeof(WaveplateColorCorrection))]
public class RendererLCD : LifeTimeControllerStateMachine {
    public static event Action BeforeRenderingL;
    public static event Action BeforeRenderingR;

    public bool Mono;

    public Shader ImageCorrectionShader;
    public Shader CopyShader;
    
    public Vector2Int DisplayResolution = new Vector2Int(1920, 1080);

    /// <summary>
    /// Select R16G16B16A16_SFloat or R32G32B32A32_SFloat format in Inspector if HDR rendering is required
    /// </summary>
    [Tooltip("Select R16G16B16A16_SFloat or R32G32B32A32_SFloat format if HDR rendering is required")]
    public UnityEngine.Experimental.Rendering.GraphicsFormat RenderTargetFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;

    public RenderTexture[] RenderTextures = null;
    
    public RenderTexture CurrentRenderTexture;
    
    [Range(0.3f, 3)] public float CommonGamma = 1.0f;

    private Material _copyMaterial;
    private Material _imageCorrectionMaterial;

    private bool _useGammaCorrection = false;
    private bool _useLimitsCorrection = false;
    private bool _useOverdriveCorrection = true;
    private bool _useWaveplateCorrection = true;
    private float _brightness;
    private int _frameN = 0;
    
    private RenderTexture GetCurrentOverdriveRenderTexture(bool left) => RenderTextures[left ? 0 : 1];
    private RenderTexture GetPreviousOverdriveRenderTexture(bool left) => RenderTextures[left ? 1 : 0];

    protected virtual void Reset() {
        var shaders = Resources.FindObjectsOfTypeAll(typeof(Shader));
        ImageCorrectionShader = (Shader)shaders.FirstOrDefault((m) => m.name == "Hidden/Illumetry/ImageCorrectionLCD");
        CopyShader = (Shader)shaders.FirstOrDefault((m) => m.name == "Hidden/Illumetry/CopyShader");
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
            } else { 
                _useWaveplateCorrection = false;    
            } 
        } 
        
        var pose = glasses?.GetEyePose(left, 0.05f);
        if (pose.HasValue) {
            waveplateCorrection.GlassesRotation = pose.Value.rotation;
            camera.transform.localPosition = pose.Value.position;
        } else {
            camera.transform.localPosition += new Vector3(left ? -0.03f : 0.03f, 0, 0);   
        }

        if (_useWaveplateCorrection) {                            
            var transmittance = waveplateCorrection.GetTransmittance();
            transmittance.x *= transmittance.x;
            transmittance.y *= transmittance.y;
            transmittance.z *= transmittance.z;
            var minTransmittance = Mathf.Min(Mathf.Min(transmittance.x, transmittance.y), transmittance.z);
            var attenuation = new Vector3(minTransmittance/transmittance.x, minTransmittance/transmittance.y, minTransmittance/transmittance.z);

            _brightness = attenuation.x + attenuation.y + attenuation.z;
            const float targetBrightness = 2.3f;
            if(_brightness > targetBrightness){
                attenuation *= targetBrightness/_brightness;    
            }
            _imageCorrectionMaterial.SetVector("Attenuation", attenuation);
        } else {
            _imageCorrectionMaterial.SetVector("Attenuation", Vector3.one); 
        }
    }

    private void LateUpdate(){
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

        camera.targetTexture = CurrentRenderTexture;
        camera.Render();

        var cameraPositionRelativeToFrame = GetComponent<IDisplay>().GetCameraPositionRelativeToFrame(camera.transform.localPosition);

        _imageCorrectionMaterial.SetVector("CameraPositionRelativeToFrame", cameraPositionRelativeToFrame);
        _imageCorrectionMaterial.SetTexture("CurrentFrame", CurrentRenderTexture);
        _imageCorrectionMaterial.SetTexture("PreviousOverdriveFrame", GetPreviousOverdriveRenderTexture(left));
        _imageCorrectionMaterial.SetInt("FrameN", _frameN);
        _imageCorrectionMaterial.SetInt("UseOverdriveCorrection", _useOverdriveCorrection ? 1 : 0);
        _imageCorrectionMaterial.SetInt("UseGammaCorrection", _useGammaCorrection ? 1 : 0);
        _imageCorrectionMaterial.SetInt("UseLimitsCorrection", _useLimitsCorrection ? 1 : 0);
        _imageCorrectionMaterial.SetFloat("CommonGamma", CommonGamma);
        _frameN = (++_frameN)%50;

        RenderTexture.active = GetCurrentOverdriveRenderTexture(left);
        _imageCorrectionMaterial.SetPass(0);
        DrawGlQuad();


        RenderTexture.active = null;
        var scale = (float)Screen.width / DisplayResolution.x;
        float scaledResolutionY = DisplayResolution.y * scale;
        GL.Viewport(new Rect(0, left?(Screen.height- scaledResolutionY):0, Screen.width, scaledResolutionY));

        _copyMaterial.SetTexture("Source", GetCurrentOverdriveRenderTexture(left));
        _copyMaterial.SetPass(QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
        DrawGlQuad();
    }

    private void OnBeforeRender() {
        var display = GetComponent<IDisplay>();

        display?.DisplayProperties?.SetToShader();

        BeforeRenderingL?.Invoke();
        RenderQuad(true);
        BeforeRenderingR?.Invoke();
        RenderQuad(false);
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


        _imageCorrectionMaterial = new Material(ImageCorrectionShader);
        _copyMaterial = new Material(CopyShader);

        RenderTextureDescriptor overdriveRenderTextureDescriptor = new(0, 0) {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            width = DisplayResolution.x,
            height = DisplayResolution.y,
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
            volumeDepth = 1,
            msaaSamples = 1,
        };

        RenderTextures = new RenderTexture[2];
        for (int i = 0; i < RenderTextures.Length; i++) {
            RenderTextures[i] = new RenderTexture(overdriveRenderTextureDescriptor);
            RenderTextures[i].name = "RenderTexture_"+i;
            RenderTextures[i].filterMode = FilterMode.Point;
        }

        RenderTextureDescriptor currentRenderTextureDescriptor = overdriveRenderTextureDescriptor;
        currentRenderTextureDescriptor.graphicsFormat = RenderTargetFormat;

        //currentRenderTextureDescriptor.msaaSamples = 8;
        //currentRenderTextureDescriptor.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D32_SFloat;

        CurrentRenderTexture = new RenderTexture(currentRenderTextureDescriptor);
        CurrentRenderTexture.filterMode = FilterMode.Point;

        {
            Application.onBeforeRender += OnBeforeRender;
            using var _ = new Disposable(() => { 
                Application.onBeforeRender -= OnBeforeRender; 

                if(RenderTextures != null) {
                    foreach (var texture in RenderTextures) {
                        if (texture != null) {
                            texture.Release();
                        }
                    }
                    RenderTextures = null;
                }

                if (CurrentRenderTexture != null) {
                    CurrentRenderTexture.Release();
                    CurrentRenderTexture = null;
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