using System.Collections;
using System.Collections.Generic;
using UI.DotNotify;
using UnityEngine;

namespace DotNotify
{
    public class RedDotView : MonoBehaviour
    {
        [SerializeField] private string keyOrPrefix; // "Map" "Map:Desert"
        [SerializeField] private GameObject dot;

        public string KeyDot
        {
            get => keyOrPrefix;
            set
            {
                keyOrPrefix = value;
                Refresh();
            }
        }

        private void OnEnable()
        {
            RedDotService.OnChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            RedDotService.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            dot.SetActive(
                keyOrPrefix.Contains(":")
                    ? RedDotService.Instance.Has(keyOrPrefix)
                    : RedDotService.Instance.HasPrefix(keyOrPrefix)
            );
        }
    }

}