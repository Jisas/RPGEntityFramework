using UnityEngine;
using System;

namespace RPGEntityFramework.Data
{
    public class RPGDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField] private Sprite icon;

        public string Id => id;
        public Sprite Icon => icon;

        // Se ejecuta cuando el script se carga o se cambia un valor en el inspector
        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }
}