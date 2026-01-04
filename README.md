# CodeCop.Sharp

A custom Roslyn-based code analyzer to enforce specific coding standards or identify common anti-patterns in C# projects. It integrates directly into Visual Studio (showing squiggles) and the `dotnet build` process, providing compile-time feedback to developers.

## Current Status

**v0.1.0** - 1 analyzer implemented (CCS0001 - Method PascalCase naming)

## Roadmap

See [ROADMAP_V1.md](roadmap/ROADMAP_V1.md) for the complete v1.0 implementation plan, including:
- 30 analyzers across 5 categories (naming, style, quality, best practices, security)
- CLI for CI/CD integration
- AvaloniaUI desktop GUI
- Visual Studio 2022 extension

## Features

### Implemented
- **CCS0001**: Method names must be PascalCase (with auto-fix)

### Planned (v1.0)
- Naming convention analyzers (classes, interfaces, fields, constants)
- Code style analyzers (braces, var usage, formatting)
- Code quality analyzers (unused code, complexity, method length)
- Best practices (async/await, LINQ, null handling)
- Basic security patterns (hardcoded secrets, weak crypto)

## Getting Started

### As a Project Reference
```bash
dotnet add package CodeCop.Sharp
```

### Building from Source
```bash
git clone https://github.com/southpawriter02/code-cop-sharp.git
cd code-cop-sharp
dotnet build
dotnet test
```

## Documentation

- [Roadmap to v1.0](roadmap/ROADMAP_V1.md)
- [GUI/UX Specification](specs/GUI_UX_ENHANCEMENT_PROPOSAL.md)
- [Feature Documentation](roadmap/)
