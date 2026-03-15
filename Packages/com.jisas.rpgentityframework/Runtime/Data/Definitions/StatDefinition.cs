using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(menuName = "RPG Entity Framework/Stat Definition")]
    public class StatDefinition : RPGDefinition
    {
        public string statName; // ej: "Max Health", "Physical Damage"
        public float baseValue; // ej: 100

        // Lista de cómo los atributos afectan a este stat
        public List<AttributeScaling> modifiers;
    }

    [System.Serializable]
    public class AttributeScaling
    {
        public AttributeDefinition attribute;
        public float multiplier; // ej: Vitalidad * 10
       public AnimationCurve scalingCurve; // Opcional, para escalados no lineales
    }
}
