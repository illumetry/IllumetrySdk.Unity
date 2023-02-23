using UnityEngine;

namespace Illumetry.Unity.Demo {
    public class PlanetMover : MonoBehaviour {
        public Vector3 startPosition;
        public Vector3 finalPosition;

        public void UpdateMove(float progress) {
            Vector3 direction = finalPosition - startPosition;
            Vector3 positionPlanet = startPosition + (direction * progress);

            transform.position = positionPlanet;
        }
    }
}