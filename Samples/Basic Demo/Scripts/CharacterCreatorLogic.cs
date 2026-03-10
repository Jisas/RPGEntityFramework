using System.Collections.Generic;
using RPGFramework.Model;
using RPGFramework.Data;
using UnityEngine;
using System;

public class CharacterCreatorLogic : MonoBehaviour
{
    public RPGCharacterModel Character { get; private set; }

    public event Action OnCharacterChanged; // Para que la UI se actualice cuando cambie algo

    // Llama esto al iniciar la UI
    public void NewCharacter()
    {
        Character = new RPGCharacterModel(
            RPGDatabase.Instance.allRaces[0],
            RPGDatabase.Instance.allSubRaces[0],
            RPGDatabase.Instance.allClasses[0],
            RPGDatabase.Instance.allClasses[0].availableSubClasses[0],
            new List<AttributeDefinition>(RPGDatabase.Instance.allAttributes), null
        );
        OnCharacterChanged?.Invoke();
    }

    public void SetRace(int raceIndex)
    {
        Character.SetRace(RPGDatabase.Instance.allRaces[raceIndex]);
        OnCharacterChanged?.Invoke();
    }

    public void SetClass(int classIndex)
    {
        var newClass = RPGDatabase.Instance.allClasses[classIndex];

        // Crea un diccionario de valores iniciales aleatorios para los atributos
        var attributes = RPGDatabase.Instance.allAttributes;
        System.Random rng = new System.Random();

        var initialValues = new Dictionary<AttributeDefinition, float>();

        foreach (var attr in attributes)
        {
            float value = rng.Next(1, 11); // 1 a 10 inclusive
            initialValues[attr] = value;
        }

        Character.SetClass(newClass, initialValues);

        OnCharacterChanged?.Invoke();
    }

    public void SetSubClass(int subClassIndex)
    {
        var available = Character.ClassDef.availableSubClasses;

        if (available != null && available.Count > subClassIndex)
            Character.SetSubClass(available[subClassIndex]);

        OnCharacterChanged?.Invoke();
    }

    public void SetAttribute(AttributeDefinition attr, float value)
    {
        if (Character.Attributes.ContainsKey(attr)) Character.Attributes[attr] = value;
        else Character.Attributes.Add(attr, value);

        OnCharacterChanged?.Invoke();
    }

    public void OnConfirmCharacterCreation()
    {
        // 1. Guardar los datos
        GameManager.Instance.SaveCharacter(Character); // serializa y guarda el modelo

        // 2. Notificar al sistema central (opcional)
        GameManager.Instance.SetPlayerCharacter(Character);

        // 3. Avanzar a la siguiente escena o estado
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");

        // (Opcional) Mostrar pantalla de resumen antes de continuar
    }
}