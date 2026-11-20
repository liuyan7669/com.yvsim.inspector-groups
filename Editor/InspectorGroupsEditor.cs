using System;
using UnityEditor;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>供带业务操作的 Inspector 复用统一字段、条件显示和分组绘制。</summary>
    public abstract class InspectorGroupsEditor : UnityEditor.Editor
    {
        private readonly InspectorGroupRenderer renderer = new InspectorGroupRenderer();
        private bool drawingInspectorBody;

        /// <summary>统一绘制字段；补充内容中的字段通过声明排除，Script 行由类特性决定。</summary>
        private void DrawGroupedProperties(params string[] excludedProperties)
        {
            serializedObject.Update();
            if (drawingInspectorBody && Array.IndexOf(excludedProperties, "m_Script") < 0)
            {
                var bodyExclusions = new string[excludedProperties.Length + 1];
                Array.Copy(excludedProperties, bodyExclusions, excludedProperties.Length);
                bodyExclusions[excludedProperties.Length] = "m_Script";
                excludedProperties = bodyExclusions;
            }
            renderer.DrawProperties(this, excludedProperties);
            serializedObject.ApplyModifiedProperties();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall -= RefreshAfterScriptReload;
            EditorApplication.delayCall += RefreshAfterScriptReload;
        }

        private static void RefreshAfterScriptReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RefreshAfterScriptReload;
                return;
            }
            // Unity 可能沿用改继承关系前的 Editor，包括通用面板到专用面板的反向切换。
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private bool rebuildQueued;

        /// <summary>所有目标都必须仍符合专用 Inspector 的类型约定，包括多选和编译后的过渡帧。</summary>
        protected bool HasCompatibleTargets
        {
            get
            {
                if (targets == null || targets.Length == 0) return false;
                foreach (var candidate in targets)
                    if (candidate == null || !IsCompatibleTarget(candidate)) return false;
                return true;
            }
        }

        protected abstract bool IsCompatibleTarget(UnityEngine.Object candidate);

        /// <summary>先处理失效的 Editor 绑定，派生类不能绕过此入口直接使用旧字段或强制转换。</summary>
        public sealed override void OnInspectorGUI()
        {
            if (target == null) return;
            if (!HasCompatibleTargets)
            {
                // 继承关系可能已改变，显示真实字段，不再绘制旧类型的业务面板。
                foreach (var candidate in targets) if (candidate == null) return;
                DrawGroupedProperties();
                if (!rebuildQueued)
                {
                    rebuildQueued = true;
                    EditorApplication.delayCall += RebuildActiveInspector;
                }
                return;
            }
            serializedObject.UpdateIfRequiredOrScript();
            RefreshSerializedProperties();
            // Script 必须位于业务分组之前，不能留到派生 Inspector 的剩余字段中绘制。
            var script = serializedObject.FindProperty("m_Script");
            if (script != null && !InspectorScriptVisibility.ShouldHideScript(targets))
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.PropertyField(script);
            drawingInspectorBody = true;
            try { DrawInspector(); }
            finally { drawingInspectorBody = false; }
        }

        /// <summary>在有效绑定上重新查询业务字段，避免保留编译前的 SerializedProperty。</summary>
        protected virtual void RefreshSerializedProperties() { }

        private void DrawInspector()
        {
            var content = new InspectorContent();
            BuildInspector(content);
            DrawGroupedProperties(content.ExcludedProperties.ToArray());
            content.Render(this);
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>仅声明状态数据和业务操作，所有控件由 Inspector Groups 绘制。</summary>
        protected virtual void BuildInspector(InspectorContent content) { }

        private void RebuildActiveInspector()
        {
            if (this == null) return;
            var tracker = ActiveEditorTracker.sharedTracker;
            // 独立创建的 Editor 不应重建用户当前无关的 Inspector。
            if (Array.IndexOf(tracker.activeEditors, this) >= 0) tracker.ForceRebuild();
        }

        protected virtual void OnDisable()
        {
            EditorApplication.delayCall -= RebuildActiveInspector;
            rebuildQueued = false;
            renderer.Dispose();
        }
    }

    /// <summary>声明专用 Inspector 的目标类型，基类统一检查编译后是否仍匹配。</summary>
    public class InspectorGroupsEditor<TTarget> : InspectorGroupsEditor where TTarget : UnityEngine.Object
    {
        protected sealed override bool IsCompatibleTarget(UnityEngine.Object candidate) => candidate is TTarget;
    }

    /// <summary>为业务按钮、动态状态与预览复用 FoldoutGroup 的相同绘制和状态管理。</summary>
    public sealed class InspectorGroupScope : IDisposable
    {
        private readonly InspectorGroupGUI group = new InspectorGroupGUI();
        private readonly int previousIndent;
        private readonly bool frameworkStyle;
        private bool disposed;
        public bool Expanded { get; }

        /// <param name="inspector">拥有该分组的 Inspector。</param>
        /// <param name="key">同一 Inspector 内稳定且唯一的分组标识。</param>
        /// <param name="title">分组标题。</param>
        /// <param name="expanded">首次是否展开。</param>
        /// <param name="style">统一分组样式，默认普通分组。</param>
        public InspectorGroupScope(UnityEditor.Editor inspector, string key, string title, bool expanded = true, InspectorGroupStyle style = InspectorGroupStyle.Default)
        {
            frameworkStyle = style == InspectorGroupStyle.Framework;
            group.Begin(new FoldoutGroupAttribute(title, expanded, style: style),
                $"FoldoutGroup:{inspector.target.GetInstanceID()}:{key}", true);
            Expanded = group.Expanded;
            previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel += group.ContentIndent;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            EditorGUI.indentLevel = previousIndent;
            group.Dispose();
            if (!frameworkStyle) EditorGUILayout.Space(2f);
        }
    }
}
