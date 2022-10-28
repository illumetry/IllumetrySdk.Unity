using UnityEngine;
using UnityEngine.UI;

public class UiSliderMovePlanet : MonoBehaviour
{
   [SerializeField] private PlanetMover planetMover;
   private Slider _slider;

   private void OnEnable()
   {
       if (_slider == null)
       {
           _slider = GetComponent<Slider>();
       }

       _slider.onValueChanged.AddListener(OnChangedSliderValue);
       _slider.value = 0.5f;
   }

   private void OnDisable()
   {
       if (_slider != null)
       {
           _slider.onValueChanged.RemoveListener(OnChangedSliderValue);
       }
   }

   public void OnChangedSliderValue(float val)
   {
       planetMover.UpdateMove(val);
   }
}
