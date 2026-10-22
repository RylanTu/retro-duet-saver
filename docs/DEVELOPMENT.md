# 开发说明

## 构建

在 Windows x64 的 PowerShell 中运行根目录 build.ps1。脚本调用 %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe，以 RetroWindows.DuetApp 为入口编译 src/*.cs，并生成对应 .scr 和 ZIP。没有 NuGet 依赖。修改版本可使用 -Version 3.5.1 参数。

源码包含两个早期独立入口，合并版应始终使用 RetroWindows.DuetApp。不要直接使用最新语言语法，系统 .NET Framework 编译器不支持现代 C# 的全部功能。

## 启动参数

| 参数 | 功能 |
| --- | --- |
| 无 | 随机窗口预览 |
| /s | 多屏屏保 |
| /c 或 --settings | 设置 |
| /p <HWND> | Windows 屏保设置的小窗口预览 |
| --windows | Windows 时钟附近的固定顺序预览 |
| --assistant | Word 与回形针 |
| --new-scenes | 新增 Mac 场景 |
| --windows-review | 新增 Windows 场景 |
| --scifi | 科幻文本场景 |
| --route <file> | 输出固定场景列表与时间 |
| --render <file.png> <time> [--warm] | 离线静帧 |
| --frames <directory> <from> <to> [fps] | 离线 PNG 序列 |

离线时间使用固定路线的源时间；普通随机播放不按该路线依次展示。动态演出需要状态预热，静帧可用 --warm。

## 文件分工

- DuetApp.cs：入口、公共镜头、随机调度、过渡、合成与帧率调度。
- MultiScreenSession.cs：多屏窗口、共享渲染与显示配置变化。
- MacApp.cs / MacFluidApp.cs：Mac 设置、场景及演出。
- WinApp.cs / WinFluidApp.cs：Windows 设置、场景及演出。
- ReferenceScenes.cs / WindowsScenes.cs / ReferenceContent.cs：静态场景绘制与缓存。
- MacReferenceFonts.cs / WinWindowsPixels.cs：像素字形。

## 发布

docs/examples/build.yml 提供可选构建模板。要启用，将它复制到 .github/workflows/build.yml，再使用拥有 workflow 权限的登录方式提交。启用后，推送 v 开头标签或手动触发可构建 Windows 包；产物先保存在 Actions artifacts，不自动公开 Release。当前仓库未启用该工作流。维护者可用 GitHub Release 上传 dist/ 下的 ZIP，并附上 SHA-256。

维护场景时应依据真实经典应用的控件和窗口结构，避免仅替换标题的混合布局。添加静态内容优先复用缓存，不要在渲染回调内逐帧重新创建大图。
