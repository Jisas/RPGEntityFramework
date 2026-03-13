using System.Collections.Generic;
using RPGEntityFramework.Models;
using RPGEntityFramework.Data;
using UnityEngine;

namespace RPGEntityFramework.Builders
{
    public class RPGEntityBuilder
    {
        private RPGEntityModel _model = new();
        private Dictionary<AttributeDefinition, float> _tempAttributes = new();

        public RPGEntityBuilder SetName(string name)
        {
            _model.EntityName = name;
            return this;
        }

        public RPGEntityBuilder WithRace(RaceDefinition race)
        {
            _model.Race = race;
            if (race != null)
            {
                // Añadir habilidades raciales base
                _model.Abilities.AddRange(race.racialAbilities);
            }
            return this;
        }

        public RPGEntityBuilder WithSubRace(SubRaceDefinition subRace)
        {
            _model.SubRace = subRace;
            if (subRace != null)
            {
                // Sumar bonos de atributos de la sub-raza
                _model.Abilities.AddRange(subRace.subRacialAbilities);
                // Aquí podrías sumar los bonusAttributes a la lógica de stats
            }
            return this;
        }

        public RPGEntityBuilder WithClass(ClassDefinition classDef)
        {
            _model.ClassDef = classDef;
            // Lógica para inicializar stats de clase
            return this;
        }

        public RPGEntityModel Build()
        {
            // Aquí realizamos las validaciones finales
            if (_model.Race == null) Debug.LogWarning("Construyendo entidad sin raza.");

            RPGEntityModel finalProduct = _model;

            // Resetear el builder para la siguiente construcción
            _model = new RPGEntityModel();
            return finalProduct;
        }
    }
}
