using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity.DisplayHandle {

    [ExecuteAlways]
    public class DisplayHandle : MonoBehaviour {
        [Range(-1,1)]
        public int OriginX = 0;

        [Range(-1, 1)]
        public int OriginY = -1;

        public enum TScaleMode {
            RealSize,
            WidthIsOne,
            HeightIsOne,
        }

        public TScaleMode ScaleMode;


        void Update() {
            var display = GetComponentInChildren<Display>();
            if (display) {
                var position = -display.DisplayProperties.ScreenPosition - display.DisplayProperties.ScreenX * OriginX - display.DisplayProperties.ScreenY * OriginY;
                
                var scale = Vector3.one;
                if (ScaleMode != TScaleMode.RealSize) {
                    var size = display.GetHalfScreenSize() * 2;
                    if (ScaleMode == TScaleMode.WidthIsOne) {
                        scale = Vector3.one / size.x;
                    } else {
                        scale = Vector3.one / size.y;
                    }
                }
                display.transform.localScale = scale;
                position.Scale(scale);
                display.transform.localPosition = position;
            }
        }
    }
}