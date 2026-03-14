using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "SubRaceDefinition", menuName = "RPG Entity Framework/SubRace Definition")]
    public class SubRaceDefinition : RPGDefinition
    {
        public string subRaceName;
        [TextArea] public string description;

        [Header("Sub-Race Bonuses")]
        public List<AttributeValue> bonusAttributes;
        public List<AbilityDefinition> subRacialAbilities;
    }
}