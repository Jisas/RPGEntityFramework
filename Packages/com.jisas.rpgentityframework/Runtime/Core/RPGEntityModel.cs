using System.Collections.Generic;
using RPGEntityFramework.Data;

namespace RPGEntityFramework.Models
{
    public class RPGEntityModel
    {
        public string EntityName { get; internal set; }
        public RaceDefinition RaceDef { get; internal set; }
        public SubRaceDefinition SubRaceDef { get; internal set; }
        public ClassDefinition ClassDef { get; internal set; }
        public SubClassDefinition SubClassDef { get; internal set; }

        public Dictionary<AttributeDefinition, float> Attributes { get; internal set; } = new();
        public List<AbilityDefinition> Abilities { get; internal set; } = new();

        // Constructor interno: solo el Builder debería poder instanciarlo directamente
        internal RPGEntityModel() { }

        public float GetAttributeValue(AttributeDefinition attribute)
        {
            return Attributes.TryGetValue(attribute, out var val) ? val : 0;
        }
    }
}