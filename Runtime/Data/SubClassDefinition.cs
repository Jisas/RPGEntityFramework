using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "SubClassDefinition", menuName = "RPG Framework/SubClass Definition")]
    public class SubClassDefinition : ScriptableObject
    {
        [Header("Datos Básicos")]
        public string subClassName;
        [TextArea] public string description;
        public Sprite icon;
    }
}