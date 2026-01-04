# CodeCop.CLI Specification

## Overview

| Property | Value |
|----------|-------|
| Project | CodeCop.CLI |
| Type | Console Application |
| Framework | net8.0 |
| Tool Name | `codecop` |
| Version | 0.3.0 |

## Description

CodeCop.CLI is a command-line interface for running CodeCop analyzers against C# solutions and projects. It enables:
- Standalone analysis without Visual Studio
- CI/CD integration with proper exit codes
- Multiple output formats (Console, JSON)
- Rule filtering and configuration

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CodeCop.CLI                              │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │   Commands   │  │  Formatters  │  │   Exit Code Handler  │  │
│  ├──────────────┤  ├──────────────┤  ├──────────────────────┤  │
│  │ AnalyzeCmd   │  │ ConsoleOut   │  │ 0 = Success          │  │
│  │ RulesCmd     │  │ JsonOutput   │  │ 1 = Errors Found     │  │
│  │ VersionCmd   │  │              │  │ 2 = Warnings Only    │  │
│  └──────────────┘  └──────────────┘  │ 3 = Analysis Failed  │  │
│                                       └──────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                       CodeCop.Core                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                   ICodeCopRunner                          │  │
│  │  - LoadSolutionAsync(path)                               │  │
│  │  - LoadProjectAsync(path)                                │  │
│  │  - AnalyzeAsync(analyzers)                               │  │
│  │  - GetDiagnosticsAsync()                                 │  │
│  └──────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                      CodeCop.Sharp                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              All Roslyn Analyzers (CCS0001-CCS0014)      │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
CodeCop.CLI/
├── CodeCop.CLI.csproj
├── Program.cs                    # Entry point
├── Commands/
│   ├── AnalyzeCommand.cs         # Main analyze command
│   ├── RulesCommand.cs           # List rules
│   └── VersionCommand.cs         # Show version
├── Formatters/
│   ├── IOutputFormatter.cs       # Formatter interface
│   ├── ConsoleFormatter.cs       # ANSI colored console
│   └── JsonFormatter.cs          # JSON output
├── Models/
│   ├── AnalysisOptions.cs        # Command options
│   ├── AnalysisResult.cs         # Analysis output
│   ├── DiagnosticInfo.cs         # Single diagnostic
│   ├── OutputFormat.cs           # Format enum
│   └── ExitCode.cs               # Exit code enum
└── Services/
    ├── AnalysisService.cs        # Analysis orchestration
    └── RuleMetadataService.cs    # Rule information
```

---

## CLI Commands

### 1. `codecop analyze`

Analyzes a solution or project for code issues.

```bash
# Basic usage
codecop analyze --target ./MySolution.sln

# With format options
codecop analyze --target ./MyProject.csproj --format Console
codecop analyze --target ./MySolution.sln --format Json --output report.json

# CI/CD mode
codecop analyze --target ./MySolution.sln --fail-on-error
codecop analyze --target ./MySolution.sln --fail-on-warning

# Filter by severity
codecop analyze --target ./MySolution.sln --min-severity Warning

# Filter by rule
codecop analyze --target ./MySolution.sln --include CCS0001,CCS0002
codecop analyze --target ./MySolution.sln --exclude CCS0010,CCS0011

# Verbose output
codecop analyze --target ./MySolution.sln --verbose
```

**Options:**

| Option | Short | Type | Required | Default | Description |
|--------|-------|------|----------|---------|-------------|
| `--target` | `-t` | string | Yes | - | Path to .sln or .csproj |
| `--format` | `-f` | enum | No | Console | Output format |
| `--output` | `-o` | string | No | stdout | Output file path |
| `--fail-on-error` | - | flag | No | false | Exit 1 if errors |
| `--fail-on-warning` | - | flag | No | false | Exit 2 if warnings |
| `--min-severity` | `-s` | enum | No | Info | Minimum severity |
| `--include` | `-i` | string | No | all | Rule IDs to include |
| `--exclude` | `-e` | string | No | none | Rule IDs to exclude |
| `--verbose` | `-v` | flag | No | false | Verbose output |

### 2. `codecop rules`

Lists available analysis rules.

```bash
# List all rules
codecop rules

# Filter by category
codecop rules --category Naming
codecop rules --category Style

# Show specific rule details
codecop rules --id CCS0001

# JSON output
codecop rules --format Json
```

**Options:**

| Option | Short | Type | Required | Default | Description |
|--------|-------|------|----------|---------|-------------|
| `--category` | `-c` | string | No | all | Filter by category |
| `--id` | - | string | No | - | Show specific rule |
| `--format` | `-f` | enum | No | Console | Output format |

### 3. `codecop version`

Shows version information.

```bash
codecop version
# Output: CodeCop.CLI v0.3.0
```

---

## Exit Codes

| Code | Constant | Description |
|------|----------|-------------|
| 0 | `Success` | Analysis completed, no issues (or no --fail-on flags) |
| 1 | `ErrorsFound` | Analysis completed, errors found (with --fail-on-error) |
| 2 | `WarningsFound` | Analysis completed, warnings found (with --fail-on-warning) |
| 3 | `AnalysisFailed` | Analysis failed (invalid target, exception, etc.) |

### Exit Code Decision Flow

```
┌──────────────────────────────────┐
│ Analysis Completed Successfully? │
└───────────────┬──────────────────┘
                │
        ┌───────▼───────┐
        │      NO       │──────────► Exit 3 (AnalysisFailed)
        └───────┬───────┘
                │ YES
                ▼
┌──────────────────────────────────┐
│ --fail-on-error AND errors > 0?  │
└───────────────┬──────────────────┘
                │
        ┌───────▼───────┐
        │      YES      │──────────► Exit 1 (ErrorsFound)
        └───────┬───────┘
                │ NO
                ▼
┌──────────────────────────────────┐
│ --fail-on-warning AND warnings>0?│
└───────────────┬──────────────────┘
                │
        ┌───────▼───────┐
        │      YES      │──────────► Exit 2 (WarningsFound)
        └───────┬───────┘
                │ NO
                ▼
            Exit 0 (Success)
```

---

## Output Formats

### Console Format (Default)

```
CodeCop Analysis Report
=======================
Target: ./MySolution.sln
Duration: 2.34s

ERRORS (2)
──────────
CCS0001  src/MyClass.cs(15,20)  Method 'getData' should be PascalCase
CCS0003  src/IMyClass.cs(5,18)  Interface 'myInterface' should start with 'I'

WARNINGS (3)
────────────
CCS0004  src/Service.cs(10,15)  Private field 'Count' should be camelCase
CCS0010  src/Handler.cs(25,9)   if statement should use braces
CCS0010  src/Handler.cs(32,9)   foreach statement should use braces

INFO (1)
────────
CCS0005  src/Config.cs(8,22)    Constant 'apiKey' should use UPPER_CASE or PascalCase

Summary: 2 errors, 3 warnings, 1 info
```

### JSON Format

```json
{
  "version": "0.3.0",
  "target": "./MySolution.sln",
  "timestamp": "2024-01-15T10:30:00Z",
  "duration": "00:00:02.340",
  "success": true,
  "summary": {
    "total": 6,
    "errors": 2,
    "warnings": 3,
    "info": 1
  },
  "diagnostics": [
    {
      "id": "CCS0001",
      "severity": "Error",
      "message": "Method 'getData' should be PascalCase",
      "file": "src/MyClass.cs",
      "line": 15,
      "column": 20,
      "endLine": 15,
      "endColumn": 27,
      "category": "Naming",
      "title": "Method should use PascalCase"
    }
  ]
}
```

---

## Implementation Details

### Program.cs

```csharp
using System.CommandLine;
using CodeCop.CLI.Commands;

namespace CodeCop.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("CodeCop - C# Code Analyzer")
        {
            Name = "codecop"
        };

        rootCommand.AddCommand(new AnalyzeCommand());
        rootCommand.AddCommand(new RulesCommand());
        rootCommand.AddCommand(new VersionCommand());

        return await rootCommand.InvokeAsync(args);
    }
}
```

### AnalyzeCommand.cs

```csharp
using System.CommandLine;
using CodeCop.CLI.Formatters;
using CodeCop.CLI.Models;
using CodeCop.CLI.Services;

namespace CodeCop.CLI.Commands;

public class AnalyzeCommand : Command
{
    public AnalyzeCommand() : base("analyze", "Analyze a solution or project for code issues")
    {
        // Define options
        var targetOption = new Option<string>(
            aliases: new[] { "--target", "-t" },
            description: "Path to .sln or .csproj file")
        { IsRequired = true };

        var formatOption = new Option<OutputFormat>(
            aliases: new[] { "--format", "-f" },
            getDefaultValue: () => OutputFormat.Console,
            description: "Output format (Console, Json)");

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path (default: stdout)");

        var failOnErrorOption = new Option<bool>(
            name: "--fail-on-error",
            getDefaultValue: () => false,
            description: "Exit with code 1 if errors found");

        var failOnWarningOption = new Option<bool>(
            name: "--fail-on-warning",
            getDefaultValue: () => false,
            description: "Exit with code 2 if warnings found");

        var minSeverityOption = new Option<string>(
            aliases: new[] { "--min-severity", "-s" },
            getDefaultValue: () => "Info",
            description: "Minimum severity to report (Info, Warning, Error)");

        var includeOption = new Option<string?>(
            aliases: new[] { "--include", "-i" },
            description: "Comma-separated rule IDs to include");

        var excludeOption = new Option<string?>(
            aliases: new[] { "--exclude", "-e" },
            description: "Comma-separated rule IDs to exclude");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            getDefaultValue: () => false,
            description: "Show verbose output");

        // Add options to command
        AddOption(targetOption);
        AddOption(formatOption);
        AddOption(outputOption);
        AddOption(failOnErrorOption);
        AddOption(failOnWarningOption);
        AddOption(minSeverityOption);
        AddOption(includeOption);
        AddOption(excludeOption);
        AddOption(verboseOption);

        // Set handler
        this.SetHandler(async (context) =>
        {
            var options = new AnalysisOptions
            {
                Target = context.ParseResult.GetValueForOption(targetOption)!,
                Format = context.ParseResult.GetValueForOption(formatOption),
                Output = context.ParseResult.GetValueForOption(outputOption),
                FailOnError = context.ParseResult.GetValueForOption(failOnErrorOption),
                FailOnWarning = context.ParseResult.GetValueForOption(failOnWarningOption),
                MinSeverity = context.ParseResult.GetValueForOption(minSeverityOption)!,
                IncludeRules = ParseRuleList(context.ParseResult.GetValueForOption(includeOption)),
                ExcludeRules = ParseRuleList(context.ParseResult.GetValueForOption(excludeOption)),
                Verbose = context.ParseResult.GetValueForOption(verboseOption)
            };

            try
            {
                var service = new AnalysisService();
                var result = await service.AnalyzeAsync(options, context.GetCancellationToken());

                var formatter = FormatterFactory.Create(options.Format);
                await formatter.WriteAsync(result, options.Output);

                context.ExitCode = (int)DetermineExitCode(result, options);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (options.Verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                context.ExitCode = (int)ExitCode.AnalysisFailed;
            }
        });
    }

    private static string[]? ParseRuleList(string? rules)
    {
        if (string.IsNullOrWhiteSpace(rules))
            return null;

        return rules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static ExitCode DetermineExitCode(AnalysisResult result, AnalysisOptions options)
    {
        if (!result.Success)
            return ExitCode.AnalysisFailed;

        if (options.FailOnError && result.ErrorCount > 0)
            return ExitCode.ErrorsFound;

        if (options.FailOnWarning && result.WarningCount > 0)
            return ExitCode.WarningsFound;

        return ExitCode.Success;
    }
}
```

### ConsoleFormatter.cs

```csharp
using System.Text;
using CodeCop.CLI.Models;

namespace CodeCop.CLI.Formatters;

public class ConsoleFormatter : IOutputFormatter
{
    // ANSI escape codes
    private const string Reset = "\x1b[0m";
    private const string Bold = "\x1b[1m";
    private const string Red = "\x1b[91m";
    private const string Yellow = "\x1b[93m";
    private const string Cyan = "\x1b[96m";
    private const string Gray = "\x1b[90m";

    public async Task WriteAsync(AnalysisResult result, string? outputPath)
    {
        var output = new StringBuilder();

        // Header
        output.AppendLine($"{Bold}CodeCop Analysis Report{Reset}");
        output.AppendLine("=======================");
        output.AppendLine($"Target: {result.Target}");
        output.AppendLine($"Duration: {result.Duration.TotalSeconds:F2}s");
        output.AppendLine();

        // Group diagnostics by severity
        var errors = result.Diagnostics.Where(d => d.Severity == "Error").ToList();
        var warnings = result.Diagnostics.Where(d => d.Severity == "Warning").ToList();
        var infos = result.Diagnostics.Where(d => d.Severity == "Info").ToList();

        // Errors
        if (errors.Any())
        {
            output.AppendLine($"{Red}{Bold}ERRORS ({errors.Count}){Reset}");
            output.AppendLine($"{Red}──────────{Reset}");
            foreach (var diag in errors)
            {
                output.AppendLine(FormatDiagnostic(diag, Red));
            }
            output.AppendLine();
        }

        // Warnings
        if (warnings.Any())
        {
            output.AppendLine($"{Yellow}{Bold}WARNINGS ({warnings.Count}){Reset}");
            output.AppendLine($"{Yellow}────────────{Reset}");
            foreach (var diag in warnings)
            {
                output.AppendLine(FormatDiagnostic(diag, Yellow));
            }
            output.AppendLine();
        }

        // Info
        if (infos.Any())
        {
            output.AppendLine($"{Cyan}{Bold}INFO ({infos.Count}){Reset}");
            output.AppendLine($"{Cyan}────────{Reset}");
            foreach (var diag in infos)
            {
                output.AppendLine(FormatDiagnostic(diag, Cyan));
            }
            output.AppendLine();
        }

        // Summary
        output.AppendLine($"Summary: {Red}{errors.Count} errors{Reset}, {Yellow}{warnings.Count} warnings{Reset}, {Cyan}{infos.Count} info{Reset}");

        // Output
        var text = output.ToString();
        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, text);
        }
        else
        {
            Console.Write(text);
        }
    }

    private string FormatDiagnostic(DiagnosticInfo diag, string color)
    {
        return $"{color}{diag.Id}  {Gray}{diag.File}({diag.Line},{diag.Column})  {Reset}{diag.Message}";
    }
}
```

### JsonFormatter.cs

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCop.CLI.Models;

namespace CodeCop.CLI.Formatters;

public class JsonFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task WriteAsync(AnalysisResult result, string? outputPath)
    {
        var json = JsonSerializer.Serialize(result, Options);

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllTextAsync(outputPath, json);
        }
        else
        {
            Console.WriteLine(json);
        }
    }
}
```

---

## Models

### AnalysisOptions.cs

```csharp
namespace CodeCop.CLI.Models;

public class AnalysisOptions
{
    public required string Target { get; init; }
    public OutputFormat Format { get; init; } = OutputFormat.Console;
    public string? Output { get; init; }
    public bool FailOnError { get; init; }
    public bool FailOnWarning { get; init; }
    public string MinSeverity { get; init; } = "Info";
    public string[]? IncludeRules { get; init; }
    public string[]? ExcludeRules { get; init; }
    public bool Verbose { get; init; }
}
```

### AnalysisResult.cs

```csharp
namespace CodeCop.CLI.Models;

public class AnalysisResult
{
    public string Version { get; init; } = "0.3.0";
    public required string Target { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public TimeSpan Duration { get; init; }
    public bool Success { get; init; }
    public AnalysisSummary Summary { get; init; } = new();
    public List<DiagnosticInfo> Diagnostics { get; init; } = new();

    public int ErrorCount => Summary.Errors;
    public int WarningCount => Summary.Warnings;
}

public class AnalysisSummary
{
    public int Total { get; set; }
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public int Info { get; set; }
}
```

### DiagnosticInfo.cs

```csharp
namespace CodeCop.CLI.Models;

public class DiagnosticInfo
{
    public required string Id { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public required string File { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public int EndLine { get; init; }
    public int EndColumn { get; init; }
    public string? Category { get; init; }
    public string? Title { get; init; }
}
```

### ExitCode.cs

```csharp
namespace CodeCop.CLI.Models;

public enum ExitCode
{
    Success = 0,
    ErrorsFound = 1,
    WarningsFound = 2,
    AnalysisFailed = 3
}
```

---

## Project File

### CodeCop.CLI.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Global Tool Configuration -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>codecop</ToolCommandName>
    <PackageOutputPath>./nupkg</PackageOutputPath>

    <!-- Package Info -->
    <PackageId>CodeCop.CLI</PackageId>
    <Version>0.3.0</Version>
    <Authors>CodeCop</Authors>
    <Description>Command-line interface for CodeCop C# code analyzer</Description>
    <PackageProjectUrl>https://github.com/your-repo/code-cop-sharp</PackageProjectUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageTags>roslyn;analyzer;csharp;cli;code-analysis</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CodeCop.Core\CodeCop.Core.csproj" />
  </ItemGroup>

</Project>
```

---

## Installation & Usage

### Install as Global Tool

```bash
# From NuGet (when published)
dotnet tool install -g CodeCop.CLI

# From local build
dotnet pack CodeCop.CLI
dotnet tool install -g --add-source ./nupkg CodeCop.CLI
```

### Uninstall

```bash
dotnet tool uninstall -g CodeCop.CLI
```

### CI/CD Integration

#### GitHub Actions

```yaml
- name: Install CodeCop
  run: dotnet tool install -g CodeCop.CLI

- name: Run Analysis
  run: codecop analyze --target ./MySolution.sln --fail-on-error --format Json --output codecop-report.json

- name: Upload Report
  uses: actions/upload-artifact@v3
  with:
    name: codecop-report
    path: codecop-report.json
```

#### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'install -g CodeCop.CLI'

- script: codecop analyze --target $(Build.SourcesDirectory)/MySolution.sln --fail-on-error
  displayName: 'Run CodeCop Analysis'
```

---

## Test Strategy

### Unit Tests

```
CodeCop.CLI.Tests/
├── Commands/
│   ├── AnalyzeCommandTests.cs
│   └── RulesCommandTests.cs
├── Formatters/
│   ├── ConsoleFormatterTests.cs
│   └── JsonFormatterTests.cs
└── Services/
    └── AnalysisServiceTests.cs
```

### Integration Tests

```csharp
public class CliIntegrationTests
{
    [Fact]
    public async Task Analyze_ValidSolution_ReturnsZeroExitCode()
    {
        var result = await RunCliAsync("analyze", "--target", "TestData/ValidSolution.sln");
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Analyze_WithErrors_FailOnError_ReturnsOneExitCode()
    {
        var result = await RunCliAsync("analyze", "--target", "TestData/WithErrors.sln", "--fail-on-error");
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Analyze_JsonFormat_ProducesValidJson()
    {
        var result = await RunCliAsync("analyze", "--target", "TestData/ValidSolution.sln", "--format", "Json");
        var json = JsonDocument.Parse(result.Output);
        Assert.NotNull(json.RootElement.GetProperty("diagnostics"));
    }

    [Fact]
    public async Task Rules_ListsAllRules()
    {
        var result = await RunCliAsync("rules");
        Assert.Contains("CCS0001", result.Output);
        Assert.Contains("CCS0010", result.Output);
    }
}
```

---

## Deliverable Checklist

### Project Setup
- [ ] Create `CodeCop.CLI.csproj`
- [ ] Add `System.CommandLine` dependency
- [ ] Add `CodeCop.Core` project reference
- [ ] Configure as global tool

### Commands
- [ ] Implement `Program.cs` entry point
- [ ] Implement `AnalyzeCommand.cs`
- [ ] Implement `RulesCommand.cs`
- [ ] Implement `VersionCommand.cs`

### Formatters
- [ ] Define `IOutputFormatter` interface
- [ ] Implement `ConsoleFormatter` with ANSI colors
- [ ] Implement `JsonFormatter`
- [ ] Create `FormatterFactory`

### Models
- [ ] Create `AnalysisOptions.cs`
- [ ] Create `AnalysisResult.cs`
- [ ] Create `DiagnosticInfo.cs`
- [ ] Create `OutputFormat.cs`
- [ ] Create `ExitCode.cs`

### Services
- [ ] Implement `AnalysisService.cs`
- [ ] Implement `RuleMetadataService.cs`

### Testing
- [ ] Write unit tests for formatters
- [ ] Write integration tests for CLI
- [ ] Test exit codes

### Documentation
- [ ] Update README with CLI usage
- [ ] Add examples to docs

### Verification
- [ ] CLI builds successfully
- [ ] Global tool installation works
- [ ] All commands function correctly
- [ ] JSON output is valid
- [ ] Exit codes work as expected
