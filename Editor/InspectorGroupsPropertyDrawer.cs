using UnityEditor;
using UnityEngine;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>以统一原生字段样式绘制具有自定义标题的序列化对象。</summary>
    public abstract class InspectorGroupsPropertyDrawer : PropertyDrawer
    {
        protected virtual GUIContent GetLabel(SerializedProperty property, GUIContent label) => label;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetLabel(property, label), true);
            if (property.isExpanded)
            using (new EditorGUI.IndentLevelScope())
            {
                var child = property.Copy();
                var end = property.GetEndProperty();
                bool next = child.NextVisible(true);
                while (next && !SerializedProperty.EqualContents(child, end))
                {
                    line.y += line.height + EditorGUIUtility.standardVerticalSpacing;
                    line.height = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(line, child, true);
                    next = child.NextVisible(false);
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;
            var child = property.Copy();
            var end = property.GetEndProperty();
            bool next = child.NextVisible(true);
            while (next && !SerializedProperty.EqualContents(child, end))
            {
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
                next = child.NextVisible(false);
            }
            return height;
        }
    }
}
