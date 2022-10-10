using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity {

    [Serializable]
    public class DisplayProperties {
        public static readonly string ShaderParameterPrefix = "Illumetry_";

        public Vector3 ScreenPosition;
        public Vector3 ScreenX;
        public Vector3 ScreenY;
        public float? ScreenPolarizationAngle;
        public Vector2Int Resolution;
        public Vector2Int Blank;
        public float Fps;
        public int StrobeOffset;
        public int StrobeDuration;
        public int EnvironmentVariant;
        public string[] Environments;

        public string CurrentEnvironment => Environments[EnvironmentVariant % Environments.Length];

        public OverdriveFunction OverdriveFunction;
        public GammaFunction GammaFunction;

        public DisplayProperties() {
            SetDefaultProperties_IllumetryIo(this);
        }

        public DisplayProperties(Antilatency.DeviceNetwork.INetwork network, Antilatency.DeviceNetwork.NodeHandle node) {
            UnityEngine.Debug.Log("DisplayProperties");
            using var propertiesReader = new AdnPropertiesReader(network, node);

            ScreenPosition = propertiesReader.TryRead("sys/ScreenPosition", AdnPropertiesReader.ReadVector3).Value;
            ScreenX = propertiesReader.TryRead("sys/ScreenX", AdnPropertiesReader.ReadVector3).Value;
            ScreenY = propertiesReader.TryRead("sys/ScreenY", AdnPropertiesReader.ReadVector3).Value;

            Resolution = propertiesReader.TryRead("sys/Resolution", AdnPropertiesReader.ReadVector2Int).Value;
            Blank = propertiesReader.TryRead("sys/Blank", AdnPropertiesReader.ReadVector2Int).Value;
            Fps = propertiesReader.TryRead("sys/Fps", AdnPropertiesReader.ReadFloat).Value;

            StrobeOffset = propertiesReader.TryRead("sys/StrobeOffset", AdnPropertiesReader.ReadInt).Value;
            StrobeDuration = propertiesReader.TryRead("sys/StrobeDuration", AdnPropertiesReader.ReadInt).Value;

            EnvironmentVariant = propertiesReader.TryRead("EnvironmentVariant", AdnPropertiesReader.ReadInt).Value;

            List<string> environments = new();
            while (true) {
                try {
                    var environment = network.nodeGetStringProperty(node, $"sys/Environment{environments.Count}");
                    environments.Add(environment);
                }
                catch (Exception ) {
                    break;
                }
            }

            Environments = environments.ToArray();     
            try {
                OverdriveFunction = new OverdriveFunction(network.nodeGetBinaryProperty(node, "sys/PixelResponseFunction.b"));
            } catch (Exception ex) {
                Debug.LogWarning(ex);
            }
            
            try {
                GammaFunction = new GammaFunction(propertiesReader.Read(GammaFunction.PropertyPath, AdnPropertiesReader.ReadFloatArray));
            } catch (Exception ex) {
                Debug.LogWarning(ex);
            }

            ScreenPolarizationAngle = propertiesReader.TryRead("sys/ScreenPolarizationAngle", AdnPropertiesReader.ReadFloat);
        }


        public static void SetDefaultProperties_IllumetryIo(DisplayProperties properties) {
            properties.ScreenPosition.x = 0;
            properties.ScreenPosition.y = 0.15f;
            properties.ScreenPosition.z = 0.003f;

            properties.ScreenX.x = 0.26568f;
            properties.ScreenX.y = 0;
            properties.ScreenX.z = 0;

            properties.ScreenY.x = 0;
            properties.ScreenY.y = 0.14944f;
            properties.ScreenY.z = 0;

            properties.Resolution.x = 1920;
            properties.Resolution.y = 1080;

            properties.Blank.x = 132;
            properties.Blank.y = 600;

            properties.Fps = 120;
            properties.StrobeOffset = 1360;
            properties.StrobeDuration = 300;
            properties.EnvironmentVariant = 0;

            properties.Environments = new string[] {
                "AntilatencyAltEnvironmentAdditionalMarkers~AAhCYGW9HqdovAAAAAAxCCy-HqdovAAAAABCYGU9HqdovAAAAAAxCCw-HqdovAAAAABCYGW9096gPgAAAAAxCCy-096gPgAAAABCYGU9096gPgAAAAAxCCw-096gPgAAAAB7QW50aWxhdGVuY3lBbHRFbnZpcm9ubWVudFBpbGxhcnN-QVFMT3FvLS1BQUFBQUFBQUFBQUE5MENBdmdZQUFEX09xbzgtQUFBQUFBQUFBQUFBM2ZCX3ZnUUFBRDhEbXBtWlBnSUJ6Y3pNUGdHYW1Sa19Cbk5qYUdWdFpR",
                "AntilatencyAltEnvironmentAdditionalMarkers~AAhCYGW9HqdovAAAAAAxCCy-HqdovAAAAABCYGU9HqdovAAAAAAxCCw-HqdovAAAAABCYGW9096gPgAAAAAxCCy-096gPgAAAABCYGU9096gPgAAAAAxCCw-096gPgAAAAB7QW50aWxhdGVuY3lBbHRFbnZpcm9ubWVudFBpbGxhcnN-QVFMT3FvLS1BQUFBQUFBQUFBQUE5MENBdmdZQUFEX09xbzgtQUFBQUFBQUFBQUFCM2ZCX3ZnUUFBRDhEbXBtWlBnSUJ6Y3pNUGdHYW1Sa19Cbk5qYUdWdFpR",
                "AntilatencyAltEnvironmentAdditionalMarkers~AAhCYGW9HqdovAAAAAAxCCy-HqdovAAAAABCYGU9HqdovAAAAAAxCCw-HqdovAAAAABCYGW9096gPgAAAAAxCCy-096gPgAAAABCYGU9096gPgAAAAAxCCw-096gPgAAAAB7QW50aWxhdGVuY3lBbHRFbnZpcm9ubWVudFBpbGxhcnN-QVFMT3FvLS1BQUFBQUFBQUFBQUI5MENBdmdZQUFEX09xbzgtQUFBQUFBQUFBQUFBM2ZCX3ZnUUFBRDhEbXBtWlBnSUJ6Y3pNUGdHYW1Sa19Cbk5qYUdWdFpR",
                "AntilatencyAltEnvironmentAdditionalMarkers~AAhCYGW9HqdovAAAAAAxCCy-HqdovAAAAABCYGU9HqdovAAAAAAxCCw-HqdovAAAAABCYGW9096gPgAAAAAxCCy-096gPgAAAABCYGU9096gPgAAAAAxCCw-096gPgAAAAB7QW50aWxhdGVuY3lBbHRFbnZpcm9ubWVudFBpbGxhcnN-QVFMT3FvLS1BQUFBQUFBQUFBQUI5MENBdmdZQUFEX09xbzgtQUFBQUFBQUFBQUFCM2ZCX3ZnUUFBRDhEbXBtWlBnSUJ6Y3pNUGdHYW1Sa19Cbk5qYUdWdFpR"
            };

            properties.GammaFunction = new GammaFunction(
                new float[] {
                    0,0,0,
                    0,0,0,
                    0,0,0,
                
                    1,1,1,
                    1,1,1,
                    1,1,1,
                
                    0,0,0,
                    0,0,0,
                    0,0,0,
                
                    0,0,
                    0,0,
                    0,0,
                
                    0,0,
                    0,0,
                    0,0,
                
                    0,0,
                    0,0,
                    0,0
                }
            );

            properties.OverdriveFunction = null;
            properties.ScreenPolarizationAngle = null;

        }

        public void SetToShader() {
            GammaFunction?.SetToShader(ShaderParameterPrefix);
            OverdriveFunction?.SetToShader(ShaderParameterPrefix);

            //Shader.SetGlobalVector(ShaderParameterPrefix + "DisplayResolution", new Vector2(Resolution.x, Resolution.y));
            //Shader.SetGlobalVector(ShaderParameterPrefix + "DisplayBlank", new Vector2(Blank.x, Blank.y));
            //Shader.SetGlobalFloat(ShaderParameterPrefix + "DisplayFps", Fps);
            //float oneLineDuration = 1f / ((Resolution.y + Blank.y) * Fps);
            //Shader.SetGlobalFloat(ShaderParameterPrefix + "DisplayActivePixelsTime", oneLineDuration * Resolution.y);
            //Shader.SetGlobalFloat(ShaderParameterPrefix + "DisplayStrobeMeanTime", oneLineDuration * (StrobeOffset + StrobeDuration/2));
        }
    }
}
