using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Yvsim.InspectorGroups.Editor;
using Object = UnityEngine.Object;

namespace Yvsim.InspectorGroups.Tests
{
    public class InheritedGroupBaseProbe : MonoBehaviour
    {
        [FoldoutGroup("Base only", false)] public int baseValue = 1;
    }
    public class InheritedGroupDerivedProbe : InheritedGroupBaseProbe
    {
        [ScriptRowMarker] public int derivedValue = 2;
    }

    public class DeclaredContentProbeEditor : InspectorGroupsEditor<InheritedGroupDerivedProbe>
    {
        protected override void BuildInspector(InspectorContent content)
        {
            content.Group("Business", "Business", section =>
            {
                section.Value("State", "Ready");
                section.Message("Available");
                section.Property("baseValue", true);
                section.Action("Disabled action", () => throw new Exception("Disabled action executed"), false);
            });
        }
    }

    public class InspectorDeclarationRegressionTests
    {
        [Test]
        public void CollapsedBaseGroup_DoesNotHideDerivedFields()
        {
            Draw(null);
        }

        [Test]
        public void DeclarativeBusinessContent_RendersAlongsideNativeFields()
        {
            Draw(typeof(DeclaredContentProbeEditor));
        }

        private static void Draw(Type editorType)
        {
            var host = new GameObject("Inspector declaration regression");
            var component = host.AddComponent<InheritedGroupDerivedProbe>();
            var editor = UnityEditor.Editor.CreateEditor(component, editorType);
            var window = ScriptableObject.CreateInstance<InspectorBindingTestWindow>();
            try
            {
                ScriptRowMarkerDrawer.draws = 0;
                window.inspector = editor;
                window.ShowUtility();
                window.position = new Rect(40, 40, 700, 700);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                Assert.That(window.error, Is.Null, window.error?.ToString());
                Assert.That(ScriptRowMarkerDrawer.draws, Is.GreaterThan(0), "Base foldout captured the derived field");
                Assert.That(component.derivedValue, Is.EqualTo(2));
                Assert.That(component.baseValue, Is.EqualTo(1));
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(host);
            }
        }
    }
}
