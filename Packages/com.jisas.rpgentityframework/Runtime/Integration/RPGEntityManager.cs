using RPGEntityFramework.Models;
using RPGEntityFramework.Data;
using RPGFramework.Builders;
using UnityEngine;

namespace RPGEntityFramework.Runtime
{
    [DefaultExecutionOrder(-10)] // Nos aseguramos de que esté listo antes que otros scripts
    public class RPGEntityManager : MonoBehaviour
    {
        [Header("Entity Configuration")]
        [SerializeField] private EntityPresetDefinition _initialPreset;
        [SerializeField] private bool _buildOnStart = true;

        // La "Fuente de la Verdad" en Runtime
        private RPGEntityModel _entityModel;

        // Evento para avisar a otros sistemas (UI, Inventario) que los datos han cambiado
        public event System.Action OnEntityInitialized;

        public RPGEntityModel EntityModel => _entityModel;

        private void Start()
        {
            if (_buildOnStart && _initialPreset != null)
            {
                BuildFromPreset(_initialPreset);
            }
        }

        #region INTEGRATION API

        /// <summary>
        /// Build or rebuild the entity using a Preset.
        /// </summary>
        /// <param name="preset"> Preset definition for static default entity </param>
        public void BuildFromPreset(EntityPresetDefinition preset)
        {
            RPGEntityBuilder builder = new();
            _entityModel = builder.FromPreset(preset).Build();

            Debug.Log($"[RPG Entity Framework] {gameObject.name} initialized as {_entityModel.EntityName}");
            OnEntityInitialized?.Invoke();
        }

        /// <summary>
        /// Allows you to inject an externally configured Builder.
        /// </summary>
        /// <param name="manualBuilder"> External builder for dynamic entity </param>
        public void BuildFromExternalBuilder(RPGEntityBuilder manualBuilder)
        {
            _entityModel = manualBuilder.Build();
            OnEntityInitialized?.Invoke();
        }

        /// <summary>
        /// Returns the Race of the entity
        /// </summary>
        public RaceDefinition GetEntityRace()
        {
            if (_entityModel == null) return null;
            return _entityModel.RaceDef;
        }

        /// <summary>
        /// Returns the Sub-Race of the entity.
        /// </summary>
        public SubRaceDefinition GetEntitySubRace()
        {
            if (_entityModel == null) return null;
            return _entityModel.SubRaceDef;
        }

        /// <summary>
        /// Returns the Class of the entity.
        /// </summary>
        public ClassDefinition GetEntityClass()
        {
            if (_entityModel == null) return null;
            return _entityModel.ClassDef;
        }

        /// <summary>
        /// Returns the Sub-Class of the entity.
        /// </summary>
        public SubClassDefinition GetEntitySubClass()
        {
            if (_entityModel == null) return null;
            return _entityModel.SubClassDef;
        }

        /// <summary>
        /// Returns the value of an attribute.
        /// </summary>
        /// <param name="attribute"> The attribute you want to get </param>
        public float GetAttributeValue(AttributeDefinition attribute)
        {
            if (_entityModel == null) return 0;
            return _entityModel.GetAttributeValue(attribute);
        }

        /// <summary>
        /// Determine whether the entity has the ability
        /// </summary>
        /// <param name="ability"> The ability you want to check </param>
        public bool HasAbility(AbilityDefinition ability)
        {
            if (_entityModel == null) return false;
            return _entityModel.Abilities.Contains(ability);
        }

        #endregion
    }
}