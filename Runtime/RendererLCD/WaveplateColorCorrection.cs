using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Illumetry.Unity {
public class WaveplateColorCorrection : MonoBehaviour {
    private const float _WL_R = 560;
    private const float _WL_G = 510;
    private const float _WL_B = 430;
    private const float _delay = _WL_G / 4;

    private readonly Vector3 _phaseShift = 2 * Mathf.PI * new Vector3(_delay / _WL_R, _delay / _WL_G, _delay / _WL_B);
    
    [HideInInspector][Range(-180, 180)] public float ScreenPolarizationDeg = 0;
    [HideInInspector][Range(-180, 180)] public float EyePolarizationAngleDeg = 90;
    [HideInInspector][Range(-180, 180)] public float WaveplateAngleDeg = 45;

    [HideInInspector] public Vector3 Transmittance = Vector3.one;
    [HideInInspector] public Quaternion GlassesRotation = Quaternion.identity;

    public Vector3 GetTransmittance() {
        var screenPolarizationAngle = Mathf.Deg2Rad * ScreenPolarizationDeg;
        var waveplateAngle = Mathf.Deg2Rad * WaveplateAngleDeg;
        var eyePolarizationAngle = Mathf.Deg2Rad * EyePolarizationAngleDeg;
        var eyePolarization = new Vector2(Mathf.Cos(eyePolarizationAngle), Mathf.Sin(eyePolarizationAngle));

        var screenPolarization = new Vector2(Mathf.Cos(screenPolarizationAngle), Mathf.Sin(screenPolarizationAngle));
        var screenPolarizationGlassesSpace3 = Quaternion.Inverse(GlassesRotation) * screenPolarization;
        var screenPolarizationGlassesSpace =
            new Vector2(screenPolarizationGlassesSpace3.x, screenPolarizationGlassesSpace3.y);

        var fast = new Vector2(Mathf.Cos(waveplateAngle), Mathf.Sin(waveplateAngle));
        var slow = new Vector2(Mathf.Cos(waveplateAngle + 0.5f * Mathf.PI), Mathf.Sin(waveplateAngle + 0.5f * Mathf.PI));
        var A = Vector2.Dot(fast, eyePolarization) * Vector2.Dot(screenPolarizationGlassesSpace, fast);
        var B = Vector2.Dot(slow, eyePolarization) * Vector2.Dot(screenPolarizationGlassesSpace, slow);

        Func<float, int, float> func = (phi, color) => A * Mathf.Cos(phi) + B * Mathf.Cos(phi + _phaseShift[color]);
        Func<float, int, float> df_dphi = (phi, color) => -A * Mathf.Sin(phi) - B * Mathf.Sin(phi + _phaseShift[color]);
        Func<float, int, float> d2f_dphi2 = (phi, color) => -A * Mathf.Cos(phi) - B * Mathf.Cos(phi + _phaseShift[color]);

        for (int color = 0; color < 3; ++color) {
            float bestPhi = 0;
            for (int i = 0; i < 10; ++i) {
                var delta = df_dphi(bestPhi, color) / d2f_dphi2(bestPhi, color);
                if (Mathf.Abs(delta) < 0.001) {
                    break;
                }

                bestPhi -= delta;
            }

            Transmittance[color] = Math.Abs(func(bestPhi, color));
        }

        return Transmittance;
    }
}
}