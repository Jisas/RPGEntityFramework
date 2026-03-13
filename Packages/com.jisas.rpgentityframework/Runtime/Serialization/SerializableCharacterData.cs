using System.Collections.Generic;
using RPGEntityFramework.Models;
using System;

/// <summary>
/// Representa una versión serializable del personaje, pensada para guardado/carga.
/// Guarda solo identificadores y valores simples, no referencias a ScriptableObjects.
/// </summary>
[Serializable]
public class SerializableCharacterData
{
    public string Race;
    public string SubRace;
    public string Class;
    public string SubClass;
    public List<SerializableAttributeValue> Attributes = new();
    public List<string> Abilities = new();
}
