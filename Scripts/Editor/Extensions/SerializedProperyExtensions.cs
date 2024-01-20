using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    public static class SerializedPropertyExtensions
    {
        public static float GetPropertyHeightOfChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty iterator = serializedProperty.Copy();
            if (!iterator.hasVisibleChildren)
                return 0.0f;
            
            iterator.NextVisible(true);

            SerializedProperty nextSibling = serializedProperty.Copy();
            bool hasSibling = nextSibling.NextVisible(false);
            float totalHeight = 0.0f;
            do
            {
                totalHeight += EditorGUI.GetPropertyHeight(iterator, true);
            }
            while (iterator.NextVisible(false) &&
                   (!hasSibling || !SerializedProperty.EqualContents(iterator, nextSibling)));

            return totalHeight;
        }
        
        public static void PropertyFieldForChildren(
            this SerializedProperty serializedProperty, Rect position, bool indent = true)
        {
            SerializedProperty iterator = serializedProperty.Copy();
            if (!iterator.hasVisibleChildren)
                return;
            
            iterator.NextVisible(true);
            
            // Make sure the children are indented.
            if (indent)
                EditorGUI.indentLevel++;

            SerializedProperty nextSibling = serializedProperty.Copy();
            bool hasSibling = nextSibling.NextVisible(false);
            float y = position.yMin;
            do
            {
                float height = EditorGUI.GetPropertyHeight(iterator);
                Rect controlRect = new Rect(position.xMin, y, position.width, height);
                EditorGUI.PropertyField(controlRect, iterator, true);
                y += height;
                
                // Spacing, too!
                y += EditorGUIUtility.standardVerticalSpacing;
            }
            while (iterator.NextVisible(false) &&
                   (!hasSibling || !SerializedProperty.EqualContents(iterator, nextSibling)));
            
            // Restore indentation.
            if (indent)
                EditorGUI.indentLevel--;
        }
    }
}
