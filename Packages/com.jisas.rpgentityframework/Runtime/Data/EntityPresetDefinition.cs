using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPreset", menuName = "RPG Entity Framework/Entity Preset")]
public class EntityPresetDefinition : RPGDefinition
{
    public string characterName;
    public RaceDefinition race;
    public SubRaceDefinition subRace;
    public ClassDefinition classDef;
    public SubClassDefinition subClass;
    public List<AttributeValue> attributes;
    public List<AbilityDefinition> extraAbilities;
    // Puedes añadir campos para equipamiento inicial, nivel, aspecto visual, etc.
}

[System.Serializable]
public class AttributeValue
{
    public AttributeDefinition attribute;
    public float value;
}