using System;

/// <summary>
/// Estructura simple para serializar un valor de atributo.
/// </summary>
[Serializable]
public class SerializableAttributeValue
{
    public string AttributeId; // Usualmente el nombre o un campo uniqueID del SO
    public float Value;
}