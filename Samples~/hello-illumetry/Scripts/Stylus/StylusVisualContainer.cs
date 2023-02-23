using UnityEngine;

namespace Illumetry.Unity.Demo {
    using Illumetry.Unity.Stylus;
    public class StylusVisualContainer : MonoBehaviour {
        public StylusGrabber StylusGrabber => _stylusGrabber;
        [SerializeField] private StylusGrabber _stylusGrabber;

        internal void Inititalize(Stylus stylus) {
            if (stylus == null) {
                if (Application.isEditor || Debug.isDebugBuild) {
                    Debug.LogError("StylusVisualContainer: Initialize failed! Stylus is null!");
                }

                return;
            }

            if (_stylusGrabber != null) {
                _stylusGrabber.SetStylus(stylus);
            }
        }
    }
}