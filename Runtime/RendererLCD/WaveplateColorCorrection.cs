using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveplateColorCorrection : MonoBehaviour
{
    const float WL_R = 560; //650
    const float WL_G = 510; //530
    const float WL_B = 430; //460
    const float delay = WL_G/4;

    private readonly Vector3 PhaseShift = 2*Mathf.PI*new Vector3(delay/WL_R, delay/WL_G, delay/WL_B); 
    [Range(-180, 180)]
    public float ScreenPolarizationDeg = 0;
    [Range(-180, 180)]
    public float EyePolarizationAngleDeg = 90;
    [Range(-180, 180)]
    public float WaveplateAngleDeg = 45;

    public Vector3 transmittance = Vector3.one;
    public Quaternion GlassesRotation = Quaternion.identity;

    public Vector3 GetTransmittance(){ 
        var screenPolarizationAngle = Mathf.Deg2Rad*ScreenPolarizationDeg;
        var waveplateAngle = Mathf.Deg2Rad*WaveplateAngleDeg;
        var eyePolarizationAngle = Mathf.Deg2Rad*EyePolarizationAngleDeg;
        var eyePolarization = new Vector2(Mathf.Cos(eyePolarizationAngle), Mathf.Sin(eyePolarizationAngle));

        var ScreenPolarization = new Vector2(Mathf.Cos(screenPolarizationAngle), Mathf.Sin(screenPolarizationAngle));
        var screenPolarizationGlassesSpace3 = Quaternion.Inverse(GlassesRotation)*ScreenPolarization;
        var screenPolarizationGlassesSpace = new Vector2(screenPolarizationGlassesSpace3.x, screenPolarizationGlassesSpace3.y);

        var fast = new Vector2(Mathf.Cos(waveplateAngle), Mathf.Sin(waveplateAngle));
        var slow = new Vector2(Mathf.Cos(waveplateAngle + 0.5f*Mathf.PI), Mathf.Sin(waveplateAngle + 0.5f*Mathf.PI));
        var A = Vector2.Dot(fast, eyePolarization) * Vector2.Dot(screenPolarizationGlassesSpace, fast);
        var B = Vector2.Dot(slow, eyePolarization) * Vector2.Dot(screenPolarizationGlassesSpace, slow);

        Func<float, int, float> func = (phi, color) => A*Mathf.Cos(phi) + B*Mathf.Cos(phi + PhaseShift[color]);
        Func<float, int, float> df_dphi = (phi, color) => -A*Mathf.Sin(phi) - B*Mathf.Sin(phi + PhaseShift[color]);
        Func<float, int, float> d2f_dphi2 = (phi, color) => -A*Mathf.Cos(phi) - B*Mathf.Cos(phi + PhaseShift[color]);

        for(int color = 0; color < 3; ++color){ 
            float bestPhi = 0;
            for(int i = 0; i < 10; ++i){ 
                var delta = df_dphi(bestPhi, color)/d2f_dphi2(bestPhi, color);
                if(Mathf.Abs(delta) < 0.001){ 
                    break;    
                }
                bestPhi -= delta;
            }
            transmittance[color] = Math.Abs(func(bestPhi, color));
        }
        return transmittance;
    }

    [Range(0, 2)]
    public int color;
    public void OnDrawGizmos()
    {
        var screenPolarizationAngle = Mathf.Deg2Rad*ScreenPolarizationDeg;
        var waveplateAngle = Mathf.Deg2Rad*WaveplateAngleDeg;
        var eyePolarizationAngle = Mathf.Deg2Rad*EyePolarizationAngleDeg;
        var eyePolarization = new Vector2(Mathf.Cos(eyePolarizationAngle), Mathf.Sin(eyePolarizationAngle));

        var ScreenPolarization = new Vector2(Mathf.Cos(screenPolarizationAngle), Mathf.Sin(screenPolarizationAngle));
        var screenPolarizationGlassesSpace3 = Quaternion.Inverse(GlassesRotation)*ScreenPolarization;
        var screenPolarizationGlassesSpace = new Vector2(screenPolarizationGlassesSpace3.x, screenPolarizationGlassesSpace3.y);

        var fast = new Vector2(Mathf.Cos(waveplateAngle), Mathf.Sin(waveplateAngle));
        var slow = new Vector2(Mathf.Cos(waveplateAngle + 0.5f*Mathf.PI), Mathf.Sin(waveplateAngle + 0.5f*Mathf.PI)); // or -
        var A = Vector2.Dot(screenPolarizationGlassesSpace, fast);
        var B = Vector2.Dot(screenPolarizationGlassesSpace, slow);
        var a = Vector2.Dot(fast, eyePolarization);
        var b = Vector2.Dot(slow, eyePolarization);

        var cols = new Color[3]{ Color.red, Color.green, Color.blue};

        for(int col = 0; col < 3; ++col){                     
            Gizmos.color = cols[col];
            Vector2? prev = null; 
            for(float phi = 0; phi < 2*Mathf.PI; phi += 0.05f){ 
                var cur = A*Mathf.Cos(phi)*fast + B*(Mathf.Cos(phi + PhaseShift[col]))*slow;  
                if(prev != null){
                    Gizmos.DrawLine(prev.Value, cur);
                }
                prev = cur;
            }
        }

        var transmittance = GetTransmittance();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, fast);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, slow);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(Vector3.zero, screenPolarizationGlassesSpace);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(Vector3.zero, eyePolarization);

        for(int col = 0; col < 3; ++col){           
            Gizmos.color = cols[col];
            var bestProjection = eyePolarization*transmittance[col];
            var perp = new Vector2(-eyePolarization.y, eyePolarization.x);
            Gizmos.DrawSphere(bestProjection, 0.02f);
            Gizmos.DrawLine(bestProjection + 10*perp, bestProjection - 10*perp);            
        }
    }
}
