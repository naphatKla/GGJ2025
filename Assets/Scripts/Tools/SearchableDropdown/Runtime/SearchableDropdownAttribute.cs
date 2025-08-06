using System;
using UnityEngine;

namespace Khami.SearchableDropdown
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SearchableDropdownAttribute : PropertyAttribute
    {
        public string MethodName { get; }

        public SearchableDropdownAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
