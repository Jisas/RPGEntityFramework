using System.Collections.Generic;
using RPGEntityFramework.Models;
using System;

/// <summary>
/// Representa una versión serializable del personaje, pensada para guardado/carga.
/// Guarda solo identificadores y valores simples, no referencias a ScriptableObjects.
/// </summary>
[Serializable]
public class SerializableEntityData
{
    public string RaceID;
    public string SubRaceID;
    public string ClassID;
    public string SubClassID;
    public List<SerializableAttributeValue> Attributes = new();
    public List<string> AbilitieIDs = new();
}
