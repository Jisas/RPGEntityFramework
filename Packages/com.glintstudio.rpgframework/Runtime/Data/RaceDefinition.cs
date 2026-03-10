using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "RaceDefinition", menuName = "RPG Framework/Race Definition")]
    public class RaceDefinition : ScriptableObject
    {
        [Header("General")]
        public string raceName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Herencias permitidas")]
        public List<SubRaceDefinition> availableSubRaces;
        public List<ClassDefinition> availableClasses;

        [Header("Habilidades Raciales")]
        public List<AbilityDefinition> racialAbilities = new();
    }
}