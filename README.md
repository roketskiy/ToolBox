# ToolBox

<p align="center">
  <img src="asset/工具箱%20(1).png" alt="ToolBox Logo" width="96"/>
</p>

> 带简介的本地程序目录 —— 把散落在硬盘各处的绿色小工具统一收进一个卡片式启动器。

ToolBox 是一个面向 Windows 的轻量程序目录应用。它**只负责记录、搜索和启动**你已有的本地程序，不重新实现工具功能，也不管安装与升级。适合管理图片批量压缩、字幕提取这类体积小、用得少、时间久了容易忘记位置和用途的工具。

## 功能特性

- **一键添加**：选择 `.exe` 后自动补全名称（ProductName → FileDescription → 文件名）、图标和工作目录，通常只需再填一句简介即可保存
- **卡片式界面**：图标 + 名称 + 最多两行简介，悬停显示更多操作
- **即时搜索**：按名称或简介不区分大小写过滤，输入即搜
- **单击启动**：使用保存的工作目录和启动参数启动程序（Ctrl+N 快速添加）
- **路径失效处理**：程序被移动/删除时不会崩溃，提示并支持重新定位
- **安全移除**：删除目录记录时**绝不会删除磁盘上的程序文件**
- **可靠存储**：数据保存到本地 JSON，写入原子替换并自动保留有效备份；数据损坏时自动从备份恢复，不覆盖原文件
- **轻量无依赖**：运行时无任何第三方库，纯 WPF + .NET 标准能力

## 环境要求

- Windows 10 / 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（运行）；.NET 8 SDK（构建）

## 构建与运行

```powershell
# 还原并构建
dotnet build

# 直接运行
dotnet run

# 发布（依赖桌面运行时，体积小）
dotnet publish -c Release
```

## 测试

项目内置一个无框架的轻量测试程序，覆盖数据往返、损坏恢复、名称补全与搜索等关键逻辑：

```powershell
dotnet run --project tests\ToolBox.Tests
```

输出 `ALL PASS` 即全部通过。

## 数据存储

工具记录保存在：

```text
%LOCALAPPDATA%\ToolBox\tools.json
```

同目录下保留 `tools.json.bak` 作为最近一次有效备份。每条记录包含：

| 字段 | 说明 |
| --- | --- |
| `name` | 工具显示名称 |
| `description` | 用户填写的用途简介 |
| `executablePath` | EXE 绝对路径 |
| `workingDirectory` | 启动时的工作目录（可选） |
| `arguments` | 启动参数（可选） |
| `iconPath` | 自定义图标路径（可选，为空则从 EXE 提取） |

图标不写入 JSON，自动图标在运行时从 EXE 提取并缓存。

## 项目结构

```text
ToolBox/
├── App.xaml / MainWindow.xaml     # 应用入口与主界面
├── EditWindow.xaml                # 添加 / 编辑窗口
├── MissingPathWindow.xaml         # 路径失效提示窗口
├── Models.cs                      # 数据模型与核心逻辑（名称补全、搜索、路径判断）
├── Storage.cs                     # JSON 持久化与备份恢复
├── IconProvider.cs                # EXE 图标提取
├── asset/                         # 应用图标与 Logo
├── tests/ToolBox.Tests/           # 轻量自动测试
└── docs/superpowers/specs/        # 设计规格（仅本地保留，不入库）
```

## 技术栈

- C# / WPF（.NET 8，`net8.0-windows`）
- JSON 持久化（`System.Text.Json`）
- 零第三方运行时依赖

## 许可

本项目暂未指定开源许可证，保留所有权利。
