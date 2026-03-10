using UnityEngine;

namespace RPGFramework.Data
{
    [CreateAssetMenu(fileName = "AttributeDefinition", menuName = "RPG Framework/Attribute Definition")]
    public class AttributeDefinition : ScriptableObject
    {
        public string attributeName;
        [TextArea] public string description;
        public Sprite icon;
    }
}