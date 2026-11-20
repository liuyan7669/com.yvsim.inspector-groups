using System;

namespace Yvsim.InspectorGroups
{
    /// <summary>Inspector 分组的视觉样式，由 Inspector Groups 统一实现。</summary>
    public enum InspectorGroupStyle
    {
        /// <summary>普通业务分组，也是未指定样式时的默认值。</summary>
        Default,
        /// <summary>包内框架配置分组，使用加粗标题、中性底色和框架标识。</summary>
        Framework
    }

    /// <summary>从当前成员开始创建折叠分组，直到同一区域的下一个分组或成员列表结束。</summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class FoldoutGroupAttribute : Attribute
    {
        /// <summary>分组标题。</summary>
        public string Title { get; }

        /// <summary>首次显示时是否展开。</summary>
        public bool Expanded { get; }

        /// <summary>当前分组结束后到下一个分组的垂直间距，单位为 Inspector GUI 点。</summary>
        public float Spacing { get; }

        /// <summary>标题文字颜色。</summary>
        public UnityEngine.Color? Color { get; }

        /// <summary>是否绘制圆角框的边框；关闭后只保留圆角底色。</summary>
        public bool Border { get; }

        /// <summary>分组样式；普通用户代码默认使用 Default。</summary>
        public InspectorGroupStyle Style { get; }

        /// <summary>创建分组，可指定初始展开状态和到下一个分组的间距。</summary>
        public FoldoutGroupAttribute(string title, bool expanded = false, float spacing = 2f, string color = null, bool border = false, InspectorGroupStyle style = InspectorGroupStyle.Default)
        {
            Title = title;
            Expanded = expanded;
            Spacing = Math.Max(0f, spacing);
            if (!string.IsNullOrEmpty(color)) Color = ColorParser.Parse(color, UnityEngine.Color.white);
            Border = border;
            Style = style;
        }
    }
}
