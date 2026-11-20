using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Yvsim.InspectorGroups.Editor;
using Object = UnityEngine.Object;

namespace Yvsim.InspectorGroups.Tests
{
    public class ScriptRowProbe : MonoBehaviour
    {
        public static int statusReads;
        [Header("Unity attributes"), Tooltip("Native range"), UnityEngine.Range(0, 10)] public int range = 4;
        [SerializeField] private float privateValue = 0.5f;
        [FoldoutGroup("Fields", true), ScriptRowMarker] public int custom = 3;
        [ShowAsString] public string Status { get { statusReads++; return "Ready"; } }
    }
    public class ScriptRowVisibleProbe : ScriptRowProbe { }
    [HideScript] public class ScriptRowHiddenProbe : ScriptRowProbe { }
    public class ScriptRowInheritedProbe : ScriptRowHiddenProbe { }
    [HideScript] public class ScriptRowHiddenAsset : ScriptableObject { public int value; }
    public sealed class ScriptRowMarkerAttribute : UnityEngine.PropertyAttribute { }
    [CustomPropertyDrawer(typeof(ScriptRowMarkerAttribute))]
    public class ScriptRowMarkerDrawer : PropertyDrawer
    {
        public static int draws;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            draws++;
            property.intValue = EditorGUI.IntField(position, label, property.intValue);
        }
    }
    public class ScriptRowTestWindow : EditorWindow
    {
        public UnityEditor.Editor inspector;
        public float contentHeight;
        public Exception error;
        public void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(false));
            try { inspector.OnInspectorGUI(); }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { error = exception; }
            EditorGUILayout.EndVertical();
            if (Event.current.type == EventType.Repaint) contentHeight = GUILayoutUtility.GetLastRect().height;
        }
    }
    public class HideScriptAttributeTests
    {
        private GameObject host;
        [SetUp] public void SetUp() { host = new GameObject("HideScript validation"); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(host); }

        [Test] public void UnmarkedClass_KeepsScriptVisible()
        { Assert.That(Hidden(host.AddComponent<ScriptRowVisibleProbe>()), Is.False); }
        [Test] public void MarkedClass_HidesOnlyPresentation()
        {
            var target = host.AddComponent<ScriptRowHiddenProbe>();
            Assert.That(Hidden(target), Is.True);
            using (var serialized = new SerializedObject(target))
                foreach (string name in new[] { "m_Script", "range", "privateValue", "custom" })
                    Assert.That(serialized.FindProperty(name), Is.Not.Null, name);
        }
        [Test] public void DerivedClass_InheritsHideScript()
        { Assert.That(Hidden(host.AddComponent<ScriptRowInheritedProbe>()), Is.True); }
        [Test] public void ScriptableObject_UsesTheSameClassPolicy()
        {
            var target = ScriptableObject.CreateInstance<ScriptRowHiddenAsset>();
            try { Assert.That(Hidden(target), Is.True); }
            finally { Object.DestroyImmediate(target); }
        }
        [Test] public void MixedSelection_DoesNotHideUnmarkedTargets()
        { Assert.That(InspectorScriptVisibility.ShouldHideScript(new Object[] { host.AddComponent<ScriptRowHiddenProbe>(), host.AddComponent<ScriptRowVisibleProbe>() }), Is.False); }
        [Test] public void MarkedMultiSelection_HidesScript()
        { Assert.That(InspectorScriptVisibility.ShouldHideScript(new Object[] { host.AddComponent<ScriptRowHiddenProbe>(), host.AddComponent<ScriptRowInheritedProbe>() }), Is.True); }
        [Test] public void MissingTargets_DoNotChangeVisibility()
        {
            Assert.That(InspectorScriptVisibility.ShouldHideScript(null), Is.False);
            Assert.That(InspectorScriptVisibility.ShouldHideScript(new Object[0]), Is.False);
            Assert.That(InspectorScriptVisibility.ShouldHideScript(new Object[] { null }), Is.False);
        }
        [Test] public void HideScript_RemovesOneRowAndKeepsNativeAndCustomDrawing()
        {
            var visible = host.AddComponent<ScriptRowVisibleProbe>();
            var hidden = host.AddComponent<ScriptRowHiddenProbe>();
            float visibleHeight = Draw(visible);
            float hiddenHeight = Draw(hidden);
            Assert.That(visibleHeight - hiddenHeight,
                Is.EqualTo(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing).Within(0.5f));
            Assert.That(hidden.range, Is.EqualTo(4));
            Assert.That(hidden.custom, Is.EqualTo(3));
        }
        private static bool Hidden(Object value) => InspectorScriptVisibility.ShouldHideScript(new[] { value });
        private static float Draw(Object target)
        {
            var editor = UnityEditor.Editor.CreateEditor(target);
            var window = ScriptableObject.CreateInstance<ScriptRowTestWindow>();
            try
            {
                window.inspector = editor;
                window.ShowUtility(); window.position = new Rect(40, 40, 700, 700);
                ScriptRowMarkerDrawer.draws = 0; ScriptRowProbe.statusReads = 0;
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(window.error, Is.Null, window.error?.ToString());
                Assert.That(ScriptRowMarkerDrawer.draws, Is.GreaterThan(0), "Custom PropertyDrawer was skipped");
                Assert.That(ScriptRowProbe.statusReads, Is.GreaterThan(0), "ShowAsString was skipped");
                Assert.That(window.contentHeight, Is.GreaterThan(0));
                return window.contentHeight;
            }
            finally { window.Close(); Object.DestroyImmediate(editor); }
        }
    }
}
