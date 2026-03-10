using System.Collections.Generic;
using RPGFramework.Model;
using System;

/// <summary>
/// Representa una versión serializable del personaje, pensada para guardado/carga.
/// Guarda solo identificadores y valores simples, no referencias a ScriptableObjects.
/// </summary>
[Serializable]
public class SerializableCharacterData
{
    public string RaceId;
    public string SubRaceId;
    public string ClassId;
    public string SubClassId;
    public List<SerializableAttributeValue> Attributes = new();
    public List<string> RacialAbilityIds = new();

    public SerializableCharacterData() { }

    public SerializableCharacterData(RPGCharacterModel model)
    {
        RaceId = model.Race != null ? model.Race.name : null;
        SubRaceId = model.SubRace != null ? model.SubRace.name : null;
        ClassId = model.ClassDef != null ? model.ClassDef.name : null;
        SubClassId = model.SubClass != null ? model.SubClass.name : null;

        // Serializar atributos
        foreach (var kvp in model.Attributes)
        {
            Attributes.Add(new SerializableAttributeValue
            {
                AttributeId = kvp.Key != null ? kvp.Key.name : null,
                Value = kvp.Value
            });
        }

        // Serializar habilidades raciales
        foreach (var ability in model.RacialAbilities)
        {
            if (ability != null)
                RacialAbilityIds.Add(ability.name);
        }
    }
}
