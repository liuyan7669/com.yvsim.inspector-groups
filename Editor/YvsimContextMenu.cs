using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>统一 Yvsim 右键菜单的位置与禁用项显示，不引用任何业务包。</summary>
    [InitializeOnLoad]
    internal static class YvsimContextMenu
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type MenuType = typeof(EditorApplication).Assembly.GetType("UnityEditor.Menu");
        private static readonly MethodInfo GetItems = MenuType?.GetMethod("GetMenuItems", Flags, null,
            new[] { typeof(string), typeof(bool), typeof(bool) }, null);
        private static readonly MethodInfo GetEnabled = MenuType?.GetMethod("GetEnabled", Flags);
        private static readonly FieldInfo GlobalHandler = typeof(EditorApplication).GetField("globalEventHandler", Flags);
        private static readonly FieldInfo ItemsField = typeof(GenericMenu).GetField("m_MenuItems", Flags);
        private static readonly EditorApplication.CallbackFunction Callback = OnContextClick;
        // 自动验证可以截获最终菜单，不弹出会阻塞测试的原生菜单窗口。
        internal static Action<GenericMenu> DisplayOverride;

        static YvsimContextMenu()
        {
            if (GlobalHandler == null || GetItems == null || ItemsField == null) return;
            GlobalHandler.SetValue(null, Delegate.Combine((Delegate)GlobalHandler.GetValue(null), Callback));
            AssemblyReloadEvents.beforeAssemblyReload += Unsubscribe;
            EditorApplication.quitting += Unsubscribe;
        }

        private static void Unsubscribe()
        {
            if (GlobalHandler != null)
                GlobalHandler.SetValue(null, Delegate.Remove((Delegate)GlobalHandler.GetValue(null), Callback));
        }

        private static void OnContextClick()
        {
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.ContextClick) return;
            EditorWindow window = EditorWindow.focusedWindow ?? EditorWindow.mouseOverWindow;
            if (window == null) return;
            string name = window.GetType().FullName;
            try
            {
                GenericMenu menu;
                if (name == "UnityEditor.SceneHierarchyWindow")
                {
                    if (!HasYvsimEntries("GameObject")) return;
                    var hierarchy = Member(window, "sceneHierarchy") ?? Member(window, "m_SceneHierarchy");
                    if (hierarchy == null || !(Member(hierarchy, "treeViewRect") is Rect rect) || !rect.Contains(evt.mousePosition)) return;
                    var tree = Member(hierarchy, "treeView");
                    var selection = tree?.GetType().GetMethod("GetSelection", Flags)?.Invoke(tree, null) as int[];
                    // 场景标题的专用菜单保持 Unity 原样；这里只处理物体和空白处。
                    if (selection != null && selection.Length > 0 &&
                        selection.All(id => EditorUtility.InstanceIDToObject(id) == null)) return;
                    menu = BuildHierarchy(hierarchy, Selection.activeGameObject);
                }
                else if (name == "UnityEditor.ProjectBrowser")
                {
                    if (!HasYvsimEntries("Assets")) return;
                    bool inTree = Member(window, "m_TreeViewRect") is Rect treeRect && treeRect.Contains(evt.mousePosition);
                    bool inList = Member(window, "m_ListAreaRect") is Rect listRect && listRect.Contains(evt.mousePosition);
                    if (!inTree && !inList) return;
                    var folderContext = window.GetType().GetField("isFolderTreeViewContextClicked", Flags);
                    folderContext?.SetValue(window, inTree && Member(window, "m_ViewMode")?.ToString() == "TwoColumns");
                    menu = BuildProject();
                }
                else return;
                evt.Use();
                if (DisplayOverride != null) DisplayOverride(menu);
                else menu.ShowAsContext();
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception)
            {
                // 未识别的 Unity 内部接口不接管事件，仍由原菜单处理。
                Debug.LogWarning("Yvsim 菜单未接管此窗口：" + exception.GetBaseException().Message);
            }
        }

        internal static GenericMenu BuildHierarchy(object hierarchy, GameObject context)
        {
            var menu = new GenericMenu();
            if (context != null)
            {
                var build = hierarchy.GetType().GetMethod("CreateGameObjectContextClick", Flags);
                if (build == null) throw new MissingMethodException("SceneHierarchy.CreateGameObjectContextClick");
                build.Invoke(hierarchy, new object[] { menu, context.GetInstanceID() });
            }
            else AddNativeItems(menu, "GameObject", "");
            var items = (IList)ItemsField.GetValue(menu);
            for (int i = items.Count - 1; i >= 0; i--)
                if (Content(items[i]).StartsWith("Yvsim/", StringComparison.Ordinal)) items.RemoveAt(i);
            var yvsim = new GenericMenu();
            AddNativeItems(yvsim, "GameObject", "Yvsim/");
            var insert = (IList)ItemsField.GetValue(yvsim);
            int position = 0;
            // 放在原生编辑操作之后、Create Empty 之前。
            for (int i = 0; i < items.Count; i++)
                if (Content(items[i]) == "Create Empty") { position = i; break; }
            foreach (object item in insert) items.Insert(position++, item);
            CleanSeparators(menu);
            return menu;
        }

        internal static GenericMenu BuildProject()
        {
            var menu = new GenericMenu();
            AddNativeItems(menu, "Assets", "");
            var items = (IList)ItemsField.GetValue(menu);
            var yvsim = new List<object>();
            for (int i = items.Count - 1; i >= 0; i--)
                if (Content(items[i]).StartsWith("Yvsim/", StringComparison.Ordinal))
                { yvsim.Insert(0, items[i]); items.RemoveAt(i); }
            int position = items.Count;
            for (int i = 0; i < items.Count; i++)
                if (Content(items[i]) == "View in Package Manager") { position = i; break; }
            foreach (object item in yvsim) items.Insert(position++, item);
            CleanSeparators(menu);
            return menu;
        }

        private static bool HasYvsimEntries(string root)
        {
            foreach (object item in (Array)GetItems.Invoke(null, new object[] { root, false, false }))
                if ((Member(item, "path") as string)?.StartsWith(root + "/Yvsim/", StringComparison.Ordinal) == true)
                    return true;
            return false;
        }

        private static void AddNativeItems(GenericMenu menu, string root, string prefix)
        {
            foreach (object item in (Array)GetItems.Invoke(null, new object[] { root, false, false }))
            {
                string path = Member(item, "path") as string;
                if (string.IsNullOrEmpty(path) || !path.StartsWith(root + "/", StringComparison.Ordinal)) continue;
                string relative = path.Substring(root.Length + 1);
                if (!relative.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (Member(item, "isSeparator") is bool separator && separator) continue;
                bool enabled = GetEnabled == null || (bool)GetEnabled.Invoke(null, new object[] { path });
                if (enabled)
                {
                    string command = path;
                    menu.AddItem(new GUIContent(relative), false, () => EditorApplication.ExecuteMenuItem(command));
                }
                else menu.AddDisabledItem(new GUIContent(relative));
            }
        }

        private static string Content(object item) => (Member(item, "content") as GUIContent)?.text ?? "";

        private static void CleanSeparators(GenericMenu menu)
        {
            var type = typeof(EditorApplication).Assembly.GetType("UnityEditor.MenuUtils");
            type?.GetMethod("RemoveInvalidSeparators", Flags)?.Invoke(null, new object[] { menu });
        }

        private static object Member(object owner, string name)
        {
            return owner.GetType().GetProperty(name, Flags)?.GetValue(owner, null) ??
                owner.GetType().GetField(name, Flags)?.GetValue(owner);
        }
    }
}
