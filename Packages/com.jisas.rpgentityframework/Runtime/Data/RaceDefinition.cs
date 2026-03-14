using System.Collections.Generic;
using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "RaceDefinition", menuName = "RPG Entity Framework/Race Definition")]
    public class RaceDefinition : RPGDefinition
    {
        [Header("General")]
        public string raceName;
        [TextArea] public string description;

        [Header("Herencias permitidas")]
        public List<SubRaceDefinition> availableSubRaces;
        public List<ClassDefinition> availableClasses;

        [Header("Habilidades Raciales")]
        public List<AbilityDefinition> racialAbilities = new();
    }
}