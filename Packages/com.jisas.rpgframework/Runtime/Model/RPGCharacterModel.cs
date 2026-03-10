using System.Collections.Generic;
using RPGFramework.Data;

namespace RPGFramework.Model
{
    /// <summary>
    /// Modelo base opcional para representar un personaje RPG.
    /// </summary>
    public class RPGCharacterModel
    {
        public RaceDefinition Race { get; private set; }
        public SubRaceDefinition SubRace { get; private set; }
        public ClassDefinition ClassDef { get; private set; }
        public SubClassDefinition SubClass { get; private set; }

        // Los atributos generales del personaje
        public Dictionary<AttributeDefinition, float> Attributes { get; private set; } = new();

        // Habilidades raciales (heredadas de la raza)
        public List<AbilityDefinition> RacialAbilities { get; private set; } = new();

        public RPGCharacterModel(
            RaceDefinition race,
            SubRaceDefinition subRace,
            ClassDefinition classDef,
            SubClassDefinition subClass,
            List<AttributeDefinition> allAttributes,
            Dictionary<AttributeDefinition, float> classAttributeInitialValues)
        {
            this.ClassDef = classDef;
            this.SubClass = subClass;

            // Inicializa atributos generales según la clase
            foreach (var attr in allAttributes)
            {
                if (classAttributeInitialValues != null && classAttributeInitialValues.TryGetValue(attr, out float val))
                    Attributes[attr] = val;
                else
                    Attributes[attr] = 0;
            }

            SetRace(race);
            SetSubRace(subRace);
        }

        public void SetRace(RaceDefinition newRace)
        {
            Race = newRace;
            RacialAbilities.Clear();

            if (Race != null && Race.racialAbilities != null)
                RacialAbilities.AddRange(Race.racialAbilities);
        }

        public void SetSubRace(SubRaceDefinition newSubRace)
        {
            SubRace = newSubRace;

            if (SubRace != null && SubRace.subRacialAbilities != null)
                RacialAbilities.AddRange(SubRace.subRacialAbilities);
        }

        public void SetClass(ClassDefinition newClass, Dictionary<AttributeDefinition, float> classAttributeInitialValues)
        {
            ClassDef = newClass;

            if (classAttributeInitialValues != null)
            {
                foreach (var kvp in classAttributeInitialValues)
                    Attributes[kvp.Key] = kvp.Value;
            }
        }

        public void SetSubClass(SubClassDefinition newSubClass)
        {
            SubClass = newSubClass;
        }

        public float GetAttributeValue(AttributeDefinition attribute)
        {
            return Attributes.TryGetValue(attribute, out var val) ? val : 0;
        }
    }
}