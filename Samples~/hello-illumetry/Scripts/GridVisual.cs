using UnityEngine;
using UnityEngine.UI;

namespace Illumetry.Unity.Demo {
    public class GridVisual : MonoBehaviour {
        [SerializeField] private GameObject grid;
        private Toggle _toggle;

        private void OnEnable() {
            if (_toggle == null) {
                _toggle = GetComponent<Toggle>();
            }

            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            _toggle.isOn = false;
        }

        private void OnDisable() {
            if (_toggle != null) {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }

        private void OnToggleValueChanged(bool val) {
            grid.SetActive(!val);
        }
    }
}