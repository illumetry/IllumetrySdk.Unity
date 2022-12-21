using System.Collections.Generic;
using UnityEngine;

namespace Illumetry.Unity.Demo
{
    using Illumetry.Unity.Stylus;

    public class StylusVisualPackInstaller : MonoBehaviour
    {
        [SerializeField] private StylusVisualSetter visualPack;
        private List<GameObject> _createdPacks = new List<GameObject>();

        private void OnEnable()
        {
            Stylus[] styluses = StylusesCreator.Styluses;

            foreach (var stylus in styluses)
            {
                if (stylus != null)
                {
                    AddVisualPackStylus(stylus);
                }
            }

            StylusesCreator.OnCreatedStylus += AddVisualPackStylus;
        }

        private void OnDisable()
        {
            StylusesCreator.OnCreatedStylus -= AddVisualPackStylus;
            RemoveAllCreatedPacks();
        }

        private void OnDestroy()
        {
            RemoveAllCreatedPacks();
        }

        private void AddVisualPackStylus(Stylus stylus)
        {
            CheckNullAndRemovePacks();
            GameObject instanceVisualPack = Instantiate(visualPack.gameObject);
            instanceVisualPack.transform.parent = stylus.transform;
            instanceVisualPack.transform.localPosition = Vector3.zero;
            instanceVisualPack.transform.localRotation = Quaternion.Euler(Vector3.zero);
            instanceVisualPack.transform.localScale = Vector3.one;

            instanceVisualPack.GetComponent<StylusVisualSetter>().SetStylus(stylus);
            instanceVisualPack.SetActive(true);

            _createdPacks.Add(instanceVisualPack);
        }

        private void CheckNullAndRemovePacks()
        {
            for (int i = _createdPacks.Count - 1; i >= 0; i--)
            {
                if (_createdPacks[i] == null)
                {
                    _createdPacks.RemoveAt(i);
                }
            }
        }

        private void RemoveAllCreatedPacks()
        {
            foreach (var createdPack in _createdPacks)
            {
                if (createdPack != null)
                {
                    Destroy(createdPack);
                }
            }

            _createdPacks.Clear();
        }
    }
}