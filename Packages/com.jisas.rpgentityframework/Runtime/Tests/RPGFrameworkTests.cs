using RPGEntityFramework.Models;
using RPGEntityFramework.Data;
using RPGFramework.Builders;
using UnityEngine;

namespace RPGFramework.Tests
{
    public class RPGFrameworkTests : MonoBehaviour
    {
        [Header("Configuración de Prueba")]
        public RaceDefinition race;
        public SubRaceDefinition subRace;
        public ClassDefinition @class;
        public AttributeDefinition strengthAttribute;

        [ContextMenu("Ejecutar Test de Construcción")]
        public void RunTest()
        {
            Debug.Log("--- Iniciando Test RPG Framework ---");

            // 1. Instanciamos el Builder
            RPGEntityBuilder builder = new RPGEntityBuilder();

            // 2. Construimos un personaje paso a paso
            RPGEntityModel character = builder
                .SetName("Aragorn")
                .WithRace(race)
                .WithSubRace(subRace)
                .WithClass(@class)
                .Build();

            // 3. Verificaciones (Logs)
            Debug.Log($"Personaje Creado: {character.EntityName}");
            Debug.Log($"Raza: {character.Race?.raceName} | Clase: {character.ClassDef?.className}");

            // Verificar Atributos
            if (strengthAttribute != null)
            {
                float totalStrength = character.GetAttributeValue(strengthAttribute);
                Debug.Log($"Fuerza Total Calculada: {totalStrength}");
            }

            // Verificar Habilidades
            Debug.Log($"Total de Habilidades Desbloqueadas: {character.Abilities.Count}");
            foreach (var ability in character.Abilities)
            {
                Debug.Log($"- Habilidad: {ability.abilityName}");
            }

            Debug.Log("--- Test Finalizado ---");
        }
    }
}