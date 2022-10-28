using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity {

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

        private Display _display = null;

        private void Start() {
            FindDisplay();
        }

        private void Update() {
            if (_display == null) {
                if (!FindDisplay()) {
                    return;
                }
            }
            
            var position = -_display.DisplayProperties.ScreenPosition - _display.DisplayProperties.ScreenX * OriginX - _display.DisplayProperties.ScreenY * OriginY;
                
            var scale = Vector3.one;
            if (ScaleMode != TScaleMode.RealSize) {
                var size = _display.GetHalfScreenSize() * 2;
                if (ScaleMode == TScaleMode.WidthIsOne) {
                    scale = Vector3.one / size.x;
                } else {
                    scale = Vector3.one / size.y;
                }
            }
            _display.transform.localScale = scale;
            position.Scale(scale);
            _display.transform.localPosition = position;
        }

        private bool FindDisplay() {
            _display = GetComponentInChildren<Display>();
            return _display != null;
        }
    }
}