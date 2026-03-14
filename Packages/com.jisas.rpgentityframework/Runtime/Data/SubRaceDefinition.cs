using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "SubRaceDefinition", menuName = "RPG Entity Framework/SubRace Definition")]
    public class SubRaceDefinition : RPGDefinition
    {
        public string subRaceName;
        [TextArea] public string description;
        public List<AbilityDefinition> subRacialAbilities;
        public List<AttributeValue> bonusAttributes;
    }
}