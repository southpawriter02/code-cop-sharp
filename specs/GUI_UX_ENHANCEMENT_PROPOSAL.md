# CodeCop.Sharp GUI/UX Enhancement Proposal and Implementation Specifications

## 1. Executive Summary

This document outlines the comprehensive proposal for enhancing the User Interface and User Experience of the `CodeCop.Sharp` ecosystem. It details the implementation specifications for a shared Core Runner, a Command-Line Interface (CLI), a Cross-Platform Desktop GUI (AvaloniaUI), and a Web Dashboard.

## 2. Architecture Overview

To support multiple interfaces (CLI, GUI, Web) while maintaining a single source of truth for analysis logic, the system will be refactored into a layered architecture.

### Proposed Solution Structure

```text
CodeCop.Sharp.sln
├── CodeCop.Sharp (Existing)        # The Roslyn Analyzer Library (The "Rules")
├── CodeCop.Core (New)              # The Analysis Runner & Common Models
├── CodeCop.CLI (New)               # Command-Line Interface
├── CodeCop.GUI (New)               # AvaloniaUI Desktop Application
└── CodeCop.Dashboard (New)         # Web Dashboard (ASP.NET Core)
```

## 3. CodeCop.Core (The Runner)

The `CodeCop.Core` library is the foundation for all UI clients. It abstracts the complexity of loading MSBuild projects and executing Roslyn analyzers.

### 3.1. Responsibilities
- **Workspace Management:** Locating MSBuild and loading Solutions (`.sln`) or Projects (`.csproj`).
- **Analyzer Execution:** Instantiating the `CodeCop.Sharp` analyzer (and potentially 3rd party ones) and running them against the loaded compilation.
- **Reporting:** Normalizing diagnostic results into a portable format (POCOs) that can be consumed by CLI (text/json), GUI (ViewModels), and Web (JSON uploads).

### 3.2. Technical Implementation Details

**Dependencies:**
- `Microsoft.Build.Locator`: To locate the SDK on the machine.
- `Microsoft.CodeAnalysis.Workspaces.MSBuild`: To open projects/solutions.
- `Microsoft.CodeAnalysis.CSharp`: For compilation and analysis.

**Key Interfaces:**

```csharp
public interface ICodeCopRunner
{
    Task<AnalysisReport> AnalyzeProjectAsync(string projectPath, CancellationToken ct);
    Task<AnalysisReport> AnalyzeSolutionAsync(string solutionPath, CancellationToken ct);
}

public class AnalysisReport
{
    public DateTime Timestamp { get; set; }
    public List<AnalyzedProject> Projects { get; set; }
    public SummaryStats Stats { get; set; }
}

public class AnalyzedProject
{
    public string ProjectName { get; set; }
    public string FilePath { get; set; }
    public List<CopDiagnostic> Diagnostics { get; set; }
}

public class CopDiagnostic
{
    public string Id { get; set; }
    public string Message { get; set; }
    public string Severity { get; set; } // Error, Warning, Info
    public string File { get; set; }
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
}
```

**Workflow:**
1. **Discovery:** Use `MSBuildLocator.RegisterDefaults()` to bind to the host's .NET SDK.
2. **Loading:** Create an `MSBuildWorkspace`. Call `OpenSolutionAsync(path)`.
3. **Compilation:** Iterate through `Project`s in the `Solution`. Call `GetCompilationAsync()`.
4. **Analysis:**
   - Create a `CompilationWithAnalyzers` instance, passing in the `MethodDeclarationAnalyzer` (and others from `CodeCop.Sharp`).
   - Call `GetAnalyzerDiagnosticsAsync()`.
5. **Output:** Map `Diagnostic` objects to `CopDiagnostic` POCOs.

## 4. CodeCop.CLI (Command-Line Interface)

The CLI acts as the primary tool for CI/CD integration and power users. It leverages `CodeCop.Core` to perform analysis and output results in machine-readable formats.

### 4.1. Commands

#### `analyze`
Runs the static analysis.

**Arguments:**
- `-t, --target <PATH>`: (Required) Path to the .sln or .csproj file.
- `-f, --format <FORMAT>`: Output format. Options: `Console` (default), `Json`, `Xml`, `Sarif`.
- `-o, --output <PATH>`: File path to write the output to. If omitted, prints to stdout.
- `--fail-on-error`: Returns a non-zero exit code if any error-level diagnostics are found.

**Example:**
```bash
codecop analyze --target ./MySolution.sln --format Json --output ./report.json
```

#### `rules`
Lists all available rules supported by the analyzer.

### 4.2. Implementation Strategy
- **Framework:** `System.CommandLine` for robust argument parsing and help text generation.
- **Output:**
  - **Console:** ANSI colored output for human readability (Red for errors, Yellow for warnings).
  - **Machine:** Standard JSON serialization or SARIF (Static Analysis Results Interchange Format) for GitHub Actions integration.

## 5. CodeCop.GUI (AvaloniaUI Desktop App)

The Desktop GUI provides an interactive environment for developers to audit their codebases without needing to run a build or check CLI logs.

### 5.1. Tech Stack
- **Framework:** AvaloniaUI (Cross-platform: Windows, macOS, Linux).
- **Design Pattern:** MVVM (Model-View-ViewModel) using `CommunityToolkit.Mvvm`.
- **Components:**
  - `AvaloniaEdit`: For displaying code with syntax highlighting and squiggles.
  - `FluentAvalonia`: For modern, fluent design aesthetics.

### 5.2. UI Layout & UX

**Main Window Layout:**
1. **Sidebar (Project Explorer):** Tree view of the loaded Solution/Projects. Indicators (icons/colors) show which files have issues.
2. **Central Area (Code Editor):** Read-only view of the selected file using `AvaloniaEdit`.
   - **Enhancement:** Underline the problematic code (squiggles) based on the diagnostic Line/Column data.
   - **Tooltip:** Hovering over the error shows the `Message` and `Id`.
3. **Bottom Panel (Diagnostic List):** A data grid listing all issues found.
   - Columns: `Code` (e.g., CCS0001), `Severity`, `Description`, `File`, `Line`.
   - **Interaction:** Double-clicking a row navigates the Central Area to the file and line.

**User Workflow:**
1. User clicks "Open Solution..." and selects a `.sln` file.
2. App shows a progress bar ("Loading Workspace...", "Analyzing...").
3. Once complete, the Tree View is populated. Files with errors are highlighted Red.
4. User selects a file. Code is displayed.
5. User fixes the issue in their IDE (external).
6. User clicks "Re-Analyze" in the GUI to refresh.

## 6. CodeCop.Dashboard (Web Dashboard)

The Web Dashboard serves as a long-term storage and visualization platform for team leads and managers to track code quality trends over time.

### 6.1. Architecture

**Data Flow:**
1. **CI Pipeline:** Runs `CodeCop.CLI` to generate a JSON report.
2. **Upload:** Pipeline uploads the JSON report to the Dashboard API.
3. **Storage:** Dashboard saves the report to a database (e.g., SQLite for local/lightweight, PostgreSQL for prod).
4. **Visualization:** Web Frontend fetches aggregated data to display charts.

### 6.2. Tech Stack
- **Backend:** ASP.NET Core Web API.
- **Frontend:** Blazor WebAssembly (allows sharing C# models with the Core/CLI) OR React. **Recommendation: Blazor WebAssembly** for code sharing.
- **Database:** Entity Framework Core.

### 6.3. Features

**Pages:**
- **Overview:** Aggregated stats (Total Errors, Warning Count, Pass Rate).
- **Trends:** Line charts showing "Issues over Time" (Last 30 builds).
- **Project Detail:** Drill down into specific projects to see which rules are violated most frequently.
- **Leaderboard:** (Optional) Top "Cleanest" projects.

## 7. Implementation Roadmap

### Phase 1: Foundation
1. Create `CodeCop.Core` and extract logic.
2. Create `CodeCop.CLI` and implement `analyze` command.
3. Verify CLI output matches existing analyzer behavior.

### Phase 2: Desktop GUI
1. Initialize `CodeCop.GUI` Avalonia project.
2. Connect `CodeCop.Core` to the GUI.
3. Implement Project Tree and Diagnostic Grid.
4. Integrate `AvaloniaEdit` for code viewing.

### Phase 3: Web Dashboard
1. Create `CodeCop.Dashboard` API and Database schema.
2. Implement JSON upload endpoint.
3. Build Blazor frontend with Trend Charts.
