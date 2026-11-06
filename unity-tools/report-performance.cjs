const fs=require('node:fs'),path=require('node:path');
const root=path.resolve(__dirname,'..'),dir=path.join(root,'unity/GuanshanYouyan2D/ImportReports');
const read=name=>JSON.parse(fs.readFileSync(path.join(dir,name),'utf8'));
const before=read('performance-baseline-browser.json'),after=read('performance-optimized-browser.json');
const mb=n=>(n/1e6).toFixed(2),mib=n=>(n/1048576).toFixed(1);
const download=r=>r.state.resources.reduce((n,r)=>n+r.transferBytes,0);
const a=before.samples.find(x=>x.name==='walk-8s'),b=after.samples.find(x=>x.name==='walk-8s');
const build=read('full-build-web-release.json'),input=read('performance-browser-input-validation.json');
const result={before,after,build,input,runtime:read('performance-runtime-validation.json'),logic:{...read('full-story-regression.json'),results:undefined},metrics:{beforeTransferBytes:download(before),afterTransferBytes:download(after),downloadReduction:1-download(after)/download(before),beforeHeapBytes:before.state.heapBytes,afterHeapBytes:after.state.heapBytes,beforeWalkFrames:a.renderedFrames,afterWalkFrames:b.renderedFrames,beforeWalkP95:a.p95Ms,afterWalkP95:b.p95Ms}};
fs.writeFileSync(path.join(dir,'performance-comparison.json'),JSON.stringify(result,null,2));
const doc=`# 运行性能优化与验收

2026-10-30。针对现有 Unity 完整故事及本地 WebGL 版本，保留图片原件、剧情条件和存档格式。公网、旧 H5 入口与 Windows 包没有在本轮发布或替换。

## 实际结果

同一台 Windows 电脑、Chrome ${after.browser}、AMD Radeon 610M，CSS 视口1600×900、设备像素比2。每组行走采样8秒。这里是单机测量，不能推为所有手机或 Safari 的性能保证。下载来自本机服务器，不用本机毫秒耗时宣称真实网络加速倍数。

| 项目 | 优化前 | 优化后 |
| --- | ---: | ---: |
| 本轮主体网络传输 | ${mb(download(before))} MB | ${mb(download(after))} MB |
| WASM 线性内存 | ${mib(before.state.heapBytes)} MiB | ${mib(after.state.heapBytes)} MiB |
| 高分屏内部渲染 | ${before.state.canvas.width}×${before.state.canvas.height} | ${after.state.canvas.width}×${after.state.canvas.height} |
| 8秒行走渲染帧数 | ${a.renderedFrames} | ${b.renderedFrames} |
| 行走帧间隔中位数 | ${a.medianMs.toFixed(1)} ms | ${b.medianMs.toFixed(1)} ms |
| 行走帧间隔P95 | ${a.p95Ms.toFixed(1)} ms | ${b.p95Ms.toFixed(1)} ms |
| 行走超过50ms的帧 | ${a.over50ms} | ${b.over50ms} |

主包实际传输减少 ${(result.metrics.downloadReduction*100).toFixed(1)}%。该值包含组合版的纹理、静态字体、代码优化和加载后处理，不能把全部收益单独归因于某一项。文件传输量不等于磁盘ZIP大小，WASM线性内存也不是浏览器总内存或GPU显存。

## 代码与资源

- 房间/动作改为字典查询；每帧位置查询与最近目标查找去掉LINQ分配和排序。目标仅一次扫描，交互文字只在目标或状态变化时更新；射线结果列表和指针事件对象复用。
- 前景切片按房间缓存，重复切场景不再销毁并重建；自建Sprite在销毁时释放。移动图标和对话使用子Canvas减少对静态UI的重建牵连；音乐图标仅状态改变时重建网格。
- 行车插画每帧只绘制一次布局，车辆尺寸只在画布尺寸变化时更新；正常游戏的自动存档只在行走产生未保存位置时周期写入，剧情节点、暂停及页面离开仍保存，保留冲突检测和原有备份逻辑。
- 主要固定文案在编辑器预烘焙静态中文字形，发布运行不再临时从完整CJK源字体生成字形。来源许可保留；新增文案可通过FogWebOptimizer.BuildFontAndBind重新烘焙并按字符集合缓存。
- 全部场景依赖和Resources经过导入审核：关闭不需要的CPU可读图片副本和mipmap链；适合的纹理用WebGL BC1/BC3。非4像素块对齐且依赖原尺寸裁图的图片保留Unity安全回退，未强行拉伸图片；信纸、角色、道路原图字节不改。旧版未使用的手册图从Resources移到Art/UnusedUI，避免隐式打包。音乐保留双轨、立体声和循环，只适当降低压缩质量。
- 内部渲染最高约1920×1080，设备像素比上限1.25，保留CSS全屏尺寸和长宽比；横竖屏门禁不变。外层封面使用轻量预览；代码与数据按内容版本缓存，保留原服务器地址及浏览器存档。
- 发布构建使用High裁剪、IL2CPP OptimizeSize、DiskSizeLTO、引擎裁剪、Gzip原包与Brotli传输后处理；保留ExplicitlyThrown异常，以支持现有存档错误处理。没有为缩小文件关闭必要的异常捕获。

压缩格式取舍参照[Unity纹理内存与格式说明](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/textures/textures-getting-started/texture-compression-formats/texture-choose-format-by-platform)。不支持相应GPU格式的设备可能采用解压回退，因此本轮移动端显存收益未作实机承诺。

## 验证与复现

源码热点检查785项通过，包含8房间最近目标与旧排序结果一致、连续10000次热点查询0字节托管分配、所有剧情字形已烘焙、场景不依赖完整CJK源字体。剧情回归17个案例、4014次断言通过。高DPI/横竖屏/窗口尺寸预算及引擎resize通知7项测试通过；窗口尺寸改变时通过Module.setCanvasSize通知引擎。

真实Chrome原生鼠标/键盘检查${input.checks.length}项通过：仓库出口到青禾镇、44px音乐钮静音与恢复、手册屏蔽场景点击、收起后点击行走，以及保存位置/证据/房间重载恢复。所有浏览器测试均用隔离资料和专用存档夹具，未清空用户网站数据。旧Editor虚拟触摸漫游在本环境未形成press事件，本轮未将该脚本列为通过，也未用它替代浏览器原生输入证据；本轮没有声称完成全部四幕的原生浏览器通关或实体手机/Safari验收。

最终构建：${build.date}，错误${build.errors}、提示${build.warnings}，源码指纹 ${build.sourceSha256}。资源传输和当前源码指纹核对见letter-web-served-validation.json；组合改动的行车、开场和统一人物另由对应聊天验收。

可复用命令：

    node --test unity-tools/test-web-performance.cjs
    node unity-tools/profile-web.cjs optimized
    node unity-tools/verify-performance-input.cjs
    node tmp/verify-letter-web.cjs
    node unity-tools/report-performance.cjs

上述结果见ImportReports/performance-comparison.json、performance-runtime-validation.json、performance-browser-input-validation.json、performance-optimized-browser.json。原始修改前的源码、导入.meta和设置保存在output/performance-before-20261030，可供人工复核，未直接覆盖回滚。
`;
fs.writeFileSync(path.join(root,'docs/22-运行性能优化与验收.md'),doc);console.log(JSON.stringify(result.metrics,null,2));
