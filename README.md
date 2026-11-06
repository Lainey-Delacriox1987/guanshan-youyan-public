# 关山有眼 · 雾港来信

一款以反走私为主题的 2D 叙事调查游戏。玩家在港口、仓库、小镇与调查中心调查线索、比对单证，并据此作出判断。Unity 版包含四幕剧情与双结局。

## 本地运行

- **Unity 版** · 用 Unity 6000.3.27f1 打开 `unity/GuanshanYouyan2D`。场景位于 `Assets/GuanshanYouyan/Scenes`。
- **H5 版** · 进入 `h5`，运行 `npm run dev`，再打开 `http://127.0.0.1:4189/`。从仓库根目录运行 `npm install` 安装测试和美术工具依赖，再在 `h5` 目录运行 `npm test` 检查逻辑，运行 `npm run build` 生成静态网页。

`unity-tools/` 收录工程构建与验证工具，`story/` 收录剧情数据。`mentor-service/` 是本地离线问答服务。

游戏中的剧情、人物、地点与货号为虚构。源码与第三方素材的使用范围见 [许可说明](LICENSE.md) 和 [第三方素材许可](THIRD-PARTY-NOTICES.md)。
