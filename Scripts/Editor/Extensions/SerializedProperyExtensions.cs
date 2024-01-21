using System;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.Graphing
{
    public static class SerializedPropertyExtensions
    {
        public static float GetPropertyHeightOfChildren(
            this SerializedProperty serializedProperty, params string[] propertiesToExclude)
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
                // Check if it's listed as a property we should skip.
                bool shouldSkip = false;
                for (int i = 0; i < propertiesToExclude.Length; i++)
                {
                    if (string.Equals(iterator.name, propertiesToExclude[i], StringComparison.Ordinal))
                    {
                        shouldSkip = true;
                        break;
                    }
                }
                if (shouldSkip)
                    continue;
                
                totalHeight += EditorGUI.GetPropertyHeight(iterator, true);
                totalHeight += EditorGUIUtility.standardVerticalSpacing;
            }
            while (iterator.NextVisible(false) &&
                   (!hasSibling || !SerializedProperty.EqualContents(iterator, nextSibling)));

            return totalHeight;
        }
        
        public static void PropertyFieldForChildren(
            this SerializedProperty serializedProperty, Rect position, bool indent = true,
            params string[] propertiesToExclude)
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
                // Check if it's listed as a property we should skip.
                bool shouldSkip = false;
                for (int i = 0; i < propertiesToExclude.Length; i++)
                {
                    if (string.Equals(iterator.name, propertiesToExclude[i], StringComparison.Ordinal))
                    {
                        shouldSkip = true;
                        break;
                    }
                }
                if (shouldSkip)
                    continue;
                
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
