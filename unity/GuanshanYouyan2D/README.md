# 关山有眼·雾港来信 — Unity 2D

Unity 6000.3.27f1 LTS，Universal 2D 模板，URP 2D Renderer。已准备 Windows 和 WebGL 编辑器支持。

2026-10-29：依据 `docs/08-参考视频拆解与场景叙事修订.md` 和07号案情底稿，已扩展为四幕、八处场景的离线整版。按用户最新确认，许岚只保留固定话题，正常游戏不创建问答客户端、不探测模型服务，也没有自由输入入口；无需模型或密钥。实验接口仅留待将来另行启用。原 H5 继续保留原玩法和原存档。整版验收与限制见 `../../docs/10-Unity整版更改与验收.md`。

## 开始游玩

打开 `Assets/GuanshanYouyan/Scenes/FogHarborStoryV2.unity`，点击 Play，再点“开始新版故事”或“继续调查”。也可使用菜单“关山有眼 > 打开完整故事”。原 `PortStoryV2.unity` 及其试玩包继续保留为第一幕历史原型。

整版构建使用 `Builds/FullStory-windows-release` 和 `Builds/FullStory-web-release`。以对应 `ImportReports/full-build-*.json` 的实际成功记录为准。Windows需保留同目录的 `_Data`、UnityPlayer.dll和依赖文件；网页目录需用HTTP服务打开，不能双击HTML。旧 `Builds/PortStoryV2` 仅含第一幕。以上均为本地文件，不代表线上站点已更新。

完整工程可双击根目录的 `启动雾港来信本地试玩.cmd`，访问专用地址 `http://127.0.0.1:18743/guanshan-youyan/`。启动器复用本工程服务；遇到其他服务占用端口会明确报错，不结束其他项目。服务独立在后台运行，电脑重启后需再次双击启动。网页包自带 `Serve-Web.cjs`，在解压目录运行 `node Serve-Web.cjs . 18743` 后访问同一专用地址；它正确发送 Gzip 与 wasm 响应头，仅监听本机。

点击场景空白处行走；点击人物、物件或出口直接交互，无需先走近。入场说明是1.8秒自动消失的轻提示，不拦截输入。其他提示的“知道了”固定在可见底部，正文可滚动。网页右下角“全屏游玩”可切换全屏，画布始终填满可用区域。键盘 A/D、方向键移动，E 调查，Tab 打开手册，Esc 收起。正式对话和检查期间暂停移动。手册仅复看已取得资料，提示不会降低结局。

四幕依次是港口、仓库、小镇、调查中心；每幕含两处可往返场景。几何色块背景、现场立绘对白、物件检查和向许岚出示材料构成主要流程。线索板、人物资料、地图和有出处的知识卡均可选。结尾依次复盘当前异常、历史流转、人员参与和下一步；错误只退回具体段落并保留调查材料。生态线按实际观察、询问来源、提交核验分别收束。

整版存档为 `Application.persistentDataPath/guanshan-mist-story-v2.json`，含对话位置、分场景位置、朝向、材料、关系和结尾。记录恢复会重放并核对因果前置。写入使用临时文件与备份；损坏时尝试备份。新故事保留上一份 `.previous`。标题可明确导入旧第一幕试玩进度，保留原文件；不读取或改写H5旧存档。WebGL另有浏览器持久存储同步。

整版中立数据源为项目根目录 `story/story-v2.json`，Unity镜像为 `Assets/GuanshanYouyan/Data/StoryV2.json`，由 `node unity-tools/build-full-story.cjs` 同时生成。旧第一幕数据及生成器仍保留。

新版活动美术在 `Art/PortStoryV2` 和 `Art/FullStoryV2`。整版新增图像使用内置image_gen生成，提示词与输出记录见 `../../story/full-art-prompts.json`。接驳车已用独立透明插画替代矩形拼装，圆形轮胎保持等比缩放，参考与提示词见 `../../story/shuttle-art-prompt.md`。中文字体为OFL许可的Noto Sans CJK SC，授权随字体保留。原美术与参考资料继续留档；参考视频画面不打包到游戏。

2026-10-28 已完成编辑器内导入验收：150 张全分辨率 Sprite、1 个 FBX 模型、16 个当前素材入口均可读取；Windows / WebGL 构建支持可用。资源预览场景已保存，最终控制台无错误或警告。

## 资源位置

| 目录 | 内容 |
| --- | --- |
| `Assets/GuanshanYouyan/Art/WebLibrary` | 原 H5 素材库，含场景背景、角色及图集；网页格式已生成 PNG 副本 |
| `Assets/GuanshanYouyan/Art/Characters/LinJian` | 34 帧人物图片：站姿、32 帧行走与立绘 |
| `Assets/GuanshanYouyan/Art/Characters/LinJianParts` | 身体、手臂、腿和立绘部件 |
| `Assets/GuanshanYouyan/Art/CharacterAtlases` | 制作时使用的人物原始图集 |
| `Assets/GuanshanYouyan/Art/ProductionReference` | 人物制作参考和参考模型纹理 |
| `Assets/GuanshanYouyan/Art/PaperCut` | 现有剪纸挂旗素材及修订版本 |
| `Assets/GuanshanYouyan/Models/Quaternius` | 参考 FBX 和 CC0 原授权文本 |
| `Assets/GuanshanYouyan/Audio/Music` | 两首背景音乐，第一首已裁剪为精确 2:00 |
| `Assets/GuanshanYouyan/Prefabs/BackgroundMusic.prefab` | 两路背景音乐同时循环的可复用预制体 |
| `Assets/GuanshanYouyan/Data` | 剧情、场景布局、骨骼数据、动画参数与资源映射清单 |
| `Assets/GuanshanYouyan/Documentation` | 原项目策划、素材来源和生成记录 |
| `SourceArchive~` | 原始网页图片、Blender 工程和数据脚本，保留原相对目录 |

原资源快照共153项资源（150张图片、1个FBX、2首背景音乐）。包括制作参考、历史图集和缩略图，原预览的16个美术入口见 `Data/resource-manifest.json` 的 `activeEntries`。新版另有三张第一幕美术、八张整版美术与中文字体。

PNG/JPG 按字节复制；67 张 WebP/AVIF 转换后已校验解码 RGBA 像素相同；SVG 另行生成 PNG。资源快照按导入准备时刻保留，不会自动同步随后发生的 H5 改动。

## 编辑器入口

- `关山有眼 > 资源库`：按名称或分类搜索素材并定位到 Project 面板。
- `关山有眼 > 校验资源库`：检查 Sprite、FBX 及图片导入分辨率。
- `Assets/GuanshanYouyan/Scenes/ResourcePreview.unity`：已建立的资源预览场景，不包含游戏逻辑。

图片以 Bilinear、无 Mipmap、无纹理压缩、最大 8192 尺寸导入，保持现有插画清晰度。人物帧的脚底轴心与原图集基线对应。

## 背景音乐

ResourcePreview 场景已添加“背景音乐 BGM”：Underwater 的开头 120 秒（65% 音量）与 Mystery Mysterious Mystical 原曲（55% 音量）同时循环。点击 Unity 的 Play 即可试听，选中两个子对象可分别调节音量。完整 MP3 原件保存在 `SourceArchive~/Audio/Pixabay`；来源和许可记录见 `Assets/GuanshanYouyan/Documentation/背景音乐来源.md`。

## 验收记录

- `ImportReports/full-story-regression.json`：整版16个逻辑用例、4004次断言，包含自由顺序、四个退回段落、旧试玩导入、因果重放与备份恢复。
- `ImportReports/full-playtest-desktop.json`、`full-playtest-portrait.json`：Editor实际帧、移动、UI射线、虚拟触控完整流程；分别覆盖不走生态线和完整生态线。
- `ImportReports/full-artifact-validation.json`：311个原归档文件、147个对应现有H5文件哈希，整版剧情镜像与新增美术。
- `ImportReports/full-build-*.json`：各平台构建实际结果。独立程序和浏览器运行另见整版验收文档。

- `ImportReports/snapshot-validation.json`：307 个暂存素材与源文件的 SHA-256 校验结果。
- `ImportReports/unity-import-validation.json`：编辑器实际导入校验。
- `ImportReports/preview-validation.json`：场景保存、当前素材入口、URP/HDR、正交相机、后处理和构建目标的读回结果。
- `ImportReports/console-status.json`：最终编译及控制台状态。
- `ImportReports/resource-preview.png`：Unity Game View 实际渲染截图。
- `ImportReports/music-import-validation.json`：音乐时长、采样率和循环配置。
- `ImportReports/music-playback-validation.json`：两路真实输出、帧推进及循环回绕测试。

本工程包含Tilemap与Rule Tile扩展包。新版按08号文档使用横向插画场景探索，尚未制作可自动拼接的规则地形。`ResourcePreview.unity` 继续作为原资源预览。

原项目 `../../h5` 及素材原件不受此工程影响。
