using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoRenderingController : MonoBehaviour
{
    public int ScreenWidth = 1920;
    public int ScreenHeightStereo = 2760;
    public int ScreenHeightMono = 1080;

    public bool MonoMode = false;

    void Start() {
        SetRenderingMode(MonoMode);
    }

    void Update()
    {
        if((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && 
            (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
            (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) &&
            Input.GetKeyDown(KeyCode.M)) {
            SetRenderingMode(!MonoMode);
        }
    }

    public void SetRenderingMode(bool mono) {
        MonoMode = mono;

        TryAccessComponent<Illumetry.Unity.Glasses>((c)=>c.enabled = !mono);
        TryAccessComponent<Illumetry.Unity.RendererLCD>((c) => c.enabled = !mono);

        TryAccessComponent<Illumetry.Unity.MonoRenderingTracker>((c) => c.enabled = mono);
        TryAccessComponent<Illumetry.Unity.MonoRendererLCD>((c) => c.enabled = mono);

        TryAccessComponent<Illumetry.Unity.DefaultScreenResolution>((c) => { 
            c.Width = ScreenWidth; 
            c.Height = mono ? ScreenHeightMono : ScreenHeightStereo;
        });
    }

    private bool TryAccessComponent<T>(System.Action<T> action) where T: MonoBehaviour {
        var component = GetComponent<T>();
        if(component != null) {
            action(component);
            return true;
        }
        return false;
    }
}
