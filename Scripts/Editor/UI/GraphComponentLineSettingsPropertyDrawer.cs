using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    [CustomPropertyDrawer(typeof(GraphComponent.LineSettings))]
    public class GraphComponentLineSettingsPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetPropertyHeightOfChildren();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.PropertyFieldForChildren(position);
        }
    }
}
