using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(menuName = "RPG Entity Framework/Ability Definition")]
    public class AbilityDefinition : RPGDefinition
    {
        public string abilityName;
        [TextArea] public string description;

        [Header("Caracteristicas de la Habilidad")]
        public DamageTypeDefinition damageType;
        public float damage;
        public float duration; // En segundos
        public float cooldown; // En segundos
        public float manaCost;
        public float staminaCost;

        [Header("Alcance y Area")]
        public float range = 1f; // Alcance en unidades del juego
        public float areaRadius = 0f; // Si es AOE, radio en unidades

        [Header("Animaciones")]
        public string animationTrigger;

        [Header("VFX y SFX")]
        public GameObject vfxPrefab;
        public AudioClip soundEffect;
    }
}