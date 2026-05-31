# 建筑 PLINE 面积统计工具（AutoCAD 2022）

这是一个 AutoCAD 2022 .NET 插件，用来统计建筑轮廓 PLINE 的占地面积、按层数计算总面积，并在图中生成可编辑表格。

## 功能

- 选择已有闭合 `PLINE` 生成面积统计表。
- 新画闭合 `PLINE` 后，可自动加入当前统计表。
- 表格内可手动填写或修改“层数”。
- 运行更新命令后，自动重算每栋建筑的占地面积、总面积和合计。
- 支持图纸单位为毫米或米，输出统一为平方米。

## 编译

项目默认按 AutoCAD 2022 安装路径引用 DLL：

```text
C:\Program Files\Autodesk\AutoCAD 2022
```

如果你的 AutoCAD 安装在其他位置，可以在 Visual Studio 里修改 `BuildingAreaTool.csproj` 的 `AutoCADInstallDir`，或用 MSBuild 传参：

```powershell
msbuild .\BuildingAreaTool.csproj /p:Configuration=Release /p:AutoCADInstallDir="D:\Autodesk\AutoCAD 2022"
```

编译成功后 DLL 通常在：

```text
bin\Release\BuildingAreaTool.dll
```

## 加载

在 AutoCAD 2022 命令行输入：

```text
NETLOAD
```

选择编译出的 `BuildingAreaTool.dll`。

如果 AutoCAD 提示“无法加载程序集”，通常是 `SECURELOAD` 安全加载拦截。建议把插件目录加入可信路径：

```text
D:\CADPlugins\BuildingAreaTool\
```

本机已复制一份可直接加载的 DLL 到：

```text
D:\CADPlugins\BuildingAreaTool\BuildingAreaTool.dll
```

也已安装 AutoCAD 标准自动加载包到：

```text
C:\Users\sdadi\AppData\Roaming\Autodesk\ApplicationPlugins\BuildingAreaTool.bundle
```

## 命令

### `JZMJTABLE`

创建建筑面积统计表。

流程：

1. 选择图纸单位：`MM` 表示图纸按毫米绘制，`M` 表示按米绘制。默认 `MM`。
2. 选择已有闭合 `PLINE` 建筑轮廓；也可以直接回车创建空表。
3. 指定表格插入点。

创建后会自动开启新 PLINE 监听。

### `JZMJAUTO`

开启或关闭自动统计。

开启后，新画出的闭合 `PLINE` 会自动加入当前面积表。建议先运行 `JZMJTABLE` 创建表格，再开启自动统计。

### `JZMJADD`

手动把选中的闭合 `PLINE` 添加到当前面积表。

适合补录旧图里的建筑轮廓，或自动监听没有捕捉到的对象。

### `JZMJUPDATE`

更新当前面积表。

你在表格里修改“层数”后，运行这个命令即可刷新：

- 每行占地面积
- 每行总面积 = 占地面积 × 层数
- 合计占地面积
- 合计总面积

新版已增加自动联动刷新：编辑 PLINE 或修改表格“层数”后，命令结束时会自动刷新。若当前 AutoCAD 已经加载了旧 DLL，无需重启时可以额外 `NETLOAD`：

```text
D:\CADPlugins\BuildingAreaTool\BuildingAreaLiveUpdater.dll
```

加载后可用：

```text
JZMJLIVEON
JZMJREFRESH
```

其中 `JZMJLIVEON` 开启联动监听，`JZMJREFRESH` 立即刷新一次当前面积表。

### `JZMJHELP`

显示插件已加载状态和可用命令。

## 使用建议

- 建筑轮廓需要是闭合的轻量多段线，也就是 `LWPOLYLINE`。
- 如果某行显示“轮廓无效”，通常是对应 PLINE 被删除、不是闭合状态，或句柄被改动。
- 表格中的“图元句柄”用于绑定 PLINE，请不要手动修改。
- 第一版采用“修改层数后运行 `JZMJUPDATE`”的方式刷新结果；这样比强行监听表格编辑更稳定，也更符合 CAD 里人工校核的习惯。

## 后续可扩展

- 给每个 PLINE 自动编号并在图上标注编号。
- 支持多张统计表，例如按地块或专业分组统计。
- 支持读取 PLINE 所在图层，按图层分类汇总。
- 支持命令面板或 WinForms/WPF 窗口，做更像正式插件的交互。

## 地块指标表

已按 `指标表.xlsx` 的结构新增地块指标表工具。

图层约定：

- `yd`：地块范围闭合 PLINE
- `bh`：地块编号文字，位于对应 `yd` 范围内
- `jz`：建筑基底闭合 PLINE，位于对应 `yd` 范围内

命令：

```text
JZMJYDTABLE
JZMJYDUPDATE
JZMJYDHELP
```

`JZMJYDTABLE` 会扫描 `yd/bh/jz` 图层并生成类似 Excel 的指标表。现在会拆成两张 CAD 表格显示：

- 总指标表：对应 Excel `A2:E23`
- 分地块明细表：对应 Excel `A24:O...`

这样不会在一张宽表里留下大量空白格。表格内可以手动改“类型、层数、绿地率、地面车位”，刷新时会保留这些手填项。

生成时会依次询问三类图层名，默认值是：

```text
地块范围 PLINE 图层 <yd>
地块编号文字图层 <bh>
建筑基底 PLINE 图层 <jz>
```

如果图里实际编号在 `PUB_TEXT`，范围线在 `0` 或其他图层，可以在提示时输入对应图层名。非 `bh` 图层作为编号层时，会优先识别 `地块一/地块二` 或 `1#` 这类文本，避免把面积数字误当编号。

Excel 公式关系已转成 CAD 表格计算逻辑：

- `计容面积 = 基底面积 × 层数`
- `亩 = 用地面积 / 666.66`
- `密度 = 基底面积 / 用地面积`
- `绿地㎡ = 绿地率 × 用地面积`
- `非机动车㎡ = 计容面积 / 100 × 1.5`
- `机动车（个） = 计容面积 / 100 × 0.3`
- `容积率 = 计容面积 / 用地面积`
- `室内 = 机动车（个） - 地面`
