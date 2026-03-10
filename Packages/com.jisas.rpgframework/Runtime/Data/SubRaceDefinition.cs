using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "SubRaceDefinition", menuName = "RPG Framework/SubRace Definition")]
    public class SubRaceDefinition : ScriptableObject
    {
        public string subRaceName;
        [TextArea] public string description;
        public List<AbilityDefinition> subRacialAbilities;
        public List<AttributeDefinition> bonusAttributes;
    }
}