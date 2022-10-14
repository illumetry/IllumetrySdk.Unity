using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace Illumetry.Unity {
    [System.Serializable]
    public class Vector3x3 {
        public Vector3 x = default;
        public Vector3 y = default;
        public Vector3 z = default;

        public Vector3x3() { 
            
        }
        
        public Vector3x3(IEnumerator<float> values) {
            x = values.ReadVector3();
            y = values.ReadVector3();
            z = values.ReadVector3();
        }

        public void SetToShader(string prefix) {
            Shader.SetGlobalVector(prefix + "X", x);
            Shader.SetGlobalVector(prefix + "Y", y);
            Shader.SetGlobalVector(prefix + "Z", z);
        }
    }

    [System.Serializable]
    public class Vector2x3 {
        public Vector2 x = default;
        public Vector2 y = default;
        public Vector2 z = default;

        public Vector2x3() { 
            
        }

        public Vector2x3(IEnumerator<float> values) {
            x = values.ReadVector2();
            y = values.ReadVector2();
            z = values.ReadVector2();
        }

        public void SetToShader(string prefix) {
            Shader.SetGlobalVector(prefix + "X", x);
            Shader.SetGlobalVector(prefix + "Y", y);
            Shader.SetGlobalVector(prefix + "Z", z);
        }
    }

    [System.Serializable]
    public class GammaFunction {
        public static readonly string PropertyPath = "sys/GammaFunction";
        public Vector3x3 GrayValLinear, GrayValSquared, GrayValCross;
        public Vector2x3 LimitsLinear, LimitsSquared, LimitsCross;

        public GammaFunction() {
            GrayValLinear = new Vector3x3();
            GrayValSquared = new Vector3x3 { x = Vector3.one, y = Vector3.one, z = Vector3.one };
            GrayValCross = new Vector3x3();

            LimitsLinear = new Vector2x3();
            LimitsSquared = new Vector2x3();
            LimitsCross = new Vector2x3();
        }

        public GammaFunction(IEnumerable<float> values) {
            var enumerator = values.GetEnumerator();
            GrayValLinear = new Vector3x3(enumerator);
            GrayValSquared = new Vector3x3(enumerator);
            GrayValCross = new Vector3x3(enumerator);
            LimitsLinear = new Vector2x3(enumerator);
            LimitsSquared = new Vector2x3(enumerator);
            LimitsCross = new Vector2x3(enumerator);
        }

        public void SetToShader(string prefix) {
            /*var sb = new StringBuilder();
            sb.AppendLine($"v: {Linear.x.x}, {Linear.y.x}, {Linear.z.x}");
            sb.AppendLine($"v2: {Squared.x.x}, {Squared.y.x}, {Squared.z.x}");
            sb.AppendLine($"c2: {Cross.x.x}, {Cross.y.x}, {Cross.z.x}");
            UnityEngine.Debug.Log(sb.ToString());*/

            GrayValLinear.SetToShader(prefix + "GammaFunction_Linear");
            GrayValSquared.SetToShader(prefix + "GammaFunction_Squared");
            GrayValCross.SetToShader(prefix + "GammaFunction_Cross");

            LimitsLinear.SetToShader(prefix + "GammaLimits_Linear");
            LimitsSquared.SetToShader(prefix + "GammaLimits_Squared");
            LimitsCross.SetToShader(prefix + "GammaLimits_Cross");
        }
    }
}
