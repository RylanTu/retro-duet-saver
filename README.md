# Retro Duet Saver

**让 Macintosh 与 Windows 95/98 的经典界面，在同一块像素画布里缓慢游走。**

运行于 Windows 11 的复古动态屏保。以黑色像素、缓慢变换的底色、柔和的立体边缘与自动鼠标演出，重新呈现两个桌面时代。

![Macintosh 编辑器](docs/images/macintosh.png)
![Windows 经典界面](docs/images/windows.png)

## 下载与运行

前往 [Releases](https://github.com/RylanTu/retro-duet-saver/releases) 下载最新 ZIP，解压后运行 **RetroDuetSaver.exe**。也可双击 **Preview.cmd**。

- **F11 / Enter**：切换全屏；**Esc**：退出。
- **S**：打开设置；**空格**：切换配色。
- **A**：开关自动鼠标演出；**P**：暂停 / 继续；**F2**：显示帧率。
- 双击 **Screensaver.cmd** 进入多屏屏保；屏保模式下移动鼠标或按键会退出。

安装到 Windows 屏保列表的方法见 [安装与使用](docs/USAGE.md)。程序是屏幕保护程序；账户锁定由 Windows 的“在恢复时显示登录屏幕”负责。

## 你会看到什么

- **38 个场景条目**，涵盖 Macintosh 与 Windows 经典应用、控制面板、时钟、错误对话框和文档。
- 首个场景一定是时钟，系统随机；后续每次选场景有 **35%** 概率选择时钟。时钟拥有更长展示时间。
- 场景内持续漫游，场景间采用多种消散、留白和浮现过渡。
- 自动鼠标拖动滑块、拖拽窗口、点击、绘图及逐字输入。界面是代码绘制的动画场景，不是可使用的桌面或模拟器。
- Windows 场景参考真实的 Paint、WordPad、Word / Clippy、Minesweeper、Calculator、MS-DOS Prompt、Character Map 及属性对话框。
- 两个系统的签名：**By RylanTu / Passion for a better world**。
- 编辑器中藏着 DON'T PANIC、42、DO NOT ANSWER，以及 Across the Great Wall, we can reach every corner in the world.

完整目录见 [场景一览](docs/SCENES.md)。

## 多屏与性能

多屏模式共享一套场景、随机调度和动画状态，通过共享缓存映射到各个屏幕；各屏同步播放，按比例居中裁切。支持显示器配置变更后重建窗口。

可设置 60 / 120 / 144 FPS 目标上限；实际帧率取决于显示器、GPU、分辨率与系统合成。静态像素材质缓存复用，只有时钟、输入和演出状态按需更新。**尚未完成各类多屏硬件的系统性性能测试**。详见 [渲染与性能](docs/ARCHITECTURE.md)。

## 从源码构建

Windows x64，安装 Windows 自带的 .NET Framework 4.x / WPF 环境。构建脚本使用系统 csc.exe，不需要 NuGet。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

输出位于 dist/：EXE、SCR 及便携 ZIP。详见 [开发说明](docs/DEVELOPMENT.md)。

## 项目结构

```text
src/                  C# / WPF 源码
scripts/              预览、设置与多屏屏保启动器
docs/                 安装、开发、场景依据与性能说明
docs/images/          实际程序画面
build.ps1             本机构建与打包
docs/examples/        可选的 GitHub Actions 构建模板
```

## 说明与致谢

灵感来自 macOS Sequoia 的 Macintosh 屏保，以及 Macintosh、Windows 95/98 与 Office 的经典界面。本项目为独立制作的风格化重绘，不隶属于 Apple 或 Microsoft，也不承诺逐像素复刻。界面考据见 [Windows 场景依据](docs/SCENE-REFERENCES.md)。

宣传剪辑使用过的音乐与参考视频不在本仓库或发布包中分发。字体来源与品牌说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

By **RylanTu** · Passion for a better world
