# 第三方素材许可与来源

本项目在自有原创内容之外使用了以下第三方素材。各自遵循其原始许可，许可原文随工程保留在对应目录下。

## 字体

| 字体 | 用途 | 许可 | 许可原文位置 |
| --- | --- | --- | --- |
| Noto Sans CJK SC | 正文、UI 与剧情文本 | SIL Open Font License 1.1 | `unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Fonts/OFL.txt` |
| 龙藏体 LongCang | 手写来信、信件文案 | SIL Open Font License 1.1 | `unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Fonts/LongCang/OFL.txt` |
| 结局海报衬线字体 | 结局海报标题 | SIL Open Font License 1.1 | `unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Fonts/Ending/OFL.txt` |

## 音乐

两首背景音乐均由用户自行从 Pixabay 下载后提供，遵循 Pixabay Content License。
完整来源、作者与原始页面记录在
`unity/GuanshanYouyan2D/Assets/GuanshanYouyan/Documentation/背景音乐来源.md`，
文件 SHA-256 见 `Assets/GuanshanYouyan/Data/bgm-library.json`。

| 曲目 | 作者 | 原始页面 |
| --- | --- | --- |
| Underwater - mysterious ambient music | HarumachiMusic | <https://pixabay.com/music/mystery-underwater-mysterious-ambient-music-297537/> |
| Mystery Mysterious Mystical | MondaMusic | <https://pixabay.com/music/modern-classical-mystery-mysterious-mystical-519335/> |

Pixabay 授权摘要：<https://pixabay.com/service/license-summary/>
（允许按条款免费使用与改编；**禁止将音乐本身作为独立内容出售或分发**。）

> 裁剪说明：第一首取开头 0–120 秒保存为 PCM WAV，第二首保留原长度，两首同时循环播放。
> 该说明是项目的来源记录，不构成平台签发的下载授权证书。

## 3D 参考模型

| 模型 | 作者 | 许可 | 位置 |
| --- | --- | --- | --- |
| Animated Human | Quaternius | CC0 1.0 Universal（公有领域奉献） | `h5/design/motion-reference/quaternius-human/Animated Human by @Quaternius/License.txt` |

该模型仅用作骨骼与步态参考，成品使用本项目自行生成的角色美术，未直接分发原始模型。

## Unity 与第三方包

工程依赖的 Unity 官方包见 `unity/GuanshanYouyan2D/Packages/manifest.json`
（含 2D Animation、2D Sprite、Input System、Universal RP、Test Framework、Timeline、uGUI 等）。
这些包遵循 Unity 各自的许可条款，不随本仓库源码分发，打开工程时由 Unity 按清单自动还原。

## 本地模型（可选组件）

`mentor-service/` 中的离线问答网关在单独启用时，使用 Qwen2.5-1.5B-Instruct GGUF（Q4_K_M）
与 llama.cpp 运行时，两者均在本地 `127.0.0.1` 运行，**不使用任何 API 密钥**，
也不属于本仓库分发的二进制（模型与运行时需自行下载）。

## 美术素材说明

场景、角色与 UI 美术由本项目使用 AI 生成后二次加工，提示词与生成记录随
`story/`、`docs/`、`h5/design/` 保留。研究阶段参考过 Pngtree、千图、千库、爱给、
itch.io、OpenGameArt、Pixabay、Pexels、纹藏等站点的构图与机制，
**未搬运未授权图像**，成品使用本项目自行生成的场景与原创程序矢量图标。
游戏内显著标注「AI 生成」并提供创作说明入口。
