using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    [CustomPropertyDrawer(typeof(GraphComponent.LineSettings))]
    public class GraphComponentLineSettingsPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GraphLine.Modes mode = (GraphLine.Modes)property.FindPropertyRelative("mode").intValue;
            if (mode == GraphLine.Modes.Threshold)
                return property.GetPropertyHeightOfChildren();
            
            return property.GetPropertyHeightOfChildren("threshold");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GraphLine.Modes mode = (GraphLine.Modes)property.FindPropertyRelative("mode").intValue;
            if (mode == GraphLine.Modes.Threshold)
                property.PropertyFieldForChildren(position);
            else
                property.PropertyFieldForChildren(position, true, "threshold");
        }
    }
}
