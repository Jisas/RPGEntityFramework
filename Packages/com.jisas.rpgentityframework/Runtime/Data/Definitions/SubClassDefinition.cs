using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "SubClassDefinition", menuName = "RPG Entity Framework/SubClass Definition")]
    public class SubClassDefinition : RPGDefinition
    {
        public string subClassName;
        [TextArea] public string description;

        [Header("Bonus Attributes")]
        public List<AttributeValue> bonusAttributes;
    }
}