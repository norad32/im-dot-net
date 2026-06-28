# ImDotNet

A C# GUI application template that uses **[ImGui](https://github.com/ocornut/imgui)** for the interface, with **[Silk.NET](https://github.com/dotnet/Silk.NET)** powering the underlying windowing, input, and OpenGL integration.

It’s set up for Linux, Windows, and CI via GitHub Actions.

---

## System Prerequisites

### Linux (Arch)

Install graphics drivers and common X11/OpenGL runtime libraries:

```bash
sudo pacman -S --needed base-devel dotnet-sdk glfw
```

### Windows

- **.NET 10.0 SDK** (64-bit) from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0)
- A graphics driver that supports OpenGL 3.0+

---

## Quickstart

### Manual Build

```bash
dotnet build -c Release
dotnet run
```

---

## Credits

This project is made possible by these open-source libraries:

| Library | Purpose | License |
|---------|---------|---------|
| **[ImGui](https://github.com/ocornut/imgui)** | Immediate-mode GUI library | [MIT](https://github.com/ocornut/imgui/blob/master/LICENSE.txt) |
| **[ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)** | .NET bindings for ImGui | [MIT](https://github.com/ImGuiNET/ImGui.NET/blob/master/LICENSE) |
| **[Silk.NET](https://github.com/dotnet/Silk.NET)** | Windowing, OpenGL, Input, Maths | [MIT](https://github.com/dotnet/Silk.NET/blob/main/LICENSE)</a> |
| **[Serilog](https://github.com/serilog/serilog)** | Structured logging | [Apache 2.0](https://github.com/serilog/serilog/blob/dev/LICENSE) |
| **[Spectre.Console](https://github.com/spectresystems/spectre.console)** | Rich console output | [MIT](https://github.com/spectresystems/spectre.console/blob/main/LICENSE) |
| **[System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json)** | State serialization (built-in) | [.NET Library](https://github.com/dotnet/runtime/blob/main/LICENSE) |

---

## License

This project is licensed under the **MIT License** — see [LICENSE](LICENSE) for details.

---

## Author

[norad32](https://github.com/norad32)