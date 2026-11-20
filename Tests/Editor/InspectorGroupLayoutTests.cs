using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Yvsim.InspectorGroups.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Yvsim.InspectorGroups.Tests
{
    /// <summary>在真实布局事件中读取标题与内容占位，不在生产绘制器中加入测试入口。</summary>
    public sealed class GroupLayoutProbeWindow : EditorWindow
    {
        public sealed class Geometry
        {
            public Rect container;
            public Rect title;
            public Rect field;
            public Rect help;
            public Rect button;
            public bool expanded;
        }

        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type GroupType = typeof(InspectorGroupsEditor).Assembly
            .GetType("Yvsim.InspectorGroups.Editor.InspectorGroupGUI", true);
        private static readonly MethodInfo BeginGroup = GroupType.GetMethod("Begin", InstanceFields);
        private static readonly FieldInfo CurrentLayout = typeof(GUILayoutUtility)
            .GetField("current", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        public readonly List<Geometry> geometry = new List<Geometry>();
        public string keyPrefix;
        public bool separateScopes;
        public bool[] expanded = { true, true, true };
        public Exception error;

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(keyPrefix)) return;
            IDisposable renderer = null;
            try
            {
                bool repaint = Event.current.type == EventType.Repaint;
                if (repaint) geometry.Clear();
                using (new EditorGUILayout.VerticalScope(EditorStyles.inspectorDefaultMargins))
                {
                    for (int index = 0; index < expanded.Length; index++)
                    {
                        if (renderer == null) renderer = (IDisposable)Activator.CreateInstance(GroupType, true);
                        var group = new FoldoutGroupAttribute("Framework " + index, expanded[index],
                            style: InspectorGroupStyle.Framework);
                        BeginGroup.Invoke(renderer, new object[] { group, keyPrefix + index, true });
                        var sample = new Geometry { expanded = expanded[index] };
                        if (repaint)
                        {
                            object cache = CurrentLayout.GetValue(null);
                            object layout = cache.GetType().GetField("topLevel", InstanceFields).GetValue(cache);
                            var entries = (IList)layout.GetType().GetField("entries", InstanceFields).GetValue(layout);
                            sample.container = EntryRect(layout);
                            sample.title = EntryRect(entries[0]);
                        }
                        if (expanded[index])
                        {
                            sample.field = EditorGUILayout.GetControlRect();
                            EditorGUI.IntField(sample.field, "Value", 13);
                            EditorGUILayout.HelpBox("A visible description for the framework group.", MessageType.Info);
                            sample.help = GUILayoutUtility.GetLastRect();
                            GUILayout.Button("Action");
                            sample.button = GUILayoutUtility.GetLastRect();
                        }
                        if (repaint) geometry.Add(sample);
                        if (separateScopes)
                        {
                            renderer.Dispose();
                            renderer = null;
                        }
                    }
                    renderer?.Dispose();
                    renderer = null;
                }
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { error = exception; }
            finally { renderer?.Dispose(); }
        }

        private static Rect EntryRect(object entry) =>
            (Rect)entry.GetType().GetField("rect", InstanceFields).GetValue(entry);
    }

    public sealed class InspectorGroupLayoutTests
    {
        [TestCase(340, false)]
        [TestCase(700, false)]
        [TestCase(340, true)]
        [TestCase(700, true)]
        public void FrameworkGroups_KeepHeadersContentAndCollapsedGroupsSeparated(int width, bool separateScopes)
        {
            var window = ScriptableObject.CreateInstance<GroupLayoutProbeWindow>();
            window.keyPrefix = "InspectorGroupLayoutTest:" + Guid.NewGuid() + ":";
            window.separateScopes = separateScopes;
            try
            {
                window.ShowUtility();
                window.position = new Rect(40, 40, width, 700);
                foreach (var states in new[]
                {
                    new[] { true, true, true },
                    new[] { false, true, false },
                    new[] { false, false, false }
                })
                {
                    window.expanded = states;
                    for (int index = 0; index < states.Length; index++)
                        SessionState.SetBool(window.keyPrefix + index, states[index]);
                    window.error = null;
                    window.SendEvent(new Event { type = EventType.Layout });
                    window.SendEvent(new Event { type = EventType.Repaint });
                    Assert.That(window.error, Is.Null, window.error?.ToString());
                    Assert.That(window.geometry.Count, Is.EqualTo(states.Length));
                    for (int index = 0; index < window.geometry.Count; index++)
                    {
                        var sample = window.geometry[index];
                        Assert.That(sample.title.yMin, Is.GreaterThanOrEqualTo(sample.container.yMin));
                        Assert.That(sample.title.yMax, Is.LessThanOrEqualTo(sample.container.yMax));
                        if (index > 0)
                            Assert.That(sample.container.yMin - window.geometry[index - 1].container.yMax,
                                Is.GreaterThanOrEqualTo(3f), "相邻分组的背景必须有可见间距");
                        if (sample.expanded)
                        {
                            Assert.That(sample.field.yMin - sample.title.yMax, Is.GreaterThanOrEqualTo(4f),
                                "标题不能侵入首行，展开内容必须留出清晰间距");
                            Assert.That(sample.help.yMin, Is.GreaterThanOrEqualTo(sample.field.yMax));
                            Assert.That(sample.button.yMin, Is.GreaterThanOrEqualTo(sample.help.yMax));
                            Assert.That(sample.button.yMax, Is.LessThanOrEqualTo(sample.container.yMax));
                            Assert.That(sample.help.xMin, Is.EqualTo(sample.field.xMin).Within(3f));
                            Assert.That(sample.button.xMin, Is.EqualTo(sample.field.xMin).Within(3f));
                            Assert.That(sample.button.xMax, Is.EqualTo(sample.field.xMax).Within(3f));
                        }
                        else Assert.That(sample.container.height, Is.InRange(22f, 40f),
                            "收起后只保留紧凑标题，不残留内容占位");
                    }
                    var collapsed = window.geometry.Where(sample => !sample.expanded).ToArray();
                    if (collapsed.Length > 1)
                        Assert.That(collapsed.Max(sample => sample.container.height) -
                            collapsed.Min(sample => sample.container.height), Is.LessThan(0.1f),
                            "相同样式的收起分组高度必须一致");
                }
            }
            finally
            {
                for (int index = 0; index < window.expanded.Length; index++)
                    SessionState.EraseBool(window.keyPrefix + index);
                window.Close();
            }
        }

        [TestCase("Yvsim.RaycastUtilities.BoxRaycaster", "halfExtents")]
        [TestCase("Yvsim.RaycastUtilities.StraightRaycaster", "directions")]
        public void InstalledRaycasterShapeFields_StartTheirOwnFrameworkGroup(string typeName, string fieldName)
        {
            Type type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>().FirstOrDefault(candidate => candidate.FullName == typeName);
            // Inspector Groups 独立安装时没有射线包；不因此引入依赖或跳过基础测试套件。
            if (type == null) return;
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic |
                BindingFlags.Public | BindingFlags.DeclaredOnly);
            Assert.That(field, Is.Not.Null);
            var group = field.GetCustomAttribute<FoldoutGroupAttribute>(false);
            Assert.That(group, Is.Not.Null, "派生组件参数必须有独立分组，不能留在已收起的基类组外");
            Assert.That(group.Style, Is.EqualTo(InspectorGroupStyle.Framework));
        }
    }
}
