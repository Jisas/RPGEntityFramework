using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "DamageTypeDefinition", menuName = "RPG Framework/Damage Type Definition")]
    public class DamageTypeDefinition : ScriptableObject
    {
        public string damageTypeName;
        [TextArea] public string description;
        public Sprite icon;
    }
}