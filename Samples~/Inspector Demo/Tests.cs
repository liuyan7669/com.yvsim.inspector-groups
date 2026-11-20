using System;
using System.Collections.Generic;
using Yvsim.InspectorGroups;
using UnityEngine;

/// <summary>
/// 软件唯一测试脚本
/// </summary>
[ExecuteAlways]
public class Tests : MonoBehaviour
{

    /// <summary>包内配置显式选择框架样式。</summary>
    [FoldoutGroup("框架配置（样式示例）", true, style: InspectorGroupStyle.Framework)]
    [Tooltip("框架样式分组中的开关示例，用于观察框架配置的分组外观。")]
    [SerializeField] private bool frameworkOption = true;

    /// <summary>用户代码不指定样式时使用普通分组。</summary>
    [FoldoutGroup("业务配置（默认样式）", true)]
    [Tooltip("普通业务分组中的标题文本示例，用于观察默认分组样式。")]
    [SerializeField] private string businessTitle = "我的业务配置";

    /// <summary>原有测试索引。</summary>
    [FoldoutGroup("测试")]
    [Tooltip("整数输入示例，用于检查普通数值字段在分组中的显示与编辑。")]
    public int index;
    /// <summary>原有名称测试。</summary>
    [Header("名字")][Tooltip("这是名字")] public string name;
    /// <summary>原有数值列表。</summary>
    [Tooltip("整数列表示例，可添加、删除和调整元素以检查列表绘制。")]
    public List<int> list = new List<int>();
    /// <summary>原有对象列表。</summary>
    [Tooltip("场景对象引用列表示例，可拖入 GameObject 检查集合中的对象选择。")]
    public List<GameObject> listGo = new List<GameObject>();

    [FoldoutGroup("01 · ReadOnly 只读", spacing: 8f)]
    [ReadOnly, Tooltip("不能手动修改；点击底部的增加进度按钮会更新此值。")]
    [SerializeField] private int buttonClickCount;

    [FoldoutGroup("02 · ShowIf 条件显示", spacing: 8f)]
    [Tooltip("切换开关，观察下面两段文字交替出现。")]
    [SerializeField] private bool showDetails = true;

    [ShowIf(nameof(showDetails)), Tooltip("ShowIf 的默认条件为 true。")]
    [SerializeField] private string visibleWhenTrue = "开关开启时显示";

    [ShowIf(nameof(showDetails), false)]
    [Tooltip("仅在 Show Details 关闭时显示的文本，用于演示 ShowIf 的反向条件。")]
    [SerializeField] private string visibleWhenFalse = "开关关闭时显示";

    [FoldoutGroup("03 · EnableIf 条件启用", spacing: 8f)]
    [Tooltip("关闭后数值仍然可见，但不能编辑。也控制底部的条件按钮。")]
    [SerializeField] private bool canEdit = true;

    [EnableIf(nameof(canEdit)), Range(0, 100)]
    [Tooltip("仅在 Can Edit 开启时允许修改的数值示例，范围为 0 到 100。")]
    [SerializeField] private int editableValue = 50;

    [FoldoutGroup("04 · Required 必填提示", spacing: 8f)]
    [Required("填写文字后，此提示会消失。")]
    [Tooltip("必填文本示例；为空时显示提示，填写后提示消失。")]
    [SerializeField] private string requiredText;

    [Required("拖入任意场景对象，观察空引用提示消失。")]
    [Tooltip("必填对象引用示例；拖入 GameObject 后空引用提示消失。")]
    [SerializeField] private GameObject requiredObject;

    [FoldoutGroup("05 · InlineEditor 内联编辑", spacing: 8f)]
    [InlineEditor, Required, Tooltip("自动生成的临时配置，可直接修改其字段并点击内部按钮。")]
    [SerializeField] private InlineSampleData inlineSample;

    [ReadOnly, InlineEditor, Tooltip("引用同一份配置。这里的内容与上方同步，但不能编辑。")]
    [SerializeField] private InlineSampleData readOnlyInlineSample;

    [FoldoutGroup("06 · AssetPreview 资源预览", spacing: 8f)]
    [AssetPreview(96f), Tooltip("自动生成的测试纹理，也可以拖入自己的 Texture2D 查看。")]
    [SerializeField] private Texture2D previewTexture;

    [FoldoutGroup("07 · Dropdown 下拉来源", spacing: 8f)]
    [Tooltip("可编辑选项集合，下面三个下拉框分别通过字段、属性和方法读取它。")]
    [SerializeField] private string[] moduleOptions = { "模块一", "模块二", "模块三" };

    [Dropdown(nameof(moduleOptions)), Tooltip("选项来自字段。")]
    [SerializeField] private string fieldOption = "模块一";

    [Dropdown(nameof(ModuleOptions)), Tooltip("选项来自属性。")]
    [SerializeField] private string propertyOption = "模块二";

    [Dropdown(nameof(GetModuleOptions)), Tooltip("选项来自无参方法。")]
    [SerializeField] private string methodOption = "模块三";

    [FoldoutGroup("08 · ProgressBar 进度条", spacing: 8f)]
    [Range(0f, 100f), ProgressBar(0f, 100f, 22f)]
    [Tooltip("拖动原生 Range 滑条，观察下方进度条；也可使用底部的增加进度按钮。")]
    [SerializeField] private float progress = 35f;

    [FoldoutGroup("09 · ShowAsString 文本展示", spacing: 8f)]
    [ShowAsString("只读文本"), Tooltip("此字段使用文本展示，不能直接编辑。")]
    [SerializeField] private string textSample = "ShowAsString 与普通输入框的区别";

    [TextArea(2, 4), ShowAsString("文本摘要"), Tooltip("原生文本框与只读展示同时生效。")]
    [SerializeField] private string editableNote = "修改上方文本框，观察只读摘要。";

    [FoldoutGroup("10 · 嵌套与组合", spacing: 8f)]
    [Tooltip("展开后可测试嵌套分组、局部条件、多种展示和嵌套按钮。")]
    [SerializeField] private NestedSample nestedSample = new NestedSample();

    [Tooltip("展开元素，内部仍支持全部适用特性。")]
    [SerializeField] private List<NestedSample> nestedSamples = new List<NestedSample> { new NestedSample() };

    [Dropdown(nameof(GetScores)), ProgressBar(0, 100), ShowAsString, Range(0, 100), Min(10)]
    [Tooltip("列表的每个元素同时显示原生滑条、下拉框、进度条与文本。")]
    [SerializeField] private List<int> combinedScores = new List<int> { 25, 75 };

    [ReadOnly, Tooltip("父级 ReadOnly 会同时禁用内部字段和按钮。")]
    [SerializeField] private NestedSample readOnlyNestedSample = new NestedSample();

    [FoldoutGroup("12 · 字段外观与选择器", spacing: 8f, color: "#4FC3F7", border: true)]
    [Title("字段外观")]
    [InfoBox("拖动彩色进度条或双端滑块，修改下方数值。")]
    [Divider("新消息")]
    [Label("生命值"), ProgressBar(100f, "#E53935")]
    [Tooltip("生命值进度条示例，最大值为 100；点击回满按钮会恢复此值。")]
    [SerializeField] private float health = 80f;

    [ColorField("#FFD54F", true), Line(LinePosition.Both)]
    [Tooltip("角色名称示例，用于演示字段着色和分隔线；调试按钮会打印此名称。")]
    [SerializeField] private string characterName = "Hero";

    [MinMaxSlider(0f, 100f)]
    [Tooltip("区间选择示例，X 为下界、Y 为上界；可在 0 到 100 之间拖动双端滑块。")]
    [SerializeField] private Vector2 damageRange = new Vector2(10f, 30f);

    [ReadOnly, Label("唯一 ID")]
    [Tooltip("只读整数示例，用于演示自定义标签和禁止在 Inspector 中编辑。")]
    [SerializeField] private int characterId = 1001;

    [ColorField(1f, 0.5f, 0.5f)]
    [Tooltip("数值输入示例，用于演示字段背景着色以强调配置。")]
    [SerializeField] private float importantValue = 1f;

    [ProgressBar(50f, "#42A5F5")]
    [Tooltip("法力值进度条示例，最大值为 50；点击回满按钮会恢复此值。")]
    [SerializeField] private int mana = 30;

    [Title("项目选择器", "#AB47BC")]
    [Divider("这里是一段更长的分隔文字", textColor: "#FFCA80", lineColor: "#4488CC", thickness: 2f, fontSize: 15)]
    [Tag]
    [Tooltip("从项目已定义的 Tag 中选择一个标签，用于演示标签选择器。")]
    [SerializeField] private string targetTag = "Untagged";

    [Layer]
    [Tooltip("从项目 Layer 列表选择层级，保存其编号；仅演示选择器。")]
    [SerializeField] private int targetLayer;

    [SceneName]
    [Tooltip("从 Build Settings 中启用的场景选择名称；此示例不会自动切换场景。")]
    [SerializeField] private string nextScene;

    private readonly string[] difficulties = { "Easy", "Normal", "Hard" };
    private readonly float[] speedOptions = { 0.5f, 1f, 2f, 4f };

    [Dropdown(nameof(difficulties))]
    [Tooltip("难度下拉框示例，选项来自预设的 difficulties 字符串数组。")]
    [SerializeField] private string difficulty = "Normal";

    [Dropdown(nameof(speedOptions))]
    [Tooltip("速度下拉框示例，选项来自 speedOptions 数组；此值不驱动实际移动。")]
    [SerializeField] private float speed = 1f;

    [Flags]
    private enum Element { None = 0, Fire = 1, Water = 2, Wind = 4, Earth = 8 }

    [EnumFlags]
    [Tooltip("枚举标志示例，可同时选择火、水、风、土等多个元素。")]
    [SerializeField] private Element elements = Element.Fire | Element.Wind;

    private enum AttackType { Melee, Ranged, Magic }

    [Tooltip("攻击类型示例，用于控制射程、法力消耗和投射物速度字段是否显示。")]
    [SerializeField] private AttackType attackType;

    [ShowIf(nameof(attackType), AttackType.Ranged, AttackType.Magic)]
    [Tooltip("射程示例，仅在攻击类型为远程或魔法时显示。")]
    [SerializeField] private float attackRange = 10f;

    [ShowIf(nameof(attackType), AttackType.Magic)]
    [Tooltip("法力消耗示例，仅在攻击类型为魔法时显示。")]
    [SerializeField] private int manaCost = 5;

    [HideIf(nameof(attackType), AttackType.Melee)]
    [Tooltip("投射物速度示例；近战类型下隐藏，仅演示条件显示。")]
    [SerializeField] private float projectileSpeed = 5f;

    [Title("分割线位置", "#26A69A")]
    [Line]
    [Tooltip("上方分隔线的数值示例，用于检查默认 Line 样式。")]
    [SerializeField] private int lineAbove = 1;

    [Line("#FF7043", 2f, LinePosition.Below)]
    [Tooltip("下方分隔线的数值示例，用于检查指定颜色和粗细的 Line 样式。")]
    [SerializeField] private int lineBelow = 2;

    [Line("#29B6F6", 2f, LinePosition.Both)]
    [Tooltip("上下两侧分隔线的数值示例，用于检查 Line 的 Both 模式。")]
    [SerializeField] private int lineBoth = 3;

    [FoldoutGroup("12a · 移动与音效", color: "#4FC3F7")]
    [Tooltip("移动速度配置示例，仅用于演示分组内的数值字段，不驱动角色移动。")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("跳跃高度配置示例，仅用于演示数值字段，不执行跳跃。")]
    [SerializeField] private float jumpHeight = 2f;
    [Tooltip("二段跳开关示例；开启后显示 Double Jump Height 字段。")]
    [SerializeField] private bool canDoubleJump;

    [ShowIf(nameof(canDoubleJump))]
    [Tooltip("二段跳高度配置示例，仅在 Can Double Jump 开启时显示。")]
    [SerializeField] private float doubleJumpHeight = 1f;

    [Tooltip("音效引用示例，可指定 AudioClip；此演示脚本不会播放该音效。")]
    [SerializeField] private AudioClip hitSound;

    [Range(0f, 1f)]
    [Tooltip("音量滑条示例，范围为 0 到 1；此演示脚本不控制实际音源音量。")]
    [SerializeField] private float volume = 1f;

    [FoldoutGroup("12b · 战斗与外观", expanded: false, color: "#EF5350")]
    [Tooltip("攻击力数值示例，仅用于展示战斗配置分组。")]
    [SerializeField] private int attack = 10;

    [ProgressBar(100f, "#FFA726")]
    [Tooltip("防御值进度条示例，最大值为 100，可拖动彩色进度条修改。")]
    [SerializeField] private int defense = 40;

    [ReadOnly]
    [Tooltip("暴击率配置的只读示例；Inspector 中可查看但不能修改。")]
    [SerializeField] private float critRate = 0.15f;

    [Tooltip("护盾开关示例；开启时显示护盾值，关闭时显示闪避率。")]
    [SerializeField] private bool hasShield;

    [ShowIf(nameof(hasShield))]
    [Tooltip("护盾值配置示例，仅在 Has Shield 开启时显示。")]
    [SerializeField] private float shieldValue = 50f;

    [HideIf(nameof(hasShield)), InfoBox("没有护盾时显示。", InfoBoxType.Warning)]
    [Tooltip("闪避率配置示例，仅在没有护盾时显示，并带有说明提示。")]
    [SerializeField] private float dodgeRate = 0.1f;

    [Required, AssetPreview(64f)]
    [Tooltip("必填的武器预制体引用示例，同时显示资源预览。")]
    [SerializeField] private GameObject weaponPrefab;

    [FoldoutGroup("12c · 资源预览与调试", color: "#42A5F5")]
    [AssetPreview(64f)]
    [Tooltip("图标 Sprite 引用示例，下方显示较大的资源预览。")]
    [SerializeField] private Sprite icon;

    [AssetPreview(48f)]
    [Tooltip("头像 Sprite 引用示例，下方显示较小的资源预览。")]
    [SerializeField] private Sprite avatar;

    [Tooltip("颜色选择器示例；此值不自动应用到场景渲染对象。")]
    [SerializeField] private Color tint = Color.white;

    [Tooltip("调试开关示例，控制 Tick Rate 字段和打印角色状态按钮是否显示。")]
    [SerializeField] private bool debugMode;

    [ShowIf(nameof(debugMode))]
    [Tooltip("更新间隔配置示例，仅在 Debug Mode 开启时显示，不实际调度更新。")]
    [SerializeField] private float tickRate = 0.02f;

    [EndFoldout]
    [InfoBox("此字段位于折叠分组之外。")]
    [Tooltip("折叠组外的文本字段示例，收起上方分组后仍然显示。")]
    [SerializeField] private string ungroupedNote = "收起上方分组后，这一项仍可见。";

    [FoldoutGroup("ShowAsString · 未序列化数据", spacing: 8f)]
    [ShowAsString("字典示例")]
    [Tooltip("以只读文本展示未序列化的字典，格式为键和值，不支持直接编辑。")]
    private readonly Dictionary<string, int> sampleDictionary = new Dictionary<string, int>
    {
        { "示例一", 10 },
        { "示例二", 20 }
    };

    /// <summary>实时显示按钮次数和当前进度。</summary>
    [ShowAsString("实时摘要", Tooltip = "显示当前按钮点击次数和进度值，随示例操作实时更新。")]
    public string Summary => $"按钮次数：{buttonClickCount}；进度：{progress:0}%";

    private string[] ModuleOptions => moduleOptions;
    private InlineSampleData generatedInlineSample;
    private Texture2D generatedTexture;

    private string[] GetModuleOptions()
    {
        return moduleOptions;
    }

    private int[] GetScores()
    {
        return new[] { 0, 10, 25, 50, 75, 100, 150 };
    }

    private void OnEnable()
    {
        // 演示对象仅驻留内存，不创建资源文件；禁用组件时只清理由本组件生成的对象。
        if (inlineSample == null)
        {
            generatedInlineSample = ScriptableObject.CreateInstance<InlineSampleData>();
            generatedInlineSample.name = "临时内联配置";
            generatedInlineSample.hideFlags = HideFlags.HideAndDontSave;
            inlineSample = generatedInlineSample;
        }
        if (readOnlyInlineSample == null)
        {
            readOnlyInlineSample = inlineSample;
        }
        if (previewTexture == null)
        {
            generatedTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            generatedTexture.name = "临时预览纹理";
            generatedTexture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    pixels[y * 64 + x] = new Color(x / 63f, y / 63f, (x / 8 + y / 8) % 2 == 0 ? 0.25f : 0.75f);
                }
            }
            generatedTexture.SetPixels(pixels);
            generatedTexture.Apply();
            previewTexture = generatedTexture;
        }
    }

    private void OnDisable()
    {
        if (inlineSample == generatedInlineSample) inlineSample = null;
        if (readOnlyInlineSample == generatedInlineSample) readOnlyInlineSample = null;
        if (previewTexture == generatedTexture) previewTexture = null;
        foreach (UnityEngine.Object generated in new UnityEngine.Object[] { generatedInlineSample, generatedTexture })
        {
            if (generated == null) continue;
            if (Application.isPlaying)
            {
                Destroy(generated);
            }
            else
            {
                DestroyImmediate(generated);
            }
        }
        generatedInlineSample = null;
        generatedTexture = null;
    }

    [FoldoutGroup("11 · Button 操作按钮", spacing: 8f)]
    [Button("进度 +10，并更新只读计数", height: 28f)]
    private void AdvanceProgress()
    {
        progress = progress >= 100f ? 0f : Mathf.Min(100f, progress + 10f);
        buttonClickCount++;
    }

    [Button("恢复进度与条件开关")]
    private void ResetDemo()
    {
        progress = 35f;
        buttonClickCount = 0;
        showDetails = true;
        canEdit = true;
    }

    [Button("EnableIf 条件按钮"), EnableIf(nameof(canEdit))]
    private void EnabledButton()
    {
        buttonClickCount++;
    }

    [Button("ShowIf 条件按钮"), ShowIf(nameof(showDetails))]
    private void VisibleButton()
    {
        buttonClickCount++;
    }

    [Button("回满生命和法力", height: 28f)]
    private void Heal()
    {
        health = 100f;
        mana = 50;
    }

    [Button("打印角色状态"), ShowIf(nameof(debugMode))]
    private void PrintCharacterState()
    {
        Debug.Log($"{characterName} HP={health} MP={mana}");
    }

    [Button("仅 Play Mode 可点击", playModeOnly: true)]
    private void PlayModeButton()
    {
        buttonClickCount++;
        Debug.Log("Button 的 Play Mode 限制测试通过。", this);
    }

    /// <summary>可在普通字段或列表元素中展开的组合示例。</summary>
    [Serializable]
    public class NestedSample
    {
        [FoldoutGroup("嵌套 · 条件与组合")]
        [Tooltip("当前嵌套对象的分数显示开关；关闭后隐藏该对象的 Score 字段。")]
        [SerializeField] private bool showScore = true;
        [Tooltip("当前嵌套对象的编辑开关；控制 Score 字段和重置分数按钮是否可用。")]
        [SerializeField] private bool allowEdit = true;

        [ShowIf(nameof(showScore)), EnableIf(nameof(allowEdit))]
        [Header("原生装饰与四种数值展示"), Space(4f), Tooltip("条件来源为当前嵌套对象，而不是外层 Tests。")]
        [Range(0, 100), Min(10), Dropdown(nameof(GetScores)), ProgressBar(0, 100), ShowAsString]
        [SerializeField] private int score = 50;

        [Required("输入内容后，嵌套字段的必填提示会消失。"), TextArea(2, 4)]
        [Tooltip("嵌套配置中的必填多行文本；填写后必填提示消失。")]
        [SerializeField] private string note;

        private int[] GetScores()
        {
            return new[] { 0, 10, 25, 50, 75, 100, 150 };
        }

        [FoldoutGroup("嵌套 · Button"), Button("重置嵌套分数"), EnableIf(nameof(allowEdit))]
        private void ResetScore()
        {
            score = 50;
        }
    }

    /// <summary>仅供当前组件临时创建和内联展示的配置对象。</summary>
    public class InlineSampleData : ScriptableObject
    {
        [FoldoutGroup("内联 · 嵌套特性")]
        [Required, Tooltip("可直接编辑这份临时配置。")]
        [SerializeField] private string displayName = "内联配置示例";

        [Range(0f, 100f), ProgressBar(0f, 100f)]
        [Tooltip("临时内联配置的数值，可在 0 到 100 之间调整并查看进度条。")]
        [SerializeField] private float value = 40f;

        [ReadOnly]
        [Tooltip("临时内联配置的只读修订号；点击修订号加一按钮可更新。")]
        [SerializeField] private int revision = 1;

        /// <summary>内联对象的实时文本展示。</summary>
        [ShowAsString("内联摘要", Tooltip = "显示当前内联配置的名称、数值和修订号，只读且实时更新。")]
        public string Summary => $"{displayName}，数值 {value:0}，修订 {revision}";

        [FoldoutGroup("内联 · Button"), Button("修订号 +1")]
        private void IncreaseRevision()
        {
            revision++;
        }
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
