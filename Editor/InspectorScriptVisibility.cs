using UnityEngine;

namespace Yvsim.InspectorGroups.Editor
{
    /// <summary>统一查询 Script 行的展示策略，也可供自定义 Inspector 接入。</summary>
    public static class InspectorScriptVisibility
    {
        /// <summary>仅当全部有效目标的类都声明或继承了隐藏Script时隐藏，混合选择保留 Script 行。</summary>
        public static bool ShouldHideScript(Object[] targets)
        {
            if (targets == null || targets.Length == 0) return false;
            foreach (Object target in targets)
                if (target == null || !target.GetType().IsDefined(typeof(HideScriptAttribute), true)) return false;
            return true;
        }
    }
}
