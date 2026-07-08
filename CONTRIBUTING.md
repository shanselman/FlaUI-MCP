# Contributing to FlaUI-MCP

Thank you for your interest in contributing to FlaUI-MCP!

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR-USERNAME/FlaUI-MCP.git`
3. Create a branch: `git checkout -b my-feature`
4. Make your changes
5. Test your changes
6. Push and create a Pull Request

## Development Setup

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Building

```powershell
cd src/FlaUI.Mcp
dotnet build
```

The same build can be run from GitHub Actions with the **Build** workflow's
manual dispatch button. Pull requests and pushes to `main` also run the build.

### Releasing

Releases are created by the **Release** workflow. Maintainers can either push a
semantic version tag such as `v0.1.1`, or run the workflow manually and provide
the tag name. Manual releases default to draft so the generated x64 and ARM64
ZIP files can be reviewed before publishing.

### Testing

```powershell
# Run the MCP server
dotnet run --project src/FlaUI.Mcp

# In another terminal, test with a JSON-RPC request
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}' | dotnet run --project src/FlaUI.Mcp
```

## Code Style

- Use C# conventions (PascalCase for public members, camelCase for private)
- Add XML documentation for public APIs
- Keep methods focused and small
- Prefer async/await for I/O operations

## Pull Request Guidelines

1. **One feature per PR** - Keep PRs focused
2. **Update documentation** - If you add a feature, document it
3. **Add tests** - When applicable
4. **Update CHANGELOG.md** - Add your changes under "Unreleased"

## Reporting Issues

When reporting issues, please include:
- Windows version
- .NET version (`dotnet --version`)
- Steps to reproduce
- Expected vs actual behavior
- Any error messages

## Feature Requests

Feature requests are welcome! Please describe:
- The problem you're trying to solve
- Your proposed solution
- Any alternatives you've considered

## Questions?

Open an issue with the "question" label.
