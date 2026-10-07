# I'm DotNet

[![Tests & format](https://github.com/norad32/im-dot-net/actions/workflows/test.yaml/badge.svg?branch=main)](https://github.com/norad32/im-dot-net/actions/workflows/test.yaml)
[![Release build](https://github.com/norad32/im-dot-net/actions/workflows/release.yaml/badge.svg)](https://github.com/norad32/im-dot-net/actions/workflows/release.yaml)

A starter desktop GUI built with [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) and [Silk.NET](https://github.com/dotnet/Silk.NET). It includes a CLI, persistent GUI preferences, a GUI log panel, unit tests, and GitHub Actions checks. Requires the .NET 10 SDK.

## Run locally

Install the .NET 10 SDK and the graphics dependencies for your platform.

On Arch Linux:

```bash
sudo pacman -S --needed base-devel dotnet-sdk glfw
```

On Windows, install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and a graphics driver that supports OpenGL 3.0 or newer.

Build the solution and launch the GUI from the repository root:

```bash
dotnet build ImDotNet.sln -c Release
dotnet run --project src/ImDotNet.Cli/ImDotNet.Cli.csproj
```

## Develop and test

The root solution includes the Core, GUI, CLI, and unit test projects. Run the tests and build from the repository root:

```bash
dotnet test ImDotNet.sln -c Release
dotnet build ImDotNet.sln -c Release
```

## Release artifacts

Pushing a `v*` tag triggers a release workflow. It publishes self-contained Linux x64 and Windows x64 CLI bundles and attaches them to a GitHub release. To publish locally:

```bash
dotnet publish src/ImDotNet.Cli/ImDotNet.Cli.csproj -c Release
```

## Project layout

```text
.vscode/                      Workspace settings and extension recommendations
ImDotNet.sln                  Solution containing all projects
Directory.Build.props         Shared .NET build settings
src/ImDotNet.Core/            Application information and logging
src/ImDotNet.Gui/             GUI, panels, preferences, and assets
src/ImDotNet.Cli/             CLI entry point and commands
tests/ImDotNet.Tests/          Headless unit tests
```

## Credits

This project uses the following open-source libraries and tools:

| Library                                                                  | Purpose                             | License                                                                    |
| ------------------------------------------------------------------------ | ----------------------------------- | -------------------------------------------------------------------------- |
| **[Dear ImGui](https://github.com/ocornut/imgui)**                       | Immediate-mode GUI library          | [MIT](https://github.com/ocornut/imgui/blob/master/LICENSE.txt)            |
| **[ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)**                   | .NET bindings for Dear ImGui        | [MIT](https://github.com/ImGuiNET/ImGui.NET/blob/master/LICENSE)           |
| **[Silk.NET](https://github.com/dotnet/Silk.NET)**                       | Windowing, OpenGL, input, and maths | [MIT](https://github.com/dotnet/Silk.NET/blob/main/LICENSE)                |
| **[Serilog](https://github.com/serilog/serilog)**                        | Structured logging                  | [Apache 2.0](https://github.com/serilog/serilog/blob/dev/LICENSE)          |
| **[Spectre.Console](https://github.com/spectresystems/spectre.console)** | CLI parsing and rich console output | [MIT](https://github.com/spectresystems/spectre.console/blob/main/LICENSE) |
| **[.NET](https://dotnet.microsoft.com/)**                                | Runtime and standard libraries      | [MIT](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT)             |
| **[xUnit](https://xunit.net/)**                                          | Unit testing framework              | [Apache 2.0](https://github.com/xunit/xunit/blob/main/LICENSE)             |
| **[coverlet](https://github.com/coverlet-coverage/coverlet)**            | Test coverage collection            | [MIT](https://github.com/coverlet-coverage/coverlet/blob/master/LICENSE)   |

## License

This project is licensed under the **MIT License** - see [LICENSE](LICENSE) for details.

## Author

[norad32](https://github.com/norad32)
