using System.Collections.Generic;
using RPGFramework.Data;
using UnityEngine;

[CreateAssetMenu(fileName = "RPGDatabase", menuName = "RPG Framework/Database")]
public class RPGDatabase : ScriptableObject
{
    public List<RaceDefinition> allRaces;
    public List<SubRaceDefinition> allSubRaces;
    public List<ClassDefinition> allClasses;
    public List<SubClassDefinition> allSubClasses;
    public List<AttributeDefinition> allAttributes;
    public List<AbilityDefinition> allAbilities;

    private static RPGDatabase _instance;
    public static RPGDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<RPGDatabase>("RPGDatabase");
                if (_instance == null)
                    Debug.LogError("No se encontró el asset RPGDatabase en la carpeta Resources.");
            }
            return _instance;
        }
    }
}