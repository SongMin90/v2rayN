# v2rayN

v2rayN 是一个基于 .NET 的 V2Ray GUI 客户端，支持 Windows、Linux 和 macOS (通过 Avalonia UI)。

## 功能特点

- **多协议支持**：支持 VMess, VLESS, Shadowsocks, Socks, Trojan, Hysteria2, Tuic, WireGuard, Goflyway 等多种协议。
- **订阅管理**：支持多种格式的订阅链接，支持自动更新。
- **路由规则**：内置强大的路由规则管理，支持自定义规则。
- **内核管理**：支持 Xray, Sing-box, Clash (Meta/Mihomo) 等多种内核。
- **自动维护**：(新增) 支持定时自动维护任务，包括自动更新订阅、测速并移除无效节点。
- **跨平台**：通过 `v2rayN.Desktop` 项目提供跨平台支持。

## 开发环境

- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code

## 构建与运行

### Windows

推荐使用 Visual Studio 打开 `v2rayN.sln` 进行编译和运行。

或者使用命令行：

```bash
dotnet build
dotnet run --project v2rayN.Desktop/v2rayN.Desktop.csproj
```

### Linux / macOS

```bash
dotnet build
dotnet run --project v2rayN.Desktop/v2rayN.Desktop.csproj
```

## 自动维护功能

本项目包含一个自动维护功能，可以定期执行以下操作：
1. 更新订阅（不通过代理）。
2. 对节点进行真连接测速。
3. 自动删除速度无效（<=0）的节点。
4. 对剩余节点进行延迟测试（Tcping）。

你可以在 **Subscription (订阅)** 菜单下找到 **运行自动维护(测试)** 来手动触发该功能。

## 许可证

本项目采用 [GPL-3.0](LICENSE) 许可证。
