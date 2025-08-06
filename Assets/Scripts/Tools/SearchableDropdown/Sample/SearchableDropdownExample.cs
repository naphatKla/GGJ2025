using System;
using System.Collections.Generic;
using UnityEngine;

namespace Khami.SearchableDropdown.Examples
{
    public class SearchableDropdownExample : MonoBehaviour
    {
        [Serializable]
        public class NestedClass
        {
            [SearchableDropdown(nameof(GetFoods))]
            public string dropdown;
            [SearchableDropdown(nameof(GetFoods))]
            public List<string> listDropdown;
        }
        
        [SearchableDropdown(nameof(GetFoods))]
        public string dropdown;
        [SearchableDropdown(nameof(GetFoods))]
        public List<string> listDropdown;
        [field: SerializeField, SearchableDropdown(nameof(GetFoods))] 
        public string propertyDropdown { get; set; }

        public NestedClass nestedClass;

        private IEnumerable<string> GetFoods()
        {
            return new List<string>
            {
                "Fruits",
                "Fruits/Citrus",
                "Fruits/Citrus/Orange",
                "Fruits/Citrus/Lemon",
                "Fruits/Berries",
                "Fruits/Berries/Strawberry",
                "Fruits/Berries/Blueberry",
                "Vegetables",
                "Vegetables/Leafy",
                "Vegetables/Leafy/Spinach",
                "Vegetables/Root",
                "Vegetables/Root/Carrot"
            };
        }
    }
}