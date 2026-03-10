using System.Collections.Generic;
using RPGFramework.Model;
using UnityEngine;
using System.IO;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public RPGCharacterModel PlayerCharacter { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    public void SetPlayerCharacter(RPGCharacterModel character)
    {
        PlayerCharacter = character;
    }

    /// <summary>
    /// Guardado de datos de personaje en un archivo JSON.
    /// </summary>
    public void SaveCharacter(RPGCharacterModel character)
    {
        // Serializa solo los identificadores y valores de atributos
        var data = new SerializableCharacterData(character);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Application.persistentDataPath + "/character.json", json);
    }

    /// <summary>
    /// Carga de datos de personaje desde un archivo JSON.
    /// </summary>
    /// <param name="path">
    ///     Ruta al archivo de tipo JSON dentro de los datos persistentes.
    ///     Ejemplo: "Application.persistentDataPath + "/character.json"
    /// </param>
    /// <param name="allRaces">
    ///     Array con todas las definiciones de razas posibles (RaceDefinition) disponibles en el juego.
    /// </param>
    /// <param name="allSubRaces">
    ///     Array con todas las definiciones de sub-razas posibles (SubRaceDefinition) disponibles en el juego.
    /// </param>
    /// <param name="allClasses">
    ///     Array con todas las definiciones de claases posibles (ClassDefinition) disponibles en el juego.
    /// </param>
    /// <param name="allSubClasses">
    ///     Array con todas las definiciones de sub-clases posibles (SubClassDefinition) disponibles en el juego.
    /// </param>
    /// <param name="allAttributes">
    ///     Array con todas las definiciones de atributos posibles (AttributeDefinition) disponibles en el juego.
    /// </param>
    public void LoadPlayerCharacter(string path,
        RPGFramework.Data.RaceDefinition[] allRaces,
        RPGFramework.Data.SubRaceDefinition[] allSubRaces,
        RPGFramework.Data.ClassDefinition[] allClasses,
        RPGFramework.Data.SubClassDefinition[] allSubClasses,
        RPGFramework.Data.AttributeDefinition[] allAttributes)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning("[GameManager] Archivo de personaje no encontrado");
            return;
        }

        string json = System.IO.File.ReadAllText(path);
        var data = JsonUtility.FromJson<SerializableCharacterData>(json);

        // Reconstrucción del modelo usando los SOs
        var race = System.Array.Find(allRaces, r => r.name == data.RaceId);
        var subRace = System.Array.Find(allSubRaces, r => r.name == data.SubRaceId);
        var classDef = System.Array.Find(allClasses, c => c.name == data.ClassId);
        var subClass = System.Array.Find(allSubClasses, s => s.name == data.SubClassId);

        var attributes = new List<RPGFramework.Data.AttributeDefinition>(allAttributes);
        var attrValues = new Dictionary<RPGFramework.Data.AttributeDefinition, float>();

        foreach (var attr in data.Attributes)
        {
            var attrDef = System.Array.Find(allAttributes, a => a.name == attr.AttributeId);
            if (attrDef != null)
                attrValues[attrDef] = attr.Value;
        }

        var model = new RPGCharacterModel(race, subRace, classDef, subClass, attributes, attrValues);
        SetPlayerCharacter(model);
    }

    /// <summary>
    /// Carga un CharacterPreset desde la carpeta Resources y crea un modelo de personaje (RPGCharacterModel) basado en sus datos.
    /// </summary>
    /// <param name="resourcePath">
    ///     Ruta relativa al asset de tipo CharacterPreset dentro de la carpeta 'Resources', sin extensión.
    ///     Ejemplo: "CharacterPresets/NPC_Guard"
    /// </param>
    /// <param name="allAttributes">
    ///     Array con todas las definiciones de atributos posibles (AttributeDefinition) disponibles en el juego.
    /// </param>
    public void LoadPlayerFromPreset(string resourcePath, RPGFramework.Data.AttributeDefinition[] allAttributes)
    {
        CharacterPreset preset = Resources.Load<CharacterPreset>(resourcePath);

        if (preset == null)
        {
            Debug.LogWarning("[GameManager] No se encontró el CharacterPreset en Resources: " + resourcePath);
            return;
        }

        var race = preset.race;
        var subRace = preset.subRace;
        var classDef = preset.classDef;
        var subClass = preset.subClass;
        var attributes = new List<RPGFramework.Data.AttributeDefinition>(allAttributes);
        var attrValues = new Dictionary<RPGFramework.Data.AttributeDefinition, float>();

        foreach (var attr in preset.attributes)
        {
            if (attr?.attribute == null) continue;

            // Busca por name o como corresponda según tu definición de AttributeDefinition
            var attrDef = System.Array.Find(allAttributes, a => a == attr.attribute);
            if (attrDef != null) attrValues[attrDef] = attr.value;
        }

        var model = new RPGCharacterModel(race, subRace, classDef, subClass, attributes, attrValues);
        SetPlayerCharacter(model);
    }
}