using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "ClassDefinition", menuName = "RPG entity Framework/Class Definition")]
    public class ClassDefinition : RPGDefinition
    {
        [Header("Datos Básicos")]
        public string className;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Subclases Disponibles")]
        public List<SubClassDefinition> availableSubClasses;
    }
}