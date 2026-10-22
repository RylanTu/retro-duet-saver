# Windows 场景依据与设计约束

## 约束
Windows 没有对应内容时，使用真实存在的 Windows 程序替代；不编造程序、面板或控件结构，也不把 Mac 布局仅换标题充作 Windows 窗口。
统一黑色像素材质可以保留；窗口、菜单、控件类型及所属选项卡应依据实际 Windows 95/98 或 Office 97 界面。签名可以作为真实程序中的文档或注册信息内容，不新增虚构功能。

## v3.4 替换
| 场景ID | 早期布局问题 | 替换后 |
|---|---|---|
| Windows 8 | 简化WordPad菜单/文档字体不对应 | WordPad工具栏和标尺，Fixedsys文档 |
| Windows 10 | 拼出来的欢迎窗口 | About Windows对话框 |
| Windows 14 | Paint中混用Mac工具/图案板 | 复用Windows Paint工具、画布、滚动条和单色调色板 |
| Windows 15 | Mac式填充样式板 | Windows标准Font对话框 |
| Windows 16 | 编写的系统介绍页 | MS-DOS Prompt命令窗口 |
| Windows 18 | 自造系统符号墙 | Character Map字符映射表 |
| Windows 20 | 自造图案卡片排列 | Display Properties背景页的Pattern与Wallpaper列表 |
| Windows 21 | 按钮页混入Motion指针速度设置 | Mouse Properties的Buttons页 |

## 界面来源
- 微软官方 Font Dialog Box 文档，说明字体、样式、字号与效果的真实控件结构：
  https://learn.microsoft.com/en-us/windows/win32/dlgbox/font-dialog-box
- Siemens Windows95/NT原始操作手册第23页，Character Map字体列表、字符网格、Characters to Copy及Close / Select / Copy：
  https://cache.industry.siemens.com/dl/files/717/19026717/att_45980/v1/tx1_76.pdf
- 微软 Windows95 Reviewers Guide 第3章的存档，真实系统界面与属性页结构：
  https://www.dfpug.de/loseblattsammlung/migration/whitepapers/Win95UI.htm
- Windows95原始截图索引，用于核对程序与对话框名称：
  https://guidebookgallery.org/screenshots/win95/

以上是程序界面结构依据。本项目仍是代码重绘、黑色单色像素化的屏保，不是直接运行原版Windows程序；未声称逐像素还原截图。没有依据的控件组合不得再添加。
