using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "ClassDefinition", menuName = "RPG Entity Framework/Class Definition")]
    public class ClassDefinition : RPGDefinition
    {
        public string className;
        [TextArea] public string description;

        [Header("Base Attributes")]
        public List<AttributeValue> baseAttributes;

        [Header("Available Sub-Classes")]
        public List<SubClassDefinition> availableSubClasses;
    }
}