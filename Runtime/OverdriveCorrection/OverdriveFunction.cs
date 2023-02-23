using System;
using System.IO;
using System.Linq;
//using Illumetry.DisplayOverdriveMapBuilder;
using UnityEngine;

namespace Illumetry.Unity {
    public class OverdriveFunction {
        public Texture3D OverdriveMapR, OverdriveMapG, OverdriveMapB;

        public OverdriveFunction() {
            OverdriveMapR = new Texture3D(1, 1, 1, TextureFormat.R8, false);
            OverdriveMapG = new Texture3D(1, 1, 1, TextureFormat.R8, false);
            OverdriveMapB = new Texture3D(1, 1, 1, TextureFormat.R8, false);
        }

        public OverdriveFunction(byte[] data) {

            /*var overdriveLibrary = Illumetry.DisplayOverdriveMapBuilder.Library.load();
        const uint resolutionX = 64;
        const uint resolutionY = 64;
        const uint resolutionZ = 16;

        var baked = overdriveLibrary.bake(data, resolutionX, resolutionY, resolutionZ);

        UpdateTexture((int)resolutionX, (int)resolutionY, (int)resolutionZ, baked.GetData(ColorChannel.R), out OverdriveMapR);
        UpdateTexture((int)resolutionX, (int)resolutionY, (int)resolutionZ, baked.GetData(ColorChannel.G), out OverdriveMapG);
        UpdateTexture((int)resolutionX, (int)resolutionY, (int)resolutionZ, baked.GetData(ColorChannel.B), out OverdriveMapB);
        
        return;
            */
            // TODO: remove old code

            using (var ms = new MemoryStream(data)) {
                using (var br = new BinaryReader(ms)) {
                    try {
                        var dataset = new OverdriveDataset(br);
                        BakeOverdriveTextures(dataset);
                    }
                    catch (Exception e) {
                        UnityEngine.Debug.LogError($"error in OverdriveFunction(byte[] data): {e.Message}");
                    }
                }
            }
        }

        private void UpdateTexture(int textureWidth, int textureHeight, int textureDepth, byte[] data, out Texture3D targetTexture) {
            targetTexture = new Texture3D(textureWidth, textureHeight, textureDepth, TextureFormat.R8, false);
            targetTexture.SetPixelData(data, 0);
            targetTexture.wrapMode = TextureWrapMode.Clamp;
            targetTexture.filterMode = FilterMode.Bilinear;
            targetTexture.Apply();
        }

        public void LoadOverdriveTexturesFromFile(string path) {
            try {
                using (var fs = new FileStream(path, FileMode.Open)) {
                    using (var br = new BinaryReader(fs)) {
                        var dataset = new OverdriveDataset(br);
                        BakeOverdriveTextures(dataset);
                    }
                }
            }
            catch (Exception e) {
                UnityEngine.Debug.LogError($"error in LoadOverdriveTexturesFromFile: {e.Message}");
            }
        }

        private void BakeOverdriveTextures(OverdriveDataset dataset) {
            const int resolution = 16;
            var optimization = new OverdriveOptimization(dataset.NTimemarks);
            optimization.Optimize(dataset);

            var timemarksIds = Enumerable.Range(0, dataset.NTimemarks);
            OverdriveMapR = OverdriveBakery.Bake(dataset, timemarksIds.Select(itm => optimization.Functions[0, itm]).ToArray(), resolution);
            OverdriveMapG = OverdriveBakery.Bake(dataset, timemarksIds.Select(itm => optimization.Functions[1, itm]).ToArray(), resolution);
            OverdriveMapB = OverdriveBakery.Bake(dataset, timemarksIds.Select(itm => optimization.Functions[2, itm]).ToArray(), resolution);
        }

        public void SetToShader(string prefix) {
            Shader.SetGlobalTexture(prefix + "OverdriveMapR", OverdriveMapR);
            Shader.SetGlobalTexture(prefix + "OverdriveMapG", OverdriveMapG);
            Shader.SetGlobalTexture(prefix + "OverdriveMapB", OverdriveMapB);
        }
    }
}