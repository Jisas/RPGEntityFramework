using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "ClassDefinition", menuName = "RPG Framework/Class Definition")]
    public class ClassDefinition : ScriptableObject
    {
        [Header("Datos Básicos")]
        public string className;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Subclases Disponibles")]
        public List<SubClassDefinition> availableSubClasses;
    }
}