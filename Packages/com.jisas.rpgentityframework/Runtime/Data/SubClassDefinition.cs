using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "SubClassDefinition", menuName = "RPG Entity Framework/SubClass Definition")]
    public class SubClassDefinition : RPGDefinition
    {
        [Header("Datos Básicos")]
        public string subClassName;
        [TextArea] public string description;
    }
}