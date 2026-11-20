using System;
using UnityEditor;
using UnityEngine;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>在任意对象层级绘制分组，统一管理背景、折叠和组间距。</summary>
    internal sealed class InspectorGroupGUI : IDisposable
    {
        private const float EdgeMargin = 6f;
        private const float CornerRadius = 3f;
        private const float FrameworkGap = 3f;
        private const float FrameworkHeaderHeight = 22f;
        private static GUIStyle layoutStyle;
        private static GUIStyle frameworkLayoutStyle;
        private static GUIStyle frameworkFoldoutStyle;
        private static GUIStyle frameworkBadgeStyle;
        private static bool frameworkProSkin;
        private bool inGroup;
        private bool framework;
        private float spacing;
        internal bool Expanded = true;

        internal void Begin(FoldoutGroupAttribute group, string key, bool fullWidth)
        {
            if (inGroup)
            {
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(framework ? Mathf.Max(FrameworkGap, spacing) : spacing);
            }
            framework = group.Style == InspectorGroupStyle.Framework;
            spacing = group.Spacing;
            if (framework) EditorGUILayout.Space(FrameworkGap);
            if (layoutStyle == null)
            {
                layoutStyle = new GUIStyle
                {
                    padding = new RectOffset(GUI.skin.box.padding.left, GUI.skin.box.padding.right, 6, 6),
                    margin = new RectOffset(GUI.skin.box.margin.left, GUI.skin.box.margin.right, 0, 0)
                };
                frameworkLayoutStyle = new GUIStyle(layoutStyle)
                {
                    padding = new RectOffset(6, 6, 3, 6)
                };
            }
            GUIStyle containerStyle = framework ? frameworkLayoutStyle : layoutStyle;
            Rect rect = EditorGUILayout.BeginVertical(containerStyle);
            inGroup = true;
            if (fullWidth)
            {
                rect.xMin = EdgeMargin;
                rect.xMax += containerStyle.margin.right + EditorStyles.inspectorDefaultMargins.padding.right - EdgeMargin;
            }
            else rect.xMin += Mathf.Max(0, EditorGUI.indentLevel - 1) * 15f;
            if (Event.current.type == EventType.Repaint)
            {
                if (group.Border) GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
                if (framework)
                {
                    Color fill = EditorGUIUtility.isProSkin
                        ? new Color32(0, 0, 0, 22) : new Color32(255, 255, 255, 45);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, 0f, CornerRadius);
                }
                else if (!group.Border)
                {
                    Color fill = EditorGUIUtility.isProSkin
                        ? new Color32(96, 96, 96, 52) : new Color32(235, 235, 235, 52);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, fill, 0f, CornerRadius);
                }
            }
            bool previous = SessionState.GetBool(key, group.Expanded);
            // 标题占位与绘制使用同一个矩形，背景和标记不向内容区域扩张。
            Rect foldout = EditorGUILayout.GetControlRect(false,
                framework ? FrameworkHeaderHeight : EditorGUIUtility.singleLineHeight);
            GUIStyle style = EditorStyles.foldout;
            if (framework)
            {
                EnsureFrameworkStyles();
                style = frameworkFoldoutStyle;
                if (Event.current.type == EventType.Repaint)
                {
                    Color accent = EditorGUIUtility.isProSkin
                        ? new Color32(109, 143, 158, 180) : new Color32(70, 110, 129, 180);
                    GUI.DrawTexture(new Rect(rect.xMin + 1f, foldout.center.y - 5f, 2f, 10f),
                        Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, accent, 0f, 1f);
                }
            }
            if (group.Color is Color color)
            {
                style = new GUIStyle(style);
                style.normal.textColor = style.onNormal.textColor = color;
                style.focused.textColor = style.onFocused.textColor = color;
                style.hover.textColor = style.onHover.textColor = color;
                style.active.textColor = style.onActive.textColor = color;
            }
            Expanded = EditorGUI.Foldout(foldout, previous, group.Title, true, style);
            if (framework)
            {
                GUI.Label(new Rect(rect.xMax - 42f, foldout.y, 30f, foldout.height), "框架", frameworkBadgeStyle);
                if (Expanded) EditorGUILayout.Space(4f);
            }
            if (Expanded != previous) SessionState.SetBool(key, Expanded);
        }

        private static void EnsureFrameworkStyles()
        {
            bool proSkin = EditorGUIUtility.isProSkin;
            if (frameworkFoldoutStyle != null && frameworkProSkin == proSkin) return;
            frameworkProSkin = proSkin;
            frameworkFoldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            frameworkFoldoutStyle.padding.right += 42;
            frameworkBadgeStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
            frameworkBadgeStyle.normal.textColor = proSkin
                ? new Color32(140, 148, 152, 255) : new Color32(94, 104, 110, 255);
        }

        // 框架组用容器内边距对齐字段、提示和按钮，避免只缩进字段导致错位。
        internal int ContentIndent => inGroup && !framework ? 1 : 0;

        public void Dispose()
        {
            if (inGroup)
            {
                EditorGUILayout.EndVertical();
                if (framework) EditorGUILayout.Space(Mathf.Max(FrameworkGap, spacing));
            }
            inGroup = false;
            Expanded = true;
        }
    }
}
