using RPGFramework.Model;
using UnityEngine;

public class CharacterDataTest : MonoBehaviour
{
    private void Start()
    {
        // Intentar cargar personaje guardado
        string path = Application.persistentDataPath + "/character.json";

        GameManager.Instance.LoadPlayerCharacter(
            path, 
            RPGDatabase.Instance.allRaces.ToArray(),
            RPGDatabase.Instance.allSubRaces.ToArray(),
            RPGDatabase.Instance.allClasses.ToArray(), 
            RPGDatabase.Instance.allSubClasses.ToArray(), 
            RPGDatabase.Instance.allAttributes.ToArray()
        );

        // Si no hay personaje, crear uno de prueba
        if (GameManager.Instance.PlayerCharacter == null)
        {
            var race = RPGDatabase.Instance.allRaces[0];
            var subRace = RPGDatabase.Instance.allSubRaces[0];
            var classDef = RPGDatabase.Instance.allClasses[0];
            var subClass = classDef.availableSubClasses.Count > 0 ? classDef.availableSubClasses[0] : null;
            var attrValues = new System.Collections.Generic.Dictionary<RPGFramework.Data.AttributeDefinition, float>();

            foreach (var attr in RPGDatabase.Instance.allAttributes)
                attrValues[attr] = 10;

            var model = new RPGCharacterModel(
                race,
                subRace,
                classDef, 
                subClass, 
                new System.Collections.Generic.List<RPGFramework.Data.AttributeDefinition>(RPGDatabase.Instance.allAttributes), attrValues
            );

            GameManager.Instance.SetPlayerCharacter(model);
        }

        ShowCharacterSummary();
    }

    public void ShowCharacterSummary()
    {
        var character = GameManager.Instance.PlayerCharacter;

        if (character == null)
        {
            Debug.Log("No hay personaje cargado.");
            return;
        }

        Debug.Log("-- Datos del personaje --");
        Debug.Log($"Raza: {character.Race.raceName}");
        Debug.Log($"SunRaza: {character.SubRace.subRaceName}");
        Debug.Log($"Clase: {character.ClassDef.className}");
        Debug.Log($"Subclase: {character.SubClass?.subClassName ?? "(ninguna)"}");

        Debug.Log("-- Atributos --");
        foreach (var attr in character.Attributes)
            Debug.Log($"{attr.Key.attributeName}: {attr.Value}");

        Debug.Log("-- Habilidades raciales --");
        foreach (var ability in character.RacialAbilities)
            Debug.Log(ability.abilityName);
    }
}