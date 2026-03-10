using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "RPG Framework/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string weaponName;
        public Sprite icon;

        [Header("Damage")]
        public float damage;
        public DamageTypeDefinition damageType;
    }
}