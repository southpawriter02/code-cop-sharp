# v0.7.0 "Visual" - Specification Overview

## Overview

| Property | Value |
|----------|-------|
| Version | v0.7.0 |
| Theme | "Visual" |
| Target Framework | net8.0 |
| Total Analyzers | 30 (no new analyzers) |
| Key Features | Cross-platform desktop GUI application |

## Goals

1. **Desktop Application**: Build a cross-platform GUI for code analysis
2. **Visual Feedback**: Display code with inline issue highlighting
3. **Navigation**: Easy navigation between issues and source files
4. **Export**: Export analysis results in multiple formats

## Project to Create

| Project | Type | Framework | Description |
|---------|------|-----------|-------------|
| `CodeCop.GUI` | AvaloniaUI App | net8.0 | Cross-platform desktop application |

## Tech Stack

| Technology | Purpose |
|------------|---------|
| AvaloniaUI | Cross-platform UI framework |
| AvaloniaEdit | Syntax highlighting code editor |
| FluentAvalonia | Modern Fluent Design styling |
| CommunityToolkit.Mvvm | MVVM architecture support |

## UI Components

### 1. Sidebar - Solution Explorer
- Tree view of solution/project files
- Issue count badges per file
- Filter by severity
- Search functionality

### 2. Central Panel - Code Viewer
- Syntax-highlighted code display (AvaloniaEdit)
- Inline squiggles for diagnostics
- Hover tooltips with issue details
- Line numbers and code folding

### 3. Bottom Panel - Diagnostics Grid
| Column | Description |
|--------|-------------|
| Code | Rule ID (e.g., CCS0030) |
| Severity | Error/Warning/Info icon |
| Message | Diagnostic message |
| File | Source file path |
| Line | Line number |

### 4. Toolbar
- Open Solution/Project
- Re-Analyze
- Settings
- Export (JSON, SARIF, XML)
- Theme Toggle (Light/Dark)

## Specification Documents

| Document | Description | Status |
|----------|-------------|--------|
| [GUI_Architecture.md](GUI_Architecture.md) | Overall architecture and MVVM structure | To Be Created |
| [MainWindow.md](MainWindow.md) | Main window layout and behavior | To Be Created |
| [SolutionExplorer.md](SolutionExplorer.md) | Sidebar component specification | To Be Created |
| [CodeViewer.md](CodeViewer.md) | Code display component specification | To Be Created |
| [DiagnosticsGrid.md](DiagnosticsGrid.md) | Bottom panel component specification | To Be Created |
| [Settings.md](Settings.md) | Settings/preferences system | To Be Created |
| [Export.md](Export.md) | Export functionality | To Be Created |

## File Structure

```
CodeCop.GUI/
├── App.axaml
├── App.axaml.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs
│   ├── SolutionExplorerViewModel.cs
│   ├── CodeViewerViewModel.cs
│   ├── DiagnosticsGridViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── MainWindow.axaml
│   ├── SolutionExplorerView.axaml
│   ├── CodeViewerView.axaml
│   ├── DiagnosticsGridView.axaml
│   └── SettingsView.axaml
├── Models/
│   ├── SolutionTreeItem.cs
│   ├── DiagnosticItem.cs
│   └── AppSettings.cs
├── Services/
│   ├── AnalysisService.cs
│   ├── ExportService.cs
│   └── SettingsService.cs
└── Resources/
    ├── Icons/
    └── Themes/
```

## Implementation Order

1. **Phase 1: Project Setup**
   - Create AvaloniaUI project
   - Set up MVVM architecture
   - Configure FluentAvalonia theme

2. **Phase 2: Core UI**
   - Main window layout
   - Toolbar implementation
   - Basic navigation

3. **Phase 3: Solution Explorer**
   - Tree view component
   - File loading
   - Issue indicators

4. **Phase 4: Code Viewer**
   - AvaloniaEdit integration
   - Syntax highlighting
   - Diagnostic squiggles

5. **Phase 5: Diagnostics Grid**
   - Grid component
   - Sorting and filtering
   - Navigation to source

6. **Phase 6: Polish**
   - Settings persistence
   - Export functionality
   - Theme support

## Deliverable Checklist

- [ ] Create `CodeCop.GUI` project
- [ ] Set up AvaloniaUI with FluentAvalonia
- [ ] Implement MainWindowViewModel
- [ ] Implement SolutionExplorerViewModel
- [ ] Implement CodeViewerViewModel
- [ ] Implement DiagnosticsGridViewModel
- [ ] Integrate with CodeCop.Core for analysis
- [ ] Implement settings system
- [ ] Implement export functionality
- [ ] Add light/dark theme support
- [ ] Create application icons
- [ ] Write basic UI tests
