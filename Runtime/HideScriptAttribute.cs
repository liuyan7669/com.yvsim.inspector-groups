using System;

namespace Yvsim.InspectorGroups
{
    /// <summary>隐藏类在统一 Inspector 中的 Script 引用行，不改变其他字段和特性的绘制。</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class HideScriptAttribute : Attribute
    {
    }
}
