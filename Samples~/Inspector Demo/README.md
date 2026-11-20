# Inspector Demo

`Tests.cs` 是手动验收 demo，不是 NUnit 自动化测试。

组件顶部提供框架与普通分组的同屏对比：框架配置显式使用 `style: InspectorGroupStyle.Framework`；业务配置省略样式参数，沿用默认普通外观。两种分组独立折叠，其余示例继续展示普通默认用法。

1. 在 Package Manager 选择 **Inspector Groups**，在 Samples 中导入 **Inspector Demo**。
2. 在场景创建空 GameObject，添加 `Tests` 组件。
3. 在 Edit Mode 展开各个分组，观察只读、条件显示、条件启用、必填、预览、下拉、进度条和文本展示。
4. 展开第 12 组及 12a～12c，检查彩色进度条、双端滑块、字段装饰、选择器、条件显示和资源预览。比较第 12 组的两条 Divider，调整 Inspector 宽度观察线条如何伸缩。收起 12c，确认组外备注仍可见。
5. 点击进度和重置按钮，检查实时摘要；展开嵌套对象、列表和内联配置，检查组合特性与禁用状态。
6. 进入 Play Mode，检查仅运行时可点击的按钮。

组件带有 `ExecuteAlways`，临时 ScriptableObject 与纹理只驻留内存，禁用组件时会清理由自身生成的对象。完整参数和适用限制见包根目录 README。

演示组件类名为 `Tests`。同一工程只保留一份该 Sample，避免同名类型冲突。
