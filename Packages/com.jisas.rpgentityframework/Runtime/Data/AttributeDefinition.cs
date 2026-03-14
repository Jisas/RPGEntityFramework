using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "AttributeDefinition", menuName = "RPG Entity Framework/Attribute Definition")]
    public class AttributeDefinition : RPGDefinition
    {
        public string attributeName;
        [TextArea] public string description;
    }

    [System.Serializable]
    public class AttributeValue
    {
        public AttributeDefinition attribute;
        public float value;
    }
}