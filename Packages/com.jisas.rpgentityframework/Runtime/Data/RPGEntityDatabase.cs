using System.Collections.Generic;
using RPGEntityFramework.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityDatabase", menuName = "RPG Entity Framework/Database")]
public class RPGEntityDatabase : ScriptableObject
{
    public List<RaceDefinition> allRaces;
    public List<SubRaceDefinition> allSubRaces;
    public List<ClassDefinition> allClasses;
    public List<SubClassDefinition> allSubClasses;
    public List<AttributeDefinition> allAttributes;
    public List<AbilityDefinition> allAbilities;

    private Dictionary<string, RPGDefinition> _idLookup;

    private static RPGEntityDatabase _instance;
    public static RPGEntityDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<RPGEntityDatabase>("RPGDatabase");
                if (_instance == null)
                    Debug.LogError("No se encontró el asset RPGDatabase en la carpeta Resources.");
            }
            return _instance;
        }
    }

    public void Initialize()
    {
        _idLookup = new Dictionary<string, RPGDefinition>();

        // Registramos todas las categorías en un solo mapa de búsqueda
        RegisterList(allRaces);
        RegisterList(allClasses);
        RegisterList(allAbilities);
        RegisterList(allSubClasses);
        RegisterList(allAttributes);
        RegisterList(allAbilities);
    }
    public T GetDefinitionById<T>(string id) where T : RPGDefinition
    {
        if (_idLookup == null) Initialize();
        if (_idLookup.TryGetValue(id, out var result))
            return result as T;
        return null;
    }

    private void RegisterList<T>(List<T> list) where T : RPGDefinition
    {
        foreach (var item in list)
        {
            if (item != null && !_idLookup.ContainsKey(item.Id))
                _idLookup.Add(item.Id, item);
        }
    }
}