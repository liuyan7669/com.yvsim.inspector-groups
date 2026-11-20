using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Yvsim.InspectorGroups.Editor;
using Object = UnityEngine.Object;

namespace Yvsim.InspectorGroups.Tests
{
    public class InspectorBindingProbe : MonoBehaviour
    {
        public int retainedValue = 37;
        public InspectorBindingData currentData = new InspectorBindingData();
    }

    [Serializable]
    public class InspectorBindingData { public int value; }

    public class InspectorBindingTestWindow : EditorWindow
    {
        public UnityEditor.Editor inspector;
        public Exception error;
        public int draws;
        private void OnGUI()
        {
            if (inspector == null) return;
            try { inspector.OnInspectorGUI(); draws++; }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { error = exception; }
        }
    }

    /// <summary>在真实 IMGUI 事件中重现编译后 Editor 与目标类型失配的过渡状态。</summary>
    public class InspectorBindingRegressionTests
    {
        public static IEnumerable ConsumerEditors()
        {
            var types = TypeCache.GetTypesDerivedFrom<InspectorGroupsEditor>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters &&
                    type.GetCustomAttributes(typeof(CustomEditor), false).Length > 0).ToArray();
            if (types.Length == 0)
                yield return new TestCaseData(null).Ignore("未安装专用 Inspector 消费包。");
            foreach (var type in types) yield return new TestCaseData(type);
        }

        [TestCaseSource(nameof(ConsumerEditors))]
        public void StaleEditor_RendersActualFieldsWithoutOldTypeAccess(Type editorType)
        {
            var host = new GameObject("Stale inspector binding");
            var probe = host.AddComponent<InspectorBindingProbe>();
            UnityEditor.Editor editor = null;
            try
            {
                editor = UnityEditor.Editor.CreateEditor(probe, editorType);
                AssertGuiDraws(editor);
                Assert.That(editor.serializedObject.FindProperty("retainedValue"), Is.Not.Null);
                Assert.That(probe.retainedValue, Is.EqualTo(37));
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(host);
            }
        }

        [TestCaseSource(nameof(ConsumerEditors))]
        public void ValidEditor_KeepsBusinessInspectorDrawable(Type editorType)
        {
            Type contract = editorType;
            while (contract != null && (!contract.IsGenericType ||
                contract.GetGenericTypeDefinition() != typeof(InspectorGroupsEditor<>))) contract = contract.BaseType;
            Assert.That(contract, Is.Not.Null, "专用 Inspector 必须声明类型约定");
            Type targetType = contract.GetGenericArguments()[0];
            if (targetType.IsAbstract)
                targetType = TypeCache.GetTypesDerivedFrom(targetType)
                    .First(type => !type.IsAbstract && !type.ContainsGenericParameters);
            var host = new GameObject("Valid inspector binding");
            UnityEditor.Editor editor = null;
            try
            {
                var component = host.AddComponent(targetType);
                editor = UnityEditor.Editor.CreateEditor(component, editorType);
                AssertGuiDraws(editor);
            }
            finally
            {
                if (editor != null) Object.DestroyImmediate(editor);
                Object.DestroyImmediate(host);
            }
        }

        [TestCaseSource(nameof(ConsumerEditors))]
        public void DestroyedTarget_DoesNotAccessSerializedObject(Type editorType)
        {
            var host = new GameObject("Destroyed inspector target");
            var editor = UnityEditor.Editor.CreateEditor(host.AddComponent<InspectorBindingProbe>(), editorType);
            Object.DestroyImmediate(host);
            try { Assert.DoesNotThrow(() => editor.OnInspectorGUI()); }
            finally { Object.DestroyImmediate(editor); }
        }

        public static IEnumerable ConsumerDrawers()
        {
            var types = TypeCache.GetTypesDerivedFrom<InspectorGroupsPropertyDrawer>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters).ToArray();
            if (types.Length == 0) yield return new TestCaseData(null).Ignore("未安装语义 PropertyDrawer 消费包。");
            foreach (var type in types) yield return new TestCaseData(type);
        }

        [TestCaseSource(nameof(ConsumerDrawers))]
        public void StaleSemanticDrawer_UsesCurrentLabelWhenOldFieldsDisappear(Type drawerType)
        {
            var host = new GameObject("Changed serialized shape");
            var probe = host.AddComponent<InspectorBindingProbe>();
            try
            {
                using (var serialized = new SerializedObject(probe))
                {
                    var drawer = Activator.CreateInstance(drawerType);
                    var method = drawerType.GetMethod("GetLabel", System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    var label = new GUIContent("Current data");
                    var result = (GUIContent)method.Invoke(drawer,
                        new object[] { serialized.FindProperty("currentData"), label });
                    Assert.That(result.text, Is.EqualTo(label.text));
                }
            }
            finally { Object.DestroyImmediate(host); }
        }

        private static void AssertGuiDraws(UnityEditor.Editor editor)
        {
            var window = ScriptableObject.CreateInstance<InspectorBindingTestWindow>();
            try
            {
                window.inspector = editor;
                window.ShowUtility();
                window.position = new Rect(50, 50, 700, 700);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(window.error, Is.Null, window.error?.ToString());
                Assert.That(window.draws, Is.GreaterThan(0), "未实际执行 Inspector GUI");
            }
            finally { window.Close(); }
        }
    }
}
