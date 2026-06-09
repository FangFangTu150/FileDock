# FileDock 代码审查与后续完善建议

审查日期：2026-06-09

## 审查结论

项目整体结构清晰，需求文档中的核心功能已经基本实现。Release 构建和单文件自包含发布均可通过，没有发现编译阻断问题。

本次审查发现少量需要优先处理的稳定性和兼容性风险，主要集中在窗口位置恢复、文件操作命令异常兜底、列表更新性能，以及 DPI 配置警告。以下问题均建议按定位范围定点修复，不建议顺手重构无关 UI 样式或业务结构。

## 验证结果

- `dotnet build .\FileDock.csproj -c Release`：通过，0 错误，2 个 WFAC010 DPI 警告。
- `dotnet publish .\FileDock.csproj -p:PublishProfile=Properties\PublishProfiles\win-x64-single-file.pubxml`：通过，输出到 `bin\Release\net8.0-windows\win-x64\publish\`。
- `dotnet test .\FileDock.csproj -c Release --no-build`：通过，但当前项目未见独立测试用例输出。

## 发现的问题

### P1 窗口恢复缺少屏幕可见区域校正

定位：

- `Views/DockWindow.xaml.cs:54-63`
- `Views/DockWindow.xaml.cs:204-247`
- `Views/DockWindow.xaml.cs:249-260`

问题：

窗口启动时直接恢复 `DockBounds.Left/Top/Width/Height`，之后只在距离边缘小于 20 DIP 时吸附。若用户上次把 Dock 放在副屏，之后拔掉副屏、远程桌面分辨率变化、系统缩放变化，窗口可能恢复到当前所有屏幕之外。此时托盘点击只执行 `ShowFromTray()`，不会重新拉回可见区域，用户可能看不到主窗口。

建议约束：

- 只在恢复窗口、托盘显示窗口、屏幕工作区变化相关路径加校正逻辑。
- 不改现有拖拽、缩放、吸附视觉样式。
- 校正逻辑应基于当前所有 `Forms.Screen.AllScreens` 工作区，至少保证窗口矩形与某个屏幕工作区有交集；完全不可见时放回主屏或最近屏幕边缘。

### P1 文件打开/定位命令缺少异常兜底

定位：

- `ViewModels/MainViewModel.cs:85-96`
- `ViewModels/MainViewModel.cs:98-113`

问题：

列表项来自轮询结果，但用户点击时文件可能已经被删除、移动、无权限访问，或关联程序启动失败。当前 `Process.Start(...)` 没有 `File.Exists/Directory.Exists` 复查，也没有 `try/catch`。在 WPF UI 线程中触发 `Win32Exception`、`FileNotFoundException` 等异常时，存在直接中断应用的风险。

建议约束：

- 只包裹 `OpenFile` 和 `OpenLocation` 命令执行路径。
- 打开前复查路径存在性；失败时静默忽略或用轻量提示，不应让异常冒泡到 UI 线程。
- 不改变现有右键菜单、双击、Explorer 参数格式，除非复查发现参数转义有问题。

### P2 剪贴板复制缺少异常兜底

定位：

- `ViewModels/MainViewModel.cs:115-124`
- `Views/FileCard.xaml.cs:63-67`

问题：

`Clipboard.SetFileDropList` 在剪贴板被其他进程占用、远程会话剪贴板不可用、COM 初始化状态异常时可能抛出异常。当前 `CopyButton_Click` 会无条件播放复制成功动画，即使命令没有真正复制成功；异常时也可能导致 UI 线程错误。

建议约束：

- 只调整复制命令返回结果和按钮成功动画触发条件。
- 复制失败不应播放成功动画。
- 不扩展新的复杂通知系统，除非后续产品需要统一错误提示。

### P2 大目录下列表 diff 和图标加载可能造成 UI 卡顿

定位：

- `Services/FileWatcherService.cs:171-203`
- `Services/FileWatcherService.cs:206-215`
- `Services/ShellIconProvider.cs:23-37`
- `Services/ShellIconProvider.cs:39-70`

问题：

`ApplyDiff` 在 UI 线程执行，每个条目都通过 `_target.FirstOrDefault(...)` 和 `_target.IndexOf(...)` 查找，整体接近 O(n^2)。当监听目录包含大量文件时，1 秒轮询一旦触发大批量变化，UI 线程可能明显卡顿。

另外，新条目转换为 `FileEntry` 时同步获取 Shell 图标。虽然图标按扩展名缓存，但第一次遇到大量不同扩展名、快捷方式，或 Shell 扩展响应较慢时，仍会占用 UI 线程。

建议约束：

- 优先把 diff 查找改为路径到条目的字典，避免 O(n^2)。
- 保留现有 `ObservableCollection` 和 WPF 绑定结构，不做架构级替换。
- 图标加载可继续缓存，但应考虑默认图标先渲染、Shell 图标异步回填，避免阻塞首次列表刷新。

### P3 配置写入不是原子写

定位：

- `Services/SettingsService.cs:42-47`
- `Services/SettingsService.cs:24-39`

问题：

配置保存直接 `File.WriteAllText(SettingsPath, json)`。如果写入期间进程退出、磁盘异常或杀进程，`settings.json` 可能留下空文件或半截 JSON。加载失败时当前逻辑会吞掉异常并返回默认配置，结果是窗口位置、监听路径和快照全部丢失。

建议约束：

- 只修改 `SettingsService.Save/Load`。
- 使用临时文件写入后原子替换，或保留 `.bak`。
- 加载失败时优先尝试备份文件，再退回默认配置。

### P3 托盘左键行为与需求不一致

定位：

- `App.xaml.cs:109`
- `App.xaml.cs:148-151`
- `App.xaml.cs:164-172`

问题：

需求文档要求托盘左键单击“显示/隐藏 DockWindow”。当前 `ToggleDock()` 实际只调用 `ShowDockFromTray()`，窗口显示时再次左键不会隐藏。

建议约束：

- 只调整 `ToggleDock()` 判断 `IsVisible` 后 `Hide()` 或 `ShowFromTray()`。
- 不改变右键菜单“显示 Dock / 设置 / 退出”的行为。

### P3 .NET 8 Windows Forms DPI 配置警告

定位：

- `FileDock.csproj:8-10`
- `app.manifest:5-8`

问题：

构建和发布均出现 `WFAC010`：建议从 manifest 删除高 DPI 设置，并通过 `Application.SetHighDpiMode` API 或 `ApplicationHighDpiMode` 项目属性配置。当前项目启用了 `UseWindowsForms`，且使用 `FolderBrowserDialog` 和 `Forms.Screen`，所以该警告来自 Windows Forms 兼容性分析。

建议约束：

- 若现有 DPI 行为实测正常，可短期保留并记录警告。
- 若要消除警告，应小范围调整 DPI 配置方式，不改 DockWindow 的 DIP 坐标和多屏换算逻辑。

## 后续完善优先级

1. 先修 P1：窗口可见区域校正、文件打开/定位异常兜底。
2. 再修 P2：剪贴板异常处理、列表 diff 性能和图标加载阻塞。
3. 最后处理 P3：配置原子写、托盘左键切换、DPI 警告治理。

## 回归验证建议

- 启动后修改 `%AppData%\FileDock\settings.json`，把 `dockBounds.left/top` 改到屏幕外，确认启动或托盘显示能回到可见区域。
- 监听目录中创建文件后立即删除，再双击旧卡片或右键打开位置，确认应用不崩溃。
- 剪贴板被其他程序占用时点击复制，确认不会崩溃且不会显示成功状态。
- 用包含 1000 个以上文件的目录测试轮询和首次加载，观察 DockWindow 拖动、滚动、按钮响应是否卡顿。
- 发布后运行单文件 exe，确认开机自启写入的是发布 exe 路径。
