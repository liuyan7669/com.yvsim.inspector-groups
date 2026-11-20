using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Yvsim.InspectorGroups.Editor;
using Object = UnityEngine.Object;

namespace Yvsim.InspectorGroups.Tests
{
    public class InheritedMemberProbe : InheritedGroupBaseProbe
    {
        public static int reads;
        [ShowAsString] public string Status { get { reads++; return "Visible"; } }
        [Button] private void Act() { }
    }
    public class MemberGroupBaseProbe : MonoBehaviour
    {
        [FoldoutGroup("Base members", false), ShowAsString] public string BaseStatus => "Base";
    }
    public class MemberGroupDerivedProbe : MemberGroupBaseProbe
    {
        public static int reads;
        [ShowAsString] public string Status { get { reads++; return "Visible"; } }
    }
    public class InlineCollectionAssetProbe : ScriptableObject
    {
        public static int reads;
        [ShowAsString] public string Status { get { reads++; return "Inline visible"; } }
    }
    public class InlineCollectionProbe : MonoBehaviour
    {
        [InlineEditor] public InlineCollectionAssetProbe[] entries = new InlineCollectionAssetProbe[0];
    }
    public class DerivedShowIfAttribute : ShowIfAttribute
    {
        public DerivedShowIfAttribute(string member) : base(member, true) { }
    }
    [Serializable] public class CustomConditionElement
    {
        public bool enabled;
        [DerivedShowIf(nameof(enabled)), ScriptRowMarker] public int value;
    }
    public class ConditionalCollectionProbe : MonoBehaviour
    {
        public CustomConditionElement[] entries = { new CustomConditionElement() };
    }
    public class ActionProbe : MonoBehaviour { public int value; }
    public class ActionProbeEditor : InspectorGroupsEditor<ActionProbe>
    {
        public bool actionEnabled = true;
        public int calls;
        protected override void BuildInspector(InspectorContent content)
        {
            content.Action("Execute", () =>
            {
                calls++;
                Undo.RecordObjects(targets, "Inspector action test");
                foreach (ActionProbe probe in targets) probe.value++;
            }, actionEnabled);
        }
    }
    public class InteractionProbeWindow : EditorWindow
    {
        public UnityEditor.Editor inspector;
        public Rect lastRect;
        public Exception error;
        private void OnGUI()
        {
            try
            {
                inspector.OnInspectorGUI();
                if (Event.current.type == EventType.Repaint) lastRect = GUILayoutUtility.GetLastRect();
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { error = exception; }
        }
    }
    public class InspectorInteractionRegressionTests
    {
        [Test]
        public void BaseFieldGroup_DoesNotHideDerivedPropertyAndButton()
        {
            InheritedMemberProbe.reads = 0;
            WithInspector(typeof(InheritedMemberProbe), null, (window, editor) =>
                Assert.That(InheritedMemberProbe.reads, Is.GreaterThan(0)));
        }
        [Test]
        public void BaseMemberGroup_DoesNotHideDerivedProperty()
        {
            MemberGroupDerivedProbe.reads = 0;
            WithInspector(typeof(MemberGroupDerivedProbe), null, (window, editor) =>
                Assert.That(MemberGroupDerivedProbe.reads, Is.GreaterThan(0)));
        }
        [TestCase(true)]
        [TestCase(false)]
        public void DeclaredAction_RespectsEnabledStateAndUndo(bool enabled)
        {
            WithInspector(typeof(ActionProbe), typeof(ActionProbeEditor), (window, inspector) =>
            {
                var editor = (ActionProbeEditor)inspector;
                editor.actionEnabled = enabled;
                Draw(window);
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Click(window, window.lastRect.center);
                Assert.That(window.error, Is.Null, window.error?.ToString());
                Assert.That(editor.calls, Is.EqualTo(enabled ? 1 : 0));
                Assert.That(((ActionProbe)editor.target).value, Is.EqualTo(enabled ? 1 : 0));
                if (enabled)
                {
                    Undo.FlushUndoRecordObjects();
                    Undo.CollapseUndoOperations(group);
                    Undo.PerformUndo();
                    Assert.That(((ActionProbe)editor.target).value, Is.Zero);
                }
            });
        }
        [Test]
        public void CollectionInlineEditor_StillDrawsEachReferencedAsset()
        {
            var asset = ScriptableObject.CreateInstance<InlineCollectionAssetProbe>();
            try
            {
                WithInspector(typeof(InlineCollectionProbe), null, (window, editor) =>
                {
                    ((InlineCollectionProbe)editor.target).entries = new[] { asset };
                    editor.serializedObject.Update();
                    editor.serializedObject.FindProperty("entries").isExpanded = true;
                    InlineCollectionAssetProbe.reads = 0;
                    Draw(window);
                    Assert.That(window.error, Is.Null, window.error?.ToString());
                    Assert.That(InlineCollectionAssetProbe.reads, Is.GreaterThan(0));
                });
            }
            finally { Object.DestroyImmediate(asset); }
        }
        [Test]
        public void CollectionElement_RespectsDerivedConditionAttribute()
        {
            WithInspector(typeof(ConditionalCollectionProbe), null, (window, editor) =>
            {
                editor.serializedObject.FindProperty("entries").isExpanded = true;
                editor.serializedObject.FindProperty("entries").GetArrayElementAtIndex(0).isExpanded = true;
                ScriptRowMarkerDrawer.draws = 0;
                Draw(window);
                Assert.That(ScriptRowMarkerDrawer.draws, Is.Zero);
                ((ConditionalCollectionProbe)editor.target).entries[0].enabled = true;
                ScriptRowMarkerDrawer.draws = 0;
                Draw(window);
                Assert.That(window.error, Is.Null, window.error?.ToString());
                Assert.That(ScriptRowMarkerDrawer.draws, Is.GreaterThan(0));
            });
        }

        [Test]
        public void IdentifierActions_PreserveMultiSelectionCopyAndUndo()
        {
            Type type = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType("ObjectIdentifier");
                if (type != null) break;
            }
            if (type == null) Assert.Ignore("未安装对象工具包。");
            var firstHost = new GameObject("Identifier first");
            var secondHost = new GameObject("Identifier second");
            var first = firstHost.AddComponent(type);
            var second = secondHost.AddComponent(type);
            var property = type.GetProperty("UniqueId");
            string beforeFirst = (string)property.GetValue(first);
            string beforeSecond = (string)property.GetValue(second);
            string clipboard = EditorGUIUtility.systemCopyBuffer;
            var editor = UnityEditor.Editor.CreateEditor(new[] { first, second });
            try
            {
                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                editor.GetType().GetMethod("RegenerateSelectedIds", flags).Invoke(editor, null);
                string updatedFirst = (string)property.GetValue(first);
                string updatedSecond = (string)property.GetValue(second);
                Assert.That(updatedFirst, Is.Not.EqualTo(beforeFirst));
                Assert.That(updatedSecond, Is.Not.EqualTo(beforeSecond));
                editor.GetType().GetMethod("CopySelectedIds", flags).Invoke(editor, null);
                Assert.That(EditorGUIUtility.systemCopyBuffer, Is.EqualTo(updatedFirst + Environment.NewLine + updatedSecond));
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Undo.PerformUndo();
                Assert.That(property.GetValue(first), Is.EqualTo(beforeFirst));
                Assert.That(property.GetValue(second), Is.EqualTo(beforeSecond));
            }
            finally
            {
                EditorGUIUtility.systemCopyBuffer = clipboard;
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(firstHost);
                Object.DestroyImmediate(secondHost);
            }
        }

        private static void WithInspector(Type type, Type editorType, Action<InteractionProbeWindow, UnityEditor.Editor> check)
        {
            var host = new GameObject("Inspector interaction test");
            var target = host.AddComponent(type);
            var editor = UnityEditor.Editor.CreateEditor(target, editorType);
            var window = ScriptableObject.CreateInstance<InteractionProbeWindow>();
            try
            {
                window.inspector = editor;
                window.ShowUtility();
                window.position = new Rect(40, 40, 700, 700);
                Draw(window);
                Assert.That(window.error, Is.Null, window.error?.ToString());
                check(window, editor);
            }
            finally { window.Close(); Object.DestroyImmediate(editor); Object.DestroyImmediate(host); }
        }
        private static void Draw(InteractionProbeWindow window)
        {
            window.SendEvent(new Event { type = EventType.Layout });
            window.SendEvent(new Event { type = EventType.Repaint });
        }
        private static void Click(InteractionProbeWindow window, Vector2 position)
        {
            foreach (var type in new[] { EventType.MouseDown, EventType.MouseUp })
            {
                window.SendEvent(new Event { type = EventType.Layout });
                try { window.SendEvent(new Event { type = type, button = 0, mousePosition = position }); }
                catch (ExitGUIException) { }
            }
        }
    }
}
