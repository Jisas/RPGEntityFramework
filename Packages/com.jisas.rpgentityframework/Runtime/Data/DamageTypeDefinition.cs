using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "DamageTypeDefinition", menuName = "RPG Entity Framework/Damage Type Definition")]
    public class DamageTypeDefinition : RPGDefinition
    {
        public string damageTypeName;
        [TextArea] public string description;

    }
}