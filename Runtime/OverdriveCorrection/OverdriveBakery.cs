using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class OverdriveBakery {
    public static Texture3D Bake(OverdriveDataset dataset, IFunc2d[] functions, int resolution = 16){ 
        int texWidth = 4*resolution;
        int texHeight = 4*resolution;
        int texDepth = resolution;

        float[] data = new float[texWidth*texHeight*texDepth];
        float stepStart = 1.0f/texWidth;
        float stepEnd = 1.0f/texHeight;
        int i = 0;
        for(int z = 0; z < texDepth; ++z){
            var screenPosY = (z + 0.5f)/(texDepth);
            int firstLargerId = Array.BinarySearch(dataset.Timemarks, screenPosY);
            if(firstLargerId < 0) firstLargerId = ~firstLargerId;
            
            for(int iEnd = 0; iEnd < texHeight; ++iEnd){
                var end = stepEnd*(iEnd + 0.5);
                for(int iStart = 0; iStart < texWidth; ++iStart){
                    var start = stepStart*(iStart + 0.5);
                    if(firstLargerId == 0){
                        data[i++] = (float)functions[0].Evaluate(start, end);
                    } else if (firstLargerId >= functions.Length){
                        data[i++] = (float)functions[functions.Length-1].Evaluate(start, end);
                    } else {
                        var b = firstLargerId;
                        var a = b - 1;
                        var k = (screenPosY - dataset.Timemarks[a]) / (dataset.Timemarks[b] - dataset.Timemarks[a]);
                        data[i++] = Mathf.Lerp((float)functions[a].Evaluate(start, end), (float)functions[b].Evaluate(start, end), k);
                    }
                }
            }
        }
        var tex = new Texture3D(texWidth, texHeight, texDepth, TextureFormat.R8, false);
        tex.SetPixelData(data.Select(f => (byte)(255*Mathf.Clamp01(f))).ToArray(), 0);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.Apply();
        return tex;
    }
}
