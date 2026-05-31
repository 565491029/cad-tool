独狼建筑指标统计控制台

安装：
1. 关闭 AutoCAD。
2. 双击“一键安装到当前用户.bat”。
3. 安装脚本会复制插件，并把插件目录加入 AutoCAD 受信任路径。
4. 重新打开 AutoCAD。
5. 输入命令：dulang1

卸载：
1. 关闭 AutoCAD。
2. 双击“卸载插件.bat”。

如果一键安装被安全软件拦截，也可以手动复制：
DulangLandIndexTool.bundle
复制到：
%APPDATA%\Autodesk\ApplicationPlugins\

兼容范围：
AutoCAD 2016-2024，64 位。
本发布版使用 AutoCAD 2016 .NET 引用编译，目标 .NET Framework 4.7.2。
如果 CAD2016 电脑加载失败，请先安装 .NET Framework 4.7.2 或 4.8。

如果输入 dulang1 提示未知命令，通常是 AutoCAD 的 SECURELOAD 安全加载限制导致。
请重新运行一键安装脚本，或在 AutoCAD 的“受信任的位置”中加入：
%APPDATA%\Autodesk\ApplicationPlugins\DulangLandIndexTool.bundle
%APPDATA%\Autodesk\ApplicationPlugins\DulangLandIndexTool.bundle\Contents\Win64

如果安装脚本提示 AutoCAD 正在运行，请先关闭所有 CAD/T20 窗口后重新安装。

不包含 AutoCAD 2025 版本。AutoCAD 2025 需要单独 .NET 8 版本。
