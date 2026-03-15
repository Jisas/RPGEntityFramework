using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPreset", menuName = "RPG Entity Framework/Entity Preset")]
public class EntityPresetDefinition : RPGDefinition
{
    public string characterName;
    [Space(10)]
    public RaceDefinition race;
    public SubRaceDefinition subRace;
    public ClassDefinition classDef;
    public SubClassDefinition subClass;
    [Space(10)]
    public List<AttributeValue> attributes;
    public List<AbilityDefinition> extraAbilities;
}