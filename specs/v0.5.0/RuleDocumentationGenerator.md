# Rule Documentation Generator

## Overview

Automatically generates Markdown documentation for all analyzers from their metadata and attributes.

## Generated Documentation Structure

```
docs/rules/
├── index.md           # Rule index with summary table
├── CCS0001.md         # Individual rule documentation
├── CCS0002.md
├── ...
└── CCS0036.md
```

## Generated Rule Documentation Template

```markdown
# CCS0030: AsyncMethodNaming

| Property | Value |
|----------|-------|
| Rule ID | CCS0030 |
| Category | BestPractices |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Async methods that return Task, Task<T>, ValueTask, or ValueTask<T> should have names
ending with "Async". This naming convention makes it clear to callers that the method
is asynchronous and should be awaited.

## Why This Matters

- **Clarity**: The "Async" suffix signals asynchronous behavior
- **Consistency**: Standard .NET naming convention
- **Discoverability**: Easy to identify async methods in IntelliSense

## Examples

### Non-Compliant Code

```csharp
public async Task LoadData()  // Missing Async suffix
{
    await Task.Delay(100);
}
```

### Compliant Code

```csharp
public async Task LoadDataAsync()
{
    await Task.Delay(100);
}
```

## How to Fix

Add the "Async" suffix to the method name.

## Exceptions

- `Main` entry point methods
- Event handlers (async void with EventArgs parameter)
- Override methods (must match base class)
- Interface implementations (must match interface)

## Configuration

This rule has no configurable options.

## Related Rules

- [CCS0031: AvoidAsyncVoid](CCS0031.md)
- [CCS0032: ConfigureAwaitFalse](CCS0032.md)
```

## Implementation

```csharp
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCop.CLI.Documentation
{
    public class RuleDocumentationGenerator
    {
        public async Task GenerateAsync(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);

            var assembly = typeof(CodeCop.Sharp.Analyzers.Naming.MethodDeclarationAnalyzer).Assembly;
            var analyzerTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(DiagnosticAnalyzer)) && !t.IsAbstract);

            var rules = new List<RuleInfo>();

            foreach (var analyzerType in analyzerTypes)
            {
                var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!;
                foreach (var descriptor in analyzer.SupportedDiagnostics)
                {
                    var ruleInfo = new RuleInfo
                    {
                        Id = descriptor.Id,
                        Title = descriptor.Title.ToString(),
                        Category = descriptor.Category,
                        Severity = descriptor.DefaultSeverity.ToString(),
                        Description = descriptor.Description.ToString(),
                        IsEnabledByDefault = descriptor.IsEnabledByDefault,
                        HasCodeFix = HasCodeFix(descriptor.Id, assembly)
                    };
                    rules.Add(ruleInfo);

                    // Generate individual rule file
                    await GenerateRuleFileAsync(outputDirectory, ruleInfo);
                }
            }

            // Generate index file
            await GenerateIndexAsync(outputDirectory, rules);
        }

        private async Task GenerateRuleFileAsync(string outputDir, RuleInfo rule)
        {
            var content = new StringBuilder();
            content.AppendLine($"# {rule.Id}: {rule.Title}");
            content.AppendLine();
            content.AppendLine("| Property | Value |");
            content.AppendLine("|----------|-------|");
            content.AppendLine($"| Rule ID | {rule.Id} |");
            content.AppendLine($"| Category | {rule.Category} |");
            content.AppendLine($"| Severity | {rule.Severity} |");
            content.AppendLine($"| Has Code Fix | {(rule.HasCodeFix ? "Yes" : "No")} |");
            content.AppendLine($"| Enabled by Default | {(rule.IsEnabledByDefault ? "Yes" : "No")} |");
            content.AppendLine();
            content.AppendLine("## Description");
            content.AppendLine();
            content.AppendLine(rule.Description);
            content.AppendLine();
            // Add more sections...

            var filePath = Path.Combine(outputDir, $"{rule.Id}.md");
            await File.WriteAllTextAsync(filePath, content.ToString());
        }

        private async Task GenerateIndexAsync(string outputDir, List<RuleInfo> rules)
        {
            var content = new StringBuilder();
            content.AppendLine("# CodeCop Rules Reference");
            content.AppendLine();
            content.AppendLine("| ID | Name | Category | Severity |");
            content.AppendLine("|----|------|----------|----------|");

            foreach (var rule in rules.OrderBy(r => r.Id))
            {
                content.AppendLine($"| [{rule.Id}]({rule.Id}.md) | {rule.Title} | {rule.Category} | {rule.Severity} |");
            }

            var filePath = Path.Combine(outputDir, "index.md");
            await File.WriteAllTextAsync(filePath, content.ToString());
        }

        private static bool HasCodeFix(string diagnosticId, Assembly assembly)
        {
            var codeFixTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(CodeFixProvider)) && !t.IsAbstract);

            foreach (var type in codeFixTypes)
            {
                var provider = (CodeFixProvider)Activator.CreateInstance(type)!;
                if (provider.FixableDiagnosticIds.Contains(diagnosticId))
                    return true;
            }
            return false;
        }

        private class RuleInfo
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
            public string Severity { get; set; } = "";
            public string Description { get; set; } = "";
            public bool IsEnabledByDefault { get; set; }
            public bool HasCodeFix { get; set; }
        }
    }
}
```

## CLI Command

```bash
# Generate documentation
codecop docs --output ./docs/rules
```
