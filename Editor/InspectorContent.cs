using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>业务包声明 Inspector 内容和操作；布局、样式及控件绘制完全由 Inspector Groups 管理。</summary>
    public sealed class InspectorContent
    {
        private readonly List<Action<UnityEditor.Editor>> items = new List<Action<UnityEditor.Editor>>();
        internal readonly List<string> ExcludedProperties = new List<string>();

        /// <summary>由补充内容提供展示的字段不再参与默认字段绘制。</summary>
        public void Exclude(params string[] paths) => ExcludedProperties.AddRange(paths);

        /// <summary>声明一个使用统一分组效果的内容区域。</summary>
        public void Group(string key, string title, Action<InspectorContent> describe, InspectorGroupStyle style = InspectorGroupStyle.Default)
        {
            var content = new InspectorContent();
            describe(content);
            items.Add(inspector =>
            {
                using (var group = new InspectorGroupScope(inspector, key, title, style: style))
                    if (group.Expanded) content.Render(inspector);
            });
        }

        /// <summary>声明只读状态值。</summary>
        public void Value(string label, string value) => Value(label, value, null);

        /// <summary>声明带悬停说明的只读状态值；标签和数值均显示同一提示。</summary>
        public void Value(string label, string value, string tooltip) => items.Add(inspector =>
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), new GUIContent(value ?? "-", tooltip)));

        /// <summary>声明统一提示信息。</summary>
        public void Message(string message, MessageType type = MessageType.Info) =>
            items.Add(inspector => EditorGUILayout.HelpBox(message, type));

        /// <summary>声明操作回调。禁用时保留按钮，点击前提交字段修改。</summary>
        public void Action(string label, System.Action execute, bool enabled = true, string tooltip = null)
        {
            items.Add(inspector =>
            {
                using (new EditorGUI.DisabledScope(!enabled))
                    if (GUILayout.Button(new GUIContent(label, tooltip)))
                    {
                        inspector.serializedObject.ApplyModifiedProperties();
                        execute();
                        inspector.Repaint();
                        GUIUtility.ExitGUI();
                    }
            });
        }

        /// <summary>以统一原生字段效果展示指定序列化字段。</summary>
        public void Property(string path, bool readOnly = false)
        {
            items.Add(inspector =>
            {
                var property = inspector.serializedObject.FindProperty(path);
                if (property == null) return;
                using (new EditorGUI.DisabledScope(readOnly)) EditorGUILayout.PropertyField(property, true);
            });
        }

        internal void Render(UnityEditor.Editor inspector)
        {
            foreach (var item in items) item(inspector);
        }
    }
}
