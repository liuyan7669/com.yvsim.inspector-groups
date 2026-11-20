using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yvsim.InspectorGroups.Tests
{
    /// <summary>标记实际走过自定义字段绘制的属性路径。</summary>
    public sealed class GroupStyleMarkerAttribute : UnityEngine.PropertyAttribute { }

    [CustomPropertyDrawer(typeof(GroupStyleMarkerAttribute))]
    public sealed class GroupStyleMarkerDrawer : PropertyDrawer
    {
        public static readonly HashSet<string> DrawnPaths = new HashSet<string>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DrawnPaths.Add(property.propertyPath);
            property.intValue = EditorGUI.IntField(position, label, property.intValue);
        }
    }

    public class FrameworkStyleBaseProbe : MonoBehaviour
    {
        [FoldoutGroup("Settings", true, style: InspectorGroupStyle.Framework), GroupStyleMarker]
        public int frameworkValue = 13;
    }

    public class UserStyleDerivedProbe : FrameworkStyleBaseProbe
    {
        // 相同标题应仍按字段路径分别保存折叠状态。
        [FoldoutGroup("Settings", true), GroupStyleMarker]
        public int userValue = 29;
    }

    public sealed class InspectorGroupStyleTests
    {
        [Test]
        public void ExistingAttributeArguments_KeepDefaultStyleAndValues()
        {
            var group = new FoldoutGroupAttribute("Legacy", true, 4f, "#4FC3F7", true);
            Assert.That(group.Style, Is.EqualTo(InspectorGroupStyle.Default));
            Assert.That(group.Title, Is.EqualTo("Legacy"));
            Assert.That(group.Expanded, Is.True);
            Assert.That(group.Spacing, Is.EqualTo(4f));
            Assert.That(group.Border, Is.True);
            Assert.That(group.Color.HasValue, Is.True);
            Assert.That(new FoldoutGroupAttribute("New user group").Style,
                Is.EqualTo(InspectorGroupStyle.Default));
        }

        [Test]
        public void FrameworkStyle_IsExplicitAndDoesNotChangeFoldoutDefaults()
        {
            var group = new FoldoutGroupAttribute("Package", style: InspectorGroupStyle.Framework);
            Assert.That(group.Style, Is.EqualTo(InspectorGroupStyle.Framework));
            Assert.That(group.Expanded, Is.False);
            Assert.That(group.Spacing, Is.EqualTo(2f));
        }

        [Test]
        public void FrameworkAndUserGroups_DrawTogetherAndFoldIndependently()
        {
            var host = new GameObject("Framework and user group regression");
            var component = host.AddComponent<UserStyleDerivedProbe>();
            var editor = UnityEditor.Editor.CreateEditor(component);
            var window = ScriptableObject.CreateInstance<InspectorBindingTestWindow>();
            string frameworkKey = $"FoldoutGroup:{component.GetInstanceID()}:frameworkValue";
            string userKey = $"FoldoutGroup:{component.GetInstanceID()}:userValue";
            try
            {
                window.inspector = editor;
                window.ShowUtility();
                window.position = new Rect(40, 40, 700, 700);
                SessionState.SetBool(frameworkKey, true);
                SessionState.SetBool(userKey, true);
                Draw(window);
                Assert.That(GroupStyleMarkerDrawer.DrawnPaths,
                    Is.EquivalentTo(new[] { "frameworkValue", "userValue" }));

                SessionState.SetBool(frameworkKey, false);
                Draw(window);
                Assert.That(GroupStyleMarkerDrawer.DrawnPaths,
                    Is.EquivalentTo(new[] { "userValue" }), "框架分组不能隐藏用户字段");
                Assert.That(SessionState.GetBool(userKey, false), Is.True);

                SessionState.SetBool(frameworkKey, true);
                SessionState.SetBool(userKey, false);
                Draw(window);
                Assert.That(GroupStyleMarkerDrawer.DrawnPaths,
                    Is.EquivalentTo(new[] { "frameworkValue" }), "用户分组不能隐藏框架字段");
                Assert.That(SessionState.GetBool(frameworkKey, false), Is.True);
                Assert.That(component.frameworkValue, Is.EqualTo(13));
                Assert.That(component.userValue, Is.EqualTo(29));
            }
            finally
            {
                SessionState.EraseBool(frameworkKey);
                SessionState.EraseBool(userKey);
                window.Close();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void InstalledYvsimRuntimeGroups_ExplicitlyUseFrameworkStyle()
        {
            // 仅定位真实包 Runtime 源码所属程序集；不引用任何可选的消费包。
            var runtimeNames = new HashSet<string>(CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Where(assembly => assembly.name != "Yvsim.InspectorGroups" && assembly.sourceFiles.Any(IsYvsimRuntimeSource))
                .Select(assembly => assembly.name));
            var violations = new List<string>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!runtimeNames.Contains(assembly.GetName().Name)) continue;
                foreach (Type type in assembly.GetTypes())
                {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                    foreach (MemberInfo member in type.GetMembers(flags))
                        foreach (var group in member.GetCustomAttributes<FoldoutGroupAttribute>(false))
                            if (group.Style != InspectorGroupStyle.Framework)
                                violations.Add(type.FullName + "." + member.Name + ": " + group.Title);
                }
            }
            Assert.That(violations, Is.Empty, "包自带分组遗漏框架样式：" + string.Join(", ", violations));
        }

        private static bool IsYvsimRuntimeSource(string path)
        {
            string normalized = path.Replace('\\', '/');
            return (normalized.StartsWith("Packages/com.yvsim.", StringComparison.Ordinal) ||
                normalized.Contains("/Packages/com.yvsim.")) && normalized.Contains("/Runtime/") &&
                !normalized.Contains("/Tests/") && !normalized.Contains("/Samples~/") && !normalized.Contains("/Samples/");
        }

        private static void Draw(InspectorBindingTestWindow window)
        {
            GroupStyleMarkerDrawer.DrawnPaths.Clear();
            window.error = null;
            window.SendEvent(new Event { type = EventType.Layout });
            window.SendEvent(new Event { type = EventType.Repaint });
            Assert.That(window.error, Is.Null, window.error?.ToString());
        }
    }
}
