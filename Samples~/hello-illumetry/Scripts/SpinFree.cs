using UnityEngine;
namespace Illumetry.Unity.Demo {
    public class SpinFree : MonoBehaviour {
        public Vector3 directionAndSpeed;

        private void Update() {
            Vector3 step = directionAndSpeed * Time.deltaTime;
            transform.rotation *= Quaternion.Euler(step);
        }
    }
}
