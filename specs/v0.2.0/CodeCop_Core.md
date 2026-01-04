# CodeCop.Core - Analysis Runner Library

## Overview

| Property | Value |
|----------|-------|
| Project Name | CodeCop.Core |
| Target Framework | netstandard2.0 |
| Purpose | Shared analysis runner for CLI, GUI, and integrations |
| Dependencies | MSBuild.Locator, Workspaces.MSBuild, CodeCop.Sharp |

## Description

CodeCop.Core is the foundation library that abstracts the complexity of loading MSBuild projects/solutions and executing Roslyn analyzers. It provides a unified API for all consumers (CLI, GUI, Web Dashboard) to run code analysis.

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Consumers                                      │
├──────────────────┬──────────────────┬──────────────────┬────────────────┤
│   CodeCop.CLI    │   CodeCop.GUI    │ CodeCop.Dashboard│  3rd Party     │
│  (Console App)   │  (AvaloniaUI)    │   (Web API)      │  Integrations  │
└────────┬─────────┴────────┬─────────┴────────┬─────────┴────────┬───────┘
         │                  │                  │                  │
         └──────────────────┼──────────────────┼──────────────────┘
                            │                  │
                            ▼                  ▼
         ┌─────────────────────────────────────────────────────────┐
         │                     CodeCop.Core                        │
         │  ┌───────────────────────────────────────────────────┐  │
         │  │              ICodeCopRunner                       │  │
         │  │  - AnalyzeProjectAsync(path)                      │  │
         │  │  - AnalyzeSolutionAsync(path)                     │  │
         │  │  - AnalyzeFileAsync(path, compilation)            │  │
         │  └───────────────────────────────────────────────────┘  │
         │                          │                              │
         │  ┌───────────────────────▼───────────────────────────┐  │
         │  │              WorkspaceLoader                      │  │
         │  │  - MSBuildLocator integration                     │  │
         │  │  - Solution/Project loading                       │  │
         │  └───────────────────────────────────────────────────┘  │
         │                          │                              │
         │  ┌───────────────────────▼───────────────────────────┐  │
         │  │              AnalyzerRunner                       │  │
         │  │  - CompilationWithAnalyzers                       │  │
         │  │  - Diagnostic collection                          │  │
         │  └───────────────────────────────────────────────────┘  │
         └────────────────────────────┬────────────────────────────┘
                                      │
                                      ▼
         ┌─────────────────────────────────────────────────────────┐
         │                    CodeCop.Sharp                        │
         │           (Analyzer Library - Existing)                 │
         │  - CCS0001: MethodPascalCase                            │
         │  - CCS0002: ClassPascalCase                             │
         │  - CCS0003: InterfacePrefixI                            │
         │  - CCS0004: PrivateFieldCamelCase                       │
         │  - CCS0005: ConstantUpperCase                           │
         └─────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
CodeCop.Core/
├── CodeCop.Core.csproj
├── ICodeCopRunner.cs           # Main interface
├── CodeCopRunner.cs            # Default implementation
├── Workspace/
│   ├── WorkspaceLoader.cs      # MSBuild workspace loading
│   └── WorkspaceOptions.cs     # Configuration options
├── Analysis/
│   ├── AnalyzerRunner.cs       # Runs analyzers on compilation
│   └── AnalyzerRegistry.cs     # Discovers available analyzers
├── Models/
│   ├── AnalysisReport.cs       # Top-level report
│   ├── AnalyzedProject.cs      # Per-project results
│   ├── CopDiagnostic.cs        # Individual diagnostic
│   ├── SummaryStats.cs         # Aggregated statistics
│   └── AnalysisOptions.cs      # Configuration options
├── Configuration/
│   ├── RuleConfiguration.cs    # Rule enable/disable/severity
│   └── EditorConfigReader.cs   # .editorconfig parsing
└── Progress/
    ├── IAnalysisProgress.cs    # Progress reporting interface
    └── AnalysisPhase.cs        # Analysis phase enum
```

---

## Core Interfaces

### ICodeCopRunner

```csharp
namespace CodeCop.Core
{
    /// <summary>
    /// Main interface for running code analysis on projects and solutions.
    /// </summary>
    public interface ICodeCopRunner
    {
        /// <summary>
        /// Analyzes a single .csproj file.
        /// </summary>
        Task<AnalysisReport> AnalyzeProjectAsync(
            string projectPath,
            AnalysisOptions options = null,
            IAnalysisProgress progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes all projects in a .sln file.
        /// </summary>
        Task<AnalysisReport> AnalyzeSolutionAsync(
            string solutionPath,
            AnalysisOptions options = null,
            IAnalysisProgress progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of available analyzer rules.
        /// </summary>
        IReadOnlyList<RuleInfo> GetAvailableRules();
    }
}
```

### IAnalysisProgress

```csharp
namespace CodeCop.Core.Progress
{
    /// <summary>
    /// Interface for receiving analysis progress updates.
    /// </summary>
    public interface IAnalysisProgress
    {
        /// <summary>
        /// Called when analysis phase changes.
        /// </summary>
        void OnPhaseChanged(AnalysisPhase phase, string message);

        /// <summary>
        /// Called when a project starts being analyzed.
        /// </summary>
        void OnProjectStarted(string projectName);

        /// <summary>
        /// Called when a project finishes analysis.
        /// </summary>
        void OnProjectCompleted(string projectName, int diagnosticCount);

        /// <summary>
        /// Called to report overall progress (0.0 to 1.0).
        /// </summary>
        void OnProgressChanged(double progress);
    }

    public enum AnalysisPhase
    {
        Initializing,
        LocatingMSBuild,
        LoadingWorkspace,
        LoadingProject,
        Compiling,
        Analyzing,
        GeneratingReport,
        Complete
    }
}
```

---

## Data Models

### AnalysisReport

```csharp
namespace CodeCop.Core.Models
{
    /// <summary>
    /// Complete analysis report for a solution or project.
    /// </summary>
    public class AnalysisReport
    {
        /// <summary>
        /// Timestamp when analysis started.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Duration of the analysis.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Path to the analyzed solution or project.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// List of analyzed projects with their diagnostics.
        /// </summary>
        public List<AnalyzedProject> Projects { get; set; } = new List<AnalyzedProject>();

        /// <summary>
        /// Aggregated statistics across all projects.
        /// </summary>
        public SummaryStats Stats { get; set; } = new SummaryStats();

        /// <summary>
        /// Version of CodeCop.Core used for analysis.
        /// </summary>
        public string CodeCopVersion { get; set; }

        /// <summary>
        /// Whether the analysis completed successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if analysis failed.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
```

### AnalyzedProject

```csharp
namespace CodeCop.Core.Models
{
    /// <summary>
    /// Analysis results for a single project.
    /// </summary>
    public class AnalyzedProject
    {
        /// <summary>
        /// Project name (without path).
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Full path to the .csproj file.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Target framework(s) of the project.
        /// </summary>
        public List<string> TargetFrameworks { get; set; } = new List<string>();

        /// <summary>
        /// List of diagnostics found in this project.
        /// </summary>
        public List<CopDiagnostic> Diagnostics { get; set; } = new List<CopDiagnostic>();

        /// <summary>
        /// Number of source files in the project.
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Whether the project loaded and compiled successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if project failed to load/compile.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
```

### CopDiagnostic

```csharp
namespace CodeCop.Core.Models
{
    /// <summary>
    /// Represents a single diagnostic (code issue) found during analysis.
    /// </summary>
    public class CopDiagnostic
    {
        /// <summary>
        /// Rule ID (e.g., "CCS0001").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Human-readable message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Severity level.
        /// </summary>
        public DiagnosticSeverityLevel Severity { get; set; }

        /// <summary>
        /// Category (e.g., "Naming", "Style").
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Source file path.
        /// </summary>
        public string File { get; set; }

        /// <summary>
        /// Line number (1-based).
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number (1-based).
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// End line number (for multi-line spans).
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// End column number.
        /// </summary>
        public int EndColumnNumber { get; set; }

        /// <summary>
        /// Project containing this diagnostic.
        /// </summary>
        public string ProjectName { get; set; }

        /// <summary>
        /// Whether a code fix is available.
        /// </summary>
        public bool HasCodeFix { get; set; }
    }

    public enum DiagnosticSeverityLevel
    {
        Hidden = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
```

### SummaryStats

```csharp
namespace CodeCop.Core.Models
{
    /// <summary>
    /// Aggregated statistics across all analyzed projects.
    /// </summary>
    public class SummaryStats
    {
        public int TotalProjects { get; set; }
        public int SuccessfulProjects { get; set; }
        public int FailedProjects { get; set; }
        public int TotalFiles { get; set; }
        public int TotalDiagnostics { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }

        /// <summary>
        /// Diagnostics grouped by rule ID.
        /// </summary>
        public Dictionary<string, int> DiagnosticsByRule { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Diagnostics grouped by category.
        /// </summary>
        public Dictionary<string, int> DiagnosticsByCategory { get; set; } = new Dictionary<string, int>();
    }
}
```

### AnalysisOptions

```csharp
namespace CodeCop.Core.Models
{
    /// <summary>
    /// Configuration options for analysis.
    /// </summary>
    public class AnalysisOptions
    {
        /// <summary>
        /// Rules to exclude from analysis.
        /// </summary>
        public HashSet<string> ExcludedRules { get; set; } = new HashSet<string>();

        /// <summary>
        /// Rules to include (if empty, all rules are included).
        /// </summary>
        public HashSet<string> IncludedRules { get; set; } = new HashSet<string>();

        /// <summary>
        /// Minimum severity to report.
        /// </summary>
        public DiagnosticSeverityLevel MinimumSeverity { get; set; } = DiagnosticSeverityLevel.Info;

        /// <summary>
        /// Whether to respect .editorconfig files.
        /// </summary>
        public bool UseEditorConfig { get; set; } = true;

        /// <summary>
        /// Specific MSBuild configuration (Debug/Release).
        /// </summary>
        public string Configuration { get; set; } = "Debug";

        /// <summary>
        /// Specific target framework (if multi-targeting).
        /// </summary>
        public string TargetFramework { get; set; }

        /// <summary>
        /// Whether to analyze generated code.
        /// </summary>
        public bool AnalyzeGeneratedCode { get; set; } = false;

        /// <summary>
        /// File path patterns to exclude.
        /// </summary>
        public List<string> ExcludedFilePatterns { get; set; } = new List<string>();
    }
}
```

---

## Implementation Details

### WorkspaceLoader

```csharp
namespace CodeCop.Core.Workspace
{
    internal class WorkspaceLoader : IDisposable
    {
        private MSBuildWorkspace _workspace;
        private bool _msbuildRegistered = false;

        public async Task<Solution> LoadSolutionAsync(
            string solutionPath,
            IAnalysisProgress progress,
            CancellationToken cancellationToken)
        {
            EnsureMSBuildRegistered(progress);

            progress?.OnPhaseChanged(AnalysisPhase.LoadingWorkspace, $"Loading {Path.GetFileName(solutionPath)}");

            _workspace = MSBuildWorkspace.Create();
            _workspace.WorkspaceFailed += (sender, args) =>
            {
                // Log workspace failures (missing references, etc.)
                Debug.WriteLine($"Workspace warning: {args.Diagnostic.Message}");
            };

            return await _workspace.OpenSolutionAsync(solutionPath, progress: null, cancellationToken);
        }

        public async Task<Project> LoadProjectAsync(
            string projectPath,
            IAnalysisProgress progress,
            CancellationToken cancellationToken)
        {
            EnsureMSBuildRegistered(progress);

            progress?.OnPhaseChanged(AnalysisPhase.LoadingProject, $"Loading {Path.GetFileName(projectPath)}");

            _workspace = MSBuildWorkspace.Create();
            return await _workspace.OpenProjectAsync(projectPath, progress: null, cancellationToken);
        }

        private void EnsureMSBuildRegistered(IAnalysisProgress progress)
        {
            if (_msbuildRegistered)
                return;

            progress?.OnPhaseChanged(AnalysisPhase.LocatingMSBuild, "Locating .NET SDK...");

            // Register MSBuild instance
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                if (instances.Count == 0)
                {
                    throw new InvalidOperationException(
                        "No .NET SDK found. Please install the .NET SDK.");
                }

                // Prefer the latest version
                var latest = instances.OrderByDescending(i => i.Version).First();
                MSBuildLocator.RegisterInstance(latest);
            }

            _msbuildRegistered = true;
        }

        public void Dispose()
        {
            _workspace?.Dispose();
        }
    }
}
```

### AnalyzerRunner

```csharp
namespace CodeCop.Core.Analysis
{
    internal class AnalyzerRunner
    {
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;

        public AnalyzerRunner()
        {
            _analyzers = AnalyzerRegistry.GetAllAnalyzers();
        }

        public async Task<List<CopDiagnostic>> AnalyzeProjectAsync(
            Project project,
            AnalysisOptions options,
            IAnalysisProgress progress,
            CancellationToken cancellationToken)
        {
            progress?.OnPhaseChanged(AnalysisPhase.Compiling, $"Compiling {project.Name}...");

            var compilation = await project.GetCompilationAsync(cancellationToken);
            if (compilation == null)
            {
                throw new InvalidOperationException($"Failed to compile project: {project.Name}");
            }

            progress?.OnPhaseChanged(AnalysisPhase.Analyzing, $"Analyzing {project.Name}...");

            // Filter analyzers based on options
            var analyzersToRun = FilterAnalyzers(options);

            var compilationWithAnalyzers = compilation.WithAnalyzers(
                analyzersToRun,
                project.AnalyzerOptions,
                cancellationToken);

            var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken);

            // Convert to CopDiagnostic models
            return diagnostics
                .Where(d => MeetsSeverityThreshold(d, options))
                .Where(d => !IsExcluded(d, options))
                .Select(d => MapToCopDiagnostic(d, project.Name))
                .ToList();
        }

        private ImmutableArray<DiagnosticAnalyzer> FilterAnalyzers(AnalysisOptions options)
        {
            if (options == null)
                return _analyzers;

            return _analyzers
                .Where(a =>
                {
                    var ids = a.SupportedDiagnostics.Select(d => d.Id);
                    if (options.IncludedRules.Any())
                    {
                        return ids.Any(id => options.IncludedRules.Contains(id));
                    }
                    return !ids.All(id => options.ExcludedRules.Contains(id));
                })
                .ToImmutableArray();
        }

        private CopDiagnostic MapToCopDiagnostic(Diagnostic diagnostic, string projectName)
        {
            var span = diagnostic.Location.GetLineSpan();

            return new CopDiagnostic
            {
                Id = diagnostic.Id,
                Message = diagnostic.GetMessage(),
                Severity = (DiagnosticSeverityLevel)diagnostic.Severity,
                Category = diagnostic.Descriptor.Category,
                File = span.Path ?? "",
                LineNumber = span.StartLinePosition.Line + 1,
                ColumnNumber = span.StartLinePosition.Character + 1,
                EndLineNumber = span.EndLinePosition.Line + 1,
                EndColumnNumber = span.EndLinePosition.Character + 1,
                ProjectName = projectName,
                HasCodeFix = HasCodeFix(diagnostic.Id)
            };
        }

        private bool HasCodeFix(string diagnosticId)
        {
            // Map diagnostic IDs to whether they have code fixes
            var rulesWithFixes = new HashSet<string>
            {
                "CCS0001", "CCS0002", "CCS0003", "CCS0004", "CCS0005"
            };
            return rulesWithFixes.Contains(diagnosticId);
        }
    }
}
```

### AnalyzerRegistry

```csharp
namespace CodeCop.Core.Analysis
{
    /// <summary>
    /// Discovers and provides access to all CodeCop analyzers.
    /// </summary>
    public static class AnalyzerRegistry
    {
        private static readonly Lazy<ImmutableArray<DiagnosticAnalyzer>> _analyzers =
            new Lazy<ImmutableArray<DiagnosticAnalyzer>>(DiscoverAnalyzers);

        /// <summary>
        /// Gets all available analyzers.
        /// </summary>
        public static ImmutableArray<DiagnosticAnalyzer> GetAllAnalyzers() => _analyzers.Value;

        /// <summary>
        /// Gets information about all available rules.
        /// </summary>
        public static IReadOnlyList<RuleInfo> GetRuleInfos()
        {
            return _analyzers.Value
                .SelectMany(a => a.SupportedDiagnostics)
                .Select(d => new RuleInfo
                {
                    Id = d.Id,
                    Title = d.Title.ToString(),
                    Description = d.Description.ToString(),
                    Category = d.Category,
                    DefaultSeverity = (DiagnosticSeverityLevel)d.DefaultSeverity,
                    IsEnabledByDefault = d.IsEnabledByDefault
                })
                .OrderBy(r => r.Id)
                .ToList();
        }

        private static ImmutableArray<DiagnosticAnalyzer> DiscoverAnalyzers()
        {
            // Get analyzers from CodeCop.Sharp assembly
            var analyzerAssembly = typeof(MethodDeclarationAnalyzer).Assembly;

            var analyzers = analyzerAssembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DiagnosticAnalyzer)) && !t.IsAbstract)
                .Select(t => (DiagnosticAnalyzer)Activator.CreateInstance(t))
                .ToImmutableArray();

            return analyzers;
        }
    }

    public class RuleInfo
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public DiagnosticSeverityLevel DefaultSeverity { get; set; }
        public bool IsEnabledByDefault { get; set; }
    }
}
```

---

## Workflow Diagrams

### Analysis Workflow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         AnalyzeSolutionAsync                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Phase: Initializing                                                          │
│ - Validate solution path exists                                              │
│ - Initialize AnalysisReport                                                  │
│ - Start timer                                                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Phase: LocatingMSBuild                                                       │
│ - Query Visual Studio instances                                              │
│ - Select latest .NET SDK                                                     │
│ - Register MSBuildLocator                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Phase: LoadingWorkspace                                                      │
│ - Create MSBuildWorkspace                                                    │
│ - OpenSolutionAsync(path)                                                    │
│ - Handle workspace failures (log warnings)                                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ For Each Project in Solution                                                 │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Phase: Compiling                                                        │ │
│ │ - GetCompilationAsync()                                                 │ │
│ │ - Handle compilation errors                                             │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│                                    ▼                                         │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Phase: Analyzing                                                        │ │
│ │ - Create CompilationWithAnalyzers                                       │ │
│ │ - GetAnalyzerDiagnosticsAsync()                                         │ │
│ │ - Filter by severity and excluded rules                                 │ │
│ │ - Map to CopDiagnostic models                                           │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│                                    ▼                                         │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Add to AnalyzedProject list                                             │ │
│ │ Report progress: OnProjectCompleted                                     │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Phase: GeneratingReport                                                      │
│ - Calculate SummaryStats                                                     │
│ - Group diagnostics by rule and category                                     │
│ - Stop timer, set Duration                                                   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ Phase: Complete                                                              │
│ - Return AnalysisReport                                                      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### MSBuild Registration Decision

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       EnsureMSBuildRegistered()                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
              ┌─────────────────────▼─────────────────────┐
              │ MSBuildLocator.IsRegistered?              │
              └─────────────────────┬─────────────────────┘
                       │                      │
                      YES                    NO
                       │                      │
                       ▼                      ▼
                ┌──────────┐   ┌────────────────────────────────┐
                │ Return   │   │ QueryVisualStudioInstances()   │
                │ (done)   │   └───────────────┬────────────────┘
                └──────────┘                   │
                                               ▼
                              ┌─────────────────────────────────┐
                              │ Any instances found?            │
                              └─────────────────┬───────────────┘
                                     │                  │
                                    YES                NO
                                     │                  │
                                     ▼                  ▼
                    ┌─────────────────────┐   ┌────────────────────┐
                    │ Select latest       │   │ Throw exception:   │
                    │ by version          │   │ "No .NET SDK found"│
                    └──────────┬──────────┘   └────────────────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │ RegisterInstance()  │
                    └─────────────────────┘
```

---

## Project File

### CodeCop.Core.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <PackageId>CodeCop.Core</PackageId>
    <Version>0.2.0</Version>
    <Authors>CodeCop Contributors</Authors>
    <Description>Analysis runner library for CodeCop.Sharp</Description>
    <PackageTags>roslyn;analyzer;code-quality;csharp</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <!-- MSBuild Workspace support -->
    <PackageReference Include="Microsoft.Build.Locator" Version="1.6.10" />
    <PackageReference Include="Microsoft.CodeAnalysis.Workspaces.MSBuild" Version="4.2.0" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.2.0" />

    <!-- Reference to analyzer library -->
    <ProjectReference Include="..\CodeCop.Sharp\CodeCop.Sharp.csproj" />
  </ItemGroup>

</Project>
```

---

## Test Strategy

### Unit Tests

```csharp
public class CodeCopRunnerTests
{
    [Fact]
    public async Task AnalyzeProjectAsync_ValidProject_ReturnsReport()
    {
        // Arrange
        var runner = new CodeCopRunner();
        var projectPath = GetTestProjectPath();

        // Act
        var report = await runner.AnalyzeProjectAsync(projectPath);

        // Assert
        Assert.NotNull(report);
        Assert.True(report.Success);
        Assert.Single(report.Projects);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithDiagnostics_IncludesAllProjects()
    {
        // Arrange
        var runner = new CodeCopRunner();
        var solutionPath = GetTestSolutionPath();

        // Act
        var report = await runner.AnalyzeSolutionAsync(solutionPath);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.Projects.Count); // Assuming 2 projects in test solution
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithExcludedRules_FiltersResults()
    {
        // Arrange
        var runner = new CodeCopRunner();
        var options = new AnalysisOptions
        {
            ExcludedRules = new HashSet<string> { "CCS0001" }
        };

        // Act
        var report = await runner.AnalyzeProjectAsync(GetTestProjectPath(), options);

        // Assert
        Assert.DoesNotContain(report.Projects[0].Diagnostics, d => d.Id == "CCS0001");
    }
}
```

### Integration Tests

```csharp
public class WorkspaceLoaderIntegrationTests
{
    [Fact]
    public async Task LoadSolutionAsync_ExistingSolution_LoadsSuccessfully()
    {
        using var loader = new WorkspaceLoader();
        var solution = await loader.LoadSolutionAsync(
            GetRealSolutionPath(),
            progress: null,
            CancellationToken.None);

        Assert.NotNull(solution);
        Assert.NotEmpty(solution.Projects);
    }
}
```

---

## Deliverable Checklist

### Project Setup
- [ ] Create `CodeCop.Core` project
- [ ] Add NuGet dependencies (MSBuild.Locator, Workspaces.MSBuild)
- [ ] Add project reference to CodeCop.Sharp
- [ ] Configure netstandard2.0 target

### Core Interfaces
- [ ] Implement `ICodeCopRunner`
- [ ] Implement `IAnalysisProgress`
- [ ] Define `AnalysisPhase` enum

### Data Models
- [ ] Implement `AnalysisReport`
- [ ] Implement `AnalyzedProject`
- [ ] Implement `CopDiagnostic`
- [ ] Implement `SummaryStats`
- [ ] Implement `AnalysisOptions`
- [ ] Implement `RuleInfo`
- [ ] Implement `DiagnosticSeverityLevel` enum

### Implementation
- [ ] Implement `CodeCopRunner`
- [ ] Implement `WorkspaceLoader`
- [ ] Implement `AnalyzerRunner`
- [ ] Implement `AnalyzerRegistry`
- [ ] Handle MSBuild registration
- [ ] Handle workspace failures gracefully

### Testing
- [ ] Create `CodeCop.Core.Tests` project
- [ ] Write unit tests for CodeCopRunner
- [ ] Write unit tests for AnalyzerRegistry
- [ ] Create test project/solution fixtures
- [ ] Write integration tests

### Documentation
- [ ] Add XML documentation comments
- [ ] Create README for CodeCop.Core

---

## Error Handling

| Scenario | Handling |
|----------|----------|
| Solution not found | Throw `FileNotFoundException` |
| No .NET SDK installed | Throw `InvalidOperationException` with message |
| Project fails to load | Log warning, set `AnalyzedProject.Success = false` |
| Project fails to compile | Log warning, continue with other projects |
| Analyzer throws exception | Log warning, continue analysis |
| Cancellation requested | Check token throughout, throw `OperationCanceledException` |
