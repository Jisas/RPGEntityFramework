using UnityEngine;

namespace RPGEntityFramework.Data
{
    [CreateAssetMenu(fileName = "AttributeDefinition", menuName = "RPG Framework/Attribute Definition")]
    public class AttributeDefinition : RPGDefinition
    {
        public string attributeName;
        [TextArea] public string description;
        public Sprite icon;
    }
}