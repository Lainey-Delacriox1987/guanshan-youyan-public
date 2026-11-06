# 关山有眼 Unity 资源迁移

本阶段范围：新建独立的 Unity 2D URP 工程，导入现有资源。H5 的 JavaScript 游戏逻辑没有被转换成 Unity 玩法。

## 已准备的资源快照

`../output/unity-resource-staging/` 包含：

- `Assets/GuanshanYouyan/Art/`：场景、人物、图集、剪纸和制作参考图。
- `Assets/GuanshanYouyan/Models/`：Quaternius 参考 FBX 和原授权文本。
- `Assets/GuanshanYouyan/Data/`：剧情、场景布局、角色骨骼数据、当前素材对应表和资源清单。
- `Assets/GuanshanYouyan/Documentation/`：已有策划和素材生成记录。
- `SourceArchive~/`：原始 WebP/AVIF/SVG、Blender 文件、数据脚本等制作源文件，保留原相对目录。

首份快照共 151 项美术/模型资源，其中 150 张图片、1 个 FBX；图片包括林简 34 帧（站姿、32 帧行走、立绘）和 4 个独立部件。

67 张 WebP/AVIF 图片转为 PNG，并校验解码后的 RGBA 像素一致；SVG 另行栅格化。PNG/JPG 原件按字节复制。所有资源记录来源和 SHA-256。

不重复导入 dist 发布产物、测试截图、画质基准截图、node_modules 或 Blender 自动备份。当前场景画是完整插画，不能直接当作自动拼接的地形 RuleTile 图集。

## 工具

- `prepare-resources.cjs`：生成新的资源快照；遇到已有快照会停止，避免覆盖。依赖本机 Codex 运行时的 sharp。
- `GuanshanResources.cs`：放到新工程的 `Assets/Editor/`。将资源图片按全分辨率 Sprite 导入；提供 `关山有眼/资源库` 和 `关山有眼/校验资源库` 菜单。
- `GuanshanYouyan.Editor.ResourceSetup.CreatePreview()`：通过编辑器 API 创建独立资源预览场景（已有场景不会覆盖）。

资源导入必须经 Unity 编辑器处理后，才有有效的 `.meta`、Sprite 和 FBX 资源。仅有上述暂存目录不代表已经完成 Unity 工程创建或编辑器验收。
