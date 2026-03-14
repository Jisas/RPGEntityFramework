using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "ClassDefinition", menuName = "RPG Entity Framework/Class Definition")]
    public class ClassDefinition : RPGDefinition
    {
        [Header("Datos Básicos")]
        public string className;
        [TextArea] public string description;

        [Header("Atributos Base")]
        public List<AttributeValue> baseAttributes;

        [Header("Subclases Disponibles")]
        public List<SubClassDefinition> availableSubClasses;
    }
}