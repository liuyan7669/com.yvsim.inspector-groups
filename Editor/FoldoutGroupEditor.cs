using UnityEditor;
using UnityEngine;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>供自定义 Inspector 可选接入的特性绘制入口，不依赖任何业务包。</summary>
    public sealed class InspectorGroupRenderer : System.IDisposable
    {
        private readonly InspectorAttributeGUI attributeGUI = new InspectorAttributeGUI();

        /// <summary>绘制未排除的顶层字段及带特性的成员；调用方负责 Update 和 ApplyModifiedProperties。</summary>
        /// <param name="inspector">持有所绘制对象的 Inspector。</param>
        /// <param name="excludedProperties">由调用方自行绘制的顶层属性路径。</param>
        public void DrawProperties(UnityEditor.Editor inspector, string[] excludedProperties)
        {
            attributeGUI.DrawProperties(inspector.serializedObject, null, inspector, true, excludedProperties);
        }

        /// <summary>释放缓存的内联编辑器。</summary>
        public void Dispose()
        {
            attributeGUI.Dispose();
        }
    }

    /// <summary>为没有专用 Inspector 的组件绘制分组和可组合的嵌套特性。</summary>
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class FoldoutGroupEditor : UnityEditor.Editor
    {
        private readonly InspectorAttributeGUI attributeGUI = new InspectorAttributeGUI();

        /// <summary>释放内联资源编辑器。</summary>
        protected virtual void OnDisable()
        {
            attributeGUI.Dispose();
        }

        /// <summary>递归绘制字段、分组、内联对象和方法按钮。</summary>
        public override void OnInspectorGUI()
        {
            if (target != null) attributeGUI.DrawInspector(this);
        }
    }

    /// <summary>为没有专用 Inspector 的 ScriptableObject 提供相同的折叠分组。</summary>
    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class FoldoutGroupScriptableObjectEditor : FoldoutGroupEditor
    {
    }
}
