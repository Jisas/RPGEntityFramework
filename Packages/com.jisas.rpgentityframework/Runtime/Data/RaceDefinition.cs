using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "RaceDefinition", menuName = "RPG Entity Framework/Race Definition")]
    public class RaceDefinition : RPGDefinition
    {
        public string raceName;
        [TextArea] public string description;

        [Header("Available Inheritances")]
        public List<SubRaceDefinition> availableSubRaces;
        public List<ClassDefinition> availableClasses;

        [Header("Racial Abilities")]
        public List<AbilityDefinition> racialAbilities = new();
    }
}