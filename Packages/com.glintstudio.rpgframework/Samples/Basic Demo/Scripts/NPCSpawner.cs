using System.Collections.Generic;
using RPGFramework.Model;
using RPGFramework.Data;
using UnityEngine;

/// <summary>
/// Componente que permite cargar y configurar un NPC en escena a partir de un CharacterPreset.
/// Se puede usar para cualquier NPC: solo asigna el preset correspondiente desde el inspector.
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    [Header("Preset del NPC")]
    public CharacterPreset preset;

    [Header("Debug")]
    public bool autoSpawnOnStart = true;
    public bool logResult = false;

    [HideInInspector] public RPGCharacterModel npcModel;

    void Start()
    {
        if (autoSpawnOnStart && preset != null)
        {
            SpawnNPC();
        }
    }

    /// <summary>
    /// Instancia el modelo del NPC con los datos del preset y lo deja listo para ser usado por otros sistemas.
    /// </summary>
    public void SpawnNPC()
    {
        if (preset == null)
        {
            Debug.LogWarning($"{gameObject.name} no tiene asignado un CharacterPreset.");
            return;
        }

        var attributesList = new List<AttributeDefinition>(RPGDatabase.Instance.allAttributes);
        var attrValues = new Dictionary<AttributeDefinition, float>();

        foreach (var attr in preset.attributes)
        {
            if (attr?.attribute == null) continue;
            var attrDef = attributesList.Find(a => a == attr.attribute || a.name == attr.attribute.name);
            if (attrDef != null) attrValues[attrDef] = attr.value;
        }

        npcModel = new RPGCharacterModel(
            preset.race,
            preset.subRace,
            preset.classDef,
            preset.subClass,
            attributesList,
            attrValues
        );

        if (logResult)
        {
            Debug.Log($"NPC {preset.characterName} cargado en {gameObject.name}.");
        }

        // Aquí puedes notificar a otros sistemas, asignar IA, etc.
    }
}