using System.Collections.Generic;
using RPGEntityFramework.Models;
using RPGEntityFramework.Data;
using UnityEngine;

namespace RPGFramework.Builders
{
    public class RPGEntityBuilder
    {
        private RPGEntityModel _model = new();
        private Dictionary<AttributeDefinition, float> _accumulatedAttributes = new();

        // Bandera para saltarse las validaciones si usamos un Preset
        private bool _isFromPreset = false;

        public RPGEntityBuilder FromPreset(EntityPresetDefinition preset)
        {
            if (preset == null) return this;

            _isFromPreset = true; // Ignoraremos restricciones porque el preset manda

            SetName(preset.characterName)
                .WithRace(preset.race)
                .WithSubRace(preset.subRace)
                .WithClass(preset.classDef)
                .WithSubClass(preset.subClass);

            // Sumar atributos extra del preset
            if (preset.attributes != null)
            {
                foreach (var attrVal in preset.attributes)
                    AddAttributeValue(attrVal.attribute, attrVal.value);
            }

            // Sumar habilidades extra del preset
            if (preset.extraAbilities != null)
            {
                _model.Abilities.AddRange(preset.extraAbilities);
            }

            return this;
        }

        public RPGEntityBuilder SetName(string name)
        {
            _model.EntityName = name;
            return this;
        }
        public RPGEntityBuilder WithRace(RaceDefinition race)
        {
            _model.RaceDef = race;
            if (race != null && race.racialAbilities != null)
            {
                _model.Abilities.AddRange(race.racialAbilities);
            }
            return this;
        }
        public RPGEntityBuilder WithSubRace(SubRaceDefinition subRace)
        {
            if (subRace == null) return this;

            // VALIDACIÓN: ¿La raza actual permite esta sub-raza?
            if (!_isFromPreset && _model.RaceDef != null)
            {
                if (_model.RaceDef.availableSubRaces != null && !_model.RaceDef.availableSubRaces.Contains(subRace))
                {
                    Debug.LogWarning($"[RPG Builder] La raza {_model.RaceDef.raceName} no permite la sub-raza {subRace.subRaceName}. Se ignorará la sub-raza.");
                    return this; // Abortamos la asignación
                }
            }

            _model.SubRaceDef = subRace;
            if (subRace.subRacialAbilities != null)
                _model.Abilities.AddRange(subRace.subRacialAbilities);

            if (subRace.bonusAttributes != null)
            {
                foreach (var bonus in subRace.bonusAttributes)
                    AddAttributeValue(bonus.attribute, bonus.value);
            }

            return this;
        }
        public RPGEntityBuilder WithClass(ClassDefinition classDef)
        {
            if (classDef == null) return this;

            // VALIDACIÓN: ¿La raza actual permite esta clase?
            if (!_isFromPreset && _model.RaceDef != null)
            {
                // Si la lista está vacía, asumimos que puede usar cualquier clase. Si tiene elementos, validamos.
                if (_model.RaceDef.availableClasses != null && _model.RaceDef.availableClasses.Count > 0 && !_model.RaceDef.availableClasses.Contains(classDef))
                {
                    Debug.LogWarning($"[RPG Builder] La raza {_model.RaceDef.raceName} no puede ser de la clase {classDef.className}. Se ignorará la clase.");
                    return this; // Abortamos la asignación
                }
            }

            _model.ClassDef = classDef;
            if (classDef.bonusAttributes != null)
            {
                foreach (var attr in classDef.bonusAttributes)
                    AddAttributeValue(attr.attribute, attr.value);
            }

            return this;
        }
        public RPGEntityBuilder WithSubClass(SubClassDefinition subClass)
        {
            if (subClass == null) return this;

            // VALIDACIÓN: ¿La clase actual permite esta sub-clase?
            if (!_isFromPreset && _model.ClassDef != null)
            {
                if (_model.ClassDef.availableSubClasses != null && !_model.ClassDef.availableSubClasses.Contains(subClass))
                {
                    Debug.LogWarning($"[RPG Builder] La clase {_model.ClassDef.className} no permite la sub-clase {subClass.subClassName}. Se ignorará la sub-clase.");
                    return this; // Abortamos la asignación
                }
            }

            _model.SubClassDef = subClass;
            // Nota: Aquí en el futuro puedes sumar habilidades o stats específicos de la Sub-Clase
            // de la misma manera que lo hicimos con SubRace.

            return this;
        }
        private void AddAttributeValue(AttributeDefinition attr, float value)
        {
            if (attr == null) return;
            if (_accumulatedAttributes.ContainsKey(attr))
                _accumulatedAttributes[attr] += value;
            else
                _accumulatedAttributes[attr] = value;
        }

        public RPGEntityModel Build()
        {
            if (string.IsNullOrEmpty(_model.EntityName)) _model.EntityName = "Unknown Entity";
            if (_model.RaceDef == null) Debug.LogWarning("[RPG Framework] Entidad construida sin Raza.");
            if (_model.ClassDef == null) Debug.LogWarning("[RPG Framework] Entidad construida sin Clase.");

            _model.Attributes = new Dictionary<AttributeDefinition, float>(_accumulatedAttributes);

            RPGEntityModel finalProduct = _model;
            Reset();
            return finalProduct;
        }
        private void Reset()
        {
            _model = new RPGEntityModel();
            _accumulatedAttributes.Clear();
            _isFromPreset = false;
        }
    }
}