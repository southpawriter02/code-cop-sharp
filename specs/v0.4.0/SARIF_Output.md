# SARIF Output Formatter

## Overview

| Property | Value |
|----------|-------|
| Feature | SARIF Output Format |
| Version | SARIF 2.1.0 |
| Component | CodeCop.CLI |
| File | `Formatters/SarifFormatter.cs` |

## Description

SARIF (Static Analysis Results Interchange Format) is a JSON-based standard for representing static analysis results. It's an OASIS standard supported by major CI/CD platforms and development tools.

### Why SARIF?

1. **GitHub Integration**: GitHub Advanced Security displays SARIF results
2. **Azure DevOps**: Native SARIF support in pipelines
3. **VS Code**: SARIF Viewer extension
4. **Visual Studio**: Built-in SARIF support
5. **Standardization**: Industry-standard format
6. **Rich Metadata**: Supports rules, locations, fixes, and more

---

## Supported Platforms

| Platform | Support Level | Integration |
|----------|---------------|-------------|
| GitHub Actions | Full | `github/codeql-action/upload-sarif@v2` |
| GitHub Advanced Security | Full | Security tab, code scanning alerts |
| Azure DevOps | Full | Publish Build Artifacts + SARIF tab |
| Visual Studio | Full | Error List integration |
| VS Code | Full | SARIF Viewer extension |
| JetBrains IDEs | Partial | Via plugins |

---

## CLI Usage

```bash
# Generate SARIF report
codecop analyze --target ./MySolution.sln --format Sarif --output results.sarif

# Analyze and output to stdout
codecop analyze --target ./MyProject.csproj --format Sarif

# Combine with other options
codecop analyze --target ./MySolution.sln --format Sarif --output results.sarif --min-severity Warning
```

---

## SARIF Structure

### Overview

```json
{
  "$schema": "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
  "version": "2.1.0",
  "runs": [
    {
      "tool": { /* tool information */ },
      "results": [ /* array of results */ ]
    }
  ]
}
```

### Complete Example

```json
{
  "$schema": "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
  "version": "2.1.0",
  "runs": [
    {
      "tool": {
        "driver": {
          "name": "CodeCop",
          "version": "0.4.0",
          "informationUri": "https://github.com/southpawriter02/code-cop-sharp",
          "rules": [
            {
              "id": "CCS0020",
              "name": "UnusedPrivateField",
              "shortDescription": {
                "text": "Unused private field"
              },
              "fullDescription": {
                "text": "Private fields that are never read should be removed to improve maintainability."
              },
              "defaultConfiguration": {
                "level": "warning"
              },
              "helpUri": "https://github.com/southpawriter02/code-cop-sharp/blob/main/docs/rules/CCS0020.md",
              "properties": {
                "category": "Quality"
              }
            },
            {
              "id": "CCS0023",
              "name": "MethodTooLong",
              "shortDescription": {
                "text": "Method too long"
              },
              "fullDescription": {
                "text": "Methods should be concise. Consider breaking long methods into smaller, focused methods."
              },
              "defaultConfiguration": {
                "level": "warning"
              },
              "helpUri": "https://github.com/southpawriter02/code-cop-sharp/blob/main/docs/rules/CCS0023.md",
              "properties": {
                "category": "Quality"
              }
            }
          ]
        }
      },
      "results": [
        {
          "ruleId": "CCS0020",
          "ruleIndex": 0,
          "level": "warning",
          "message": {
            "text": "Private field '_unusedField' is never used"
          },
          "locations": [
            {
              "physicalLocation": {
                "artifactLocation": {
                  "uri": "src/MyClass.cs",
                  "uriBaseId": "SRCROOT"
                },
                "region": {
                  "startLine": 10,
                  "startColumn": 17,
                  "endLine": 10,
                  "endColumn": 29
                }
              }
            }
          ]
        },
        {
          "ruleId": "CCS0023",
          "ruleIndex": 1,
          "level": "warning",
          "message": {
            "text": "Method 'ProcessData' has 75 lines, exceeding the maximum of 50"
          },
          "locations": [
            {
              "physicalLocation": {
                "artifactLocation": {
                  "uri": "src/DataProcessor.cs",
                  "uriBaseId": "SRCROOT"
                },
                "region": {
                  "startLine": 25,
                  "startColumn": 17,
                  "endLine": 25,
                  "endColumn": 28
                }
              }
            }
          ]
        }
      ]
    }
  ]
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.CLI/
├── Formatters/
│   ├── IOutputFormatter.cs
│   └── SarifFormatter.cs
└── Models/
    └── Sarif/
        ├── SarifLog.cs
        ├── SarifRun.cs
        ├── SarifTool.cs
        ├── SarifDriver.cs
        ├── SarifRule.cs
        ├── SarifResult.cs
        ├── SarifLocation.cs
        ├── SarifPhysicalLocation.cs
        ├── SarifArtifactLocation.cs
        ├── SarifRegion.cs
        └── SarifMessage.cs
```

### SARIF Model Classes

```csharp
using System.Text.Json.Serialization;

namespace CodeCop.CLI.Models.Sarif
{
    /// <summary>
    /// Root SARIF log object.
    /// </summary>
    public class SarifLog
    {
        [JsonPropertyName("$schema")]
        public string Schema { get; set; } =
            "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "2.1.0";

        [JsonPropertyName("runs")]
        public SarifRun[] Runs { get; set; } = Array.Empty<SarifRun>();
    }

    /// <summary>
    /// Represents a single analysis run.
    /// </summary>
    public class SarifRun
    {
        [JsonPropertyName("tool")]
        public SarifTool Tool { get; set; } = new();

        [JsonPropertyName("results")]
        public SarifResult[] Results { get; set; } = Array.Empty<SarifResult>();

        [JsonPropertyName("originalUriBaseIds")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, SarifArtifactLocation>? OriginalUriBaseIds { get; set; }
    }

    /// <summary>
    /// Tool information.
    /// </summary>
    public class SarifTool
    {
        [JsonPropertyName("driver")]
        public SarifDriver Driver { get; set; } = new();
    }

    /// <summary>
    /// The analysis tool driver.
    /// </summary>
    public class SarifDriver
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "CodeCop";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "0.4.0";

        [JsonPropertyName("informationUri")]
        public string InformationUri { get; set; } =
            "https://github.com/southpawriter02/code-cop-sharp";

        [JsonPropertyName("rules")]
        public SarifRule[] Rules { get; set; } = Array.Empty<SarifRule>();
    }

    /// <summary>
    /// Rule definition.
    /// </summary>
    public class SarifRule
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("shortDescription")]
        public SarifMessage? ShortDescription { get; set; }

        [JsonPropertyName("fullDescription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SarifMessage? FullDescription { get; set; }

        [JsonPropertyName("defaultConfiguration")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SarifDefaultConfiguration? DefaultConfiguration { get; set; }

        [JsonPropertyName("helpUri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? HelpUri { get; set; }

        [JsonPropertyName("properties")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Properties { get; set; }
    }

    /// <summary>
    /// Default configuration for a rule.
    /// </summary>
    public class SarifDefaultConfiguration
    {
        [JsonPropertyName("level")]
        public string Level { get; set; } = "warning";
    }

    /// <summary>
    /// A single analysis result (diagnostic).
    /// </summary>
    public class SarifResult
    {
        [JsonPropertyName("ruleId")]
        public string RuleId { get; set; } = string.Empty;

        [JsonPropertyName("ruleIndex")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RuleIndex { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = "warning";

        [JsonPropertyName("message")]
        public SarifMessage Message { get; set; } = new();

        [JsonPropertyName("locations")]
        public SarifLocation[] Locations { get; set; } = Array.Empty<SarifLocation>();
    }

    /// <summary>
    /// Location of a result.
    /// </summary>
    public class SarifLocation
    {
        [JsonPropertyName("physicalLocation")]
        public SarifPhysicalLocation? PhysicalLocation { get; set; }
    }

    /// <summary>
    /// Physical location in a file.
    /// </summary>
    public class SarifPhysicalLocation
    {
        [JsonPropertyName("artifactLocation")]
        public SarifArtifactLocation? ArtifactLocation { get; set; }

        [JsonPropertyName("region")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SarifRegion? Region { get; set; }
    }

    /// <summary>
    /// Location of an artifact (file).
    /// </summary>
    public class SarifArtifactLocation
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("uriBaseId")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? UriBaseId { get; set; }
    }

    /// <summary>
    /// Region within a file.
    /// </summary>
    public class SarifRegion
    {
        [JsonPropertyName("startLine")]
        public int StartLine { get; set; }

        [JsonPropertyName("startColumn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int StartColumn { get; set; }

        [JsonPropertyName("endLine")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int EndLine { get; set; }

        [JsonPropertyName("endColumn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int EndColumn { get; set; }
    }

    /// <summary>
    /// A text message.
    /// </summary>
    public class SarifMessage
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
```

### SARIF Formatter Implementation

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCop.CLI.Models;
using CodeCop.CLI.Models.Sarif;

namespace CodeCop.CLI.Formatters
{
    /// <summary>
    /// Formats analysis results as SARIF 2.1.0 JSON.
    /// </summary>
    public class SarifFormatter : IOutputFormatter
    {
        private static readonly Dictionary<string, RuleMetadata> RuleDatabase = new()
        {
            ["CCS0001"] = new("MethodPascalCase", "Method names must be PascalCase", "Naming"),
            ["CCS0002"] = new("ClassPascalCase", "Class names must be PascalCase", "Naming"),
            ["CCS0003"] = new("InterfacePrefixI", "Interfaces must start with 'I'", "Naming"),
            ["CCS0004"] = new("PrivateFieldCamelCase", "Private fields should use camelCase", "Naming"),
            ["CCS0005"] = new("ConstantUpperCase", "Constants should use UPPER_CASE or PascalCase", "Naming"),
            ["CCS0010"] = new("BracesRequired", "Control statements should use braces", "Style"),
            ["CCS0011"] = new("PreferVarExplicit", "Prefer explicit type over var", "Style"),
            ["CCS0012"] = new("SingleLineStatements", "Avoid multiple statements on one line", "Style"),
            ["CCS0013"] = new("TrailingWhitespace", "Remove trailing whitespace", "Style"),
            ["CCS0014"] = new("ConsistentNewlines", "Use consistent line endings", "Style"),
            ["CCS0020"] = new("UnusedPrivateField", "Private fields that are never read should be removed", "Quality"),
            ["CCS0021"] = new("UnusedParameter", "Method parameters that are never used should be removed", "Quality"),
            ["CCS0022"] = new("EmptyCatchBlock", "Empty catch blocks should have a comment or handling code", "Quality"),
            ["CCS0023"] = new("MethodTooLong", "Methods should be concise and not exceed line threshold", "Quality"),
            ["CCS0024"] = new("ClassTooLong", "Classes should be concise and not exceed line threshold", "Quality"),
            ["CCS0025"] = new("CyclomaticComplexity", "Methods with high complexity should be refactored", "Quality"),
            ["CCS0026"] = new("TooManyParameters", "Methods should not have too many parameters", "Quality"),
        };

        /// <inheritdoc/>
        public async Task WriteAsync(AnalysisResult result, string? outputPath)
        {
            var sarif = CreateSarifLog(result);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(sarif, options);

            if (outputPath != null)
            {
                await File.WriteAllTextAsync(outputPath, json);
            }
            else
            {
                Console.WriteLine(json);
            }
        }

        private SarifLog CreateSarifLog(AnalysisResult result)
        {
            // Get unique rule IDs from diagnostics
            var ruleIds = result.Diagnostics
                .Select(d => d.Id)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            // Create rule index mapping
            var ruleIndexMap = ruleIds
                .Select((id, index) => (id, index))
                .ToDictionary(x => x.id, x => x.index);

            return new SarifLog
            {
                Runs = new[]
                {
                    new SarifRun
                    {
                        Tool = new SarifTool
                        {
                            Driver = new SarifDriver
                            {
                                Name = "CodeCop",
                                Version = result.Version,
                                Rules = ruleIds.Select(CreateRule).ToArray()
                            }
                        },
                        Results = result.Diagnostics
                            .Select(d => CreateResult(d, ruleIndexMap))
                            .ToArray(),
                        OriginalUriBaseIds = new Dictionary<string, SarifArtifactLocation>
                        {
                            ["SRCROOT"] = new SarifArtifactLocation
                            {
                                Uri = result.TargetDirectory + "/"
                            }
                        }
                    }
                }
            };
        }

        private SarifRule CreateRule(string ruleId)
        {
            var metadata = RuleDatabase.TryGetValue(ruleId, out var m)
                ? m
                : new RuleMetadata(ruleId, $"Rule {ruleId}", "Unknown");

            return new SarifRule
            {
                Id = ruleId,
                Name = metadata.Name,
                ShortDescription = new SarifMessage { Text = metadata.Description },
                DefaultConfiguration = new SarifDefaultConfiguration
                {
                    Level = GetDefaultLevel(ruleId)
                },
                HelpUri = $"https://github.com/southpawriter02/code-cop-sharp/blob/main/docs/rules/{ruleId}.md",
                Properties = new Dictionary<string, string>
                {
                    ["category"] = metadata.Category
                }
            };
        }

        private SarifResult CreateResult(DiagnosticInfo diagnostic, Dictionary<string, int> ruleIndexMap)
        {
            return new SarifResult
            {
                RuleId = diagnostic.Id,
                RuleIndex = ruleIndexMap.TryGetValue(diagnostic.Id, out var idx) ? idx : 0,
                Level = MapSeverity(diagnostic.Severity),
                Message = new SarifMessage { Text = diagnostic.Message },
                Locations = new[]
                {
                    new SarifLocation
                    {
                        PhysicalLocation = new SarifPhysicalLocation
                        {
                            ArtifactLocation = new SarifArtifactLocation
                            {
                                Uri = GetRelativePath(diagnostic.File),
                                UriBaseId = "SRCROOT"
                            },
                            Region = new SarifRegion
                            {
                                StartLine = diagnostic.Line,
                                StartColumn = diagnostic.Column,
                                EndLine = diagnostic.EndLine,
                                EndColumn = diagnostic.EndColumn
                            }
                        }
                    }
                }
            };
        }

        private static string MapSeverity(string severity) => severity.ToLower() switch
        {
            "error" => "error",
            "warning" => "warning",
            "info" => "note",
            "hidden" => "none",
            _ => "warning"
        };

        private static string GetDefaultLevel(string ruleId)
        {
            // Info-level rules
            if (ruleId is "CCS0005" or "CCS0011" or "CCS0013" or "CCS0014")
                return "note";

            return "warning";
        }

        private static string GetRelativePath(string filePath)
        {
            // Convert to forward slashes for URI compatibility
            return filePath.Replace('\\', '/');
        }

        private record RuleMetadata(string Name, string Description, string Category);
    }
}
```

---

## CI/CD Integration

### GitHub Actions

```yaml
name: Code Analysis

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  analyze:
    runs-on: ubuntu-latest
    permissions:
      security-events: write
      contents: read

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Install CodeCop
        run: dotnet tool install -g CodeCop.CLI

      - name: Run CodeCop Analysis
        run: codecop analyze --target ./MySolution.sln --format Sarif --output results.sarif

      - name: Upload SARIF to GitHub
        uses: github/codeql-action/upload-sarif@v2
        with:
          sarif_file: results.sarif
          category: codecop
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '8.0.x'

  - script: dotnet tool install -g CodeCop.CLI
    displayName: 'Install CodeCop'

  - script: codecop analyze --target ./MySolution.sln --format Sarif --output $(Build.ArtifactStagingDirectory)/results.sarif
    displayName: 'Run CodeCop Analysis'

  - task: PublishBuildArtifacts@1
    inputs:
      PathtoPublish: '$(Build.ArtifactStagingDirectory)/results.sarif'
      ArtifactName: 'CodeAnalysisResults'

  - task: PublishCodeAnalysisLogs@1
    inputs:
      sarifFile: '$(Build.ArtifactStagingDirectory)/results.sarif'
```

---

## Test Cases

### Formatter Tests

| Test Name | Description |
|-----------|-------------|
| ValidSarifSchema | Output validates against SARIF 2.1.0 schema |
| EmptyResults | Empty diagnostics produce valid SARIF with empty results |
| SingleResult | Single diagnostic produces correct SARIF structure |
| MultipleResults | Multiple diagnostics produce correct array |
| RuleDeduplication | Unique rules are listed once in rules array |
| SeverityMapping | Error/Warning/Info map to error/warning/note |
| RelativePathsUsed | File paths are relative URIs |
| WriteToFile | Output is written to specified file |
| WriteToStdout | Output is written to console when no file specified |

---

## Test Code Template

```csharp
using System.Text.Json;
using CodeCop.CLI.Formatters;
using CodeCop.CLI.Models;
using CodeCop.CLI.Models.Sarif;
using Xunit;

namespace CodeCop.CLI.Tests.Formatters
{
    public class SarifFormatterTests
    {
        [Fact]
        public async Task WriteAsync_ProducesValidSarifStructure()
        {
            var formatter = new SarifFormatter();
            var result = new AnalysisResult
            {
                Version = "0.4.0",
                TargetDirectory = "/src",
                Diagnostics = new List<DiagnosticInfo>
                {
                    new()
                    {
                        Id = "CCS0020",
                        Severity = "Warning",
                        Message = "Private field '_unused' is never used",
                        File = "src/MyClass.cs",
                        Line = 10,
                        Column = 17
                    }
                }
            };

            using var writer = new StringWriter();
            Console.SetOut(writer);

            await formatter.WriteAsync(result, null);

            var json = writer.ToString();
            var sarif = JsonSerializer.Deserialize<SarifLog>(json);

            Assert.NotNull(sarif);
            Assert.Equal("2.1.0", sarif.Version);
            Assert.Single(sarif.Runs);
            Assert.Equal("CodeCop", sarif.Runs[0].Tool.Driver.Name);
            Assert.Single(sarif.Runs[0].Results);
            Assert.Equal("CCS0020", sarif.Runs[0].Results[0].RuleId);
        }

        [Fact]
        public async Task WriteAsync_MapsServerityCorrectly()
        {
            var formatter = new SarifFormatter();
            var result = new AnalysisResult
            {
                Diagnostics = new List<DiagnosticInfo>
                {
                    new() { Id = "CCS0001", Severity = "Error", Message = "Error", File = "a.cs", Line = 1 },
                    new() { Id = "CCS0002", Severity = "Warning", Message = "Warning", File = "b.cs", Line = 1 },
                    new() { Id = "CCS0003", Severity = "Info", Message = "Info", File = "c.cs", Line = 1 }
                }
            };

            using var writer = new StringWriter();
            Console.SetOut(writer);

            await formatter.WriteAsync(result, null);

            var sarif = JsonSerializer.Deserialize<SarifLog>(writer.ToString());

            Assert.Equal("error", sarif.Runs[0].Results[0].Level);
            Assert.Equal("warning", sarif.Runs[0].Results[1].Level);
            Assert.Equal("note", sarif.Runs[0].Results[2].Level);
        }

        [Fact]
        public async Task WriteAsync_DeduplicatesRules()
        {
            var formatter = new SarifFormatter();
            var result = new AnalysisResult
            {
                Diagnostics = new List<DiagnosticInfo>
                {
                    new() { Id = "CCS0020", Message = "Field 1", File = "a.cs", Line = 1 },
                    new() { Id = "CCS0020", Message = "Field 2", File = "b.cs", Line = 1 },
                    new() { Id = "CCS0020", Message = "Field 3", File = "c.cs", Line = 1 }
                }
            };

            using var writer = new StringWriter();
            Console.SetOut(writer);

            await formatter.WriteAsync(result, null);

            var sarif = JsonSerializer.Deserialize<SarifLog>(writer.ToString());

            Assert.Single(sarif.Runs[0].Tool.Driver.Rules);
            Assert.Equal(3, sarif.Runs[0].Results.Length);
        }

        [Fact]
        public async Task WriteAsync_EmptyDiagnostics_ProducesValidSarif()
        {
            var formatter = new SarifFormatter();
            var result = new AnalysisResult
            {
                Diagnostics = new List<DiagnosticInfo>()
            };

            using var writer = new StringWriter();
            Console.SetOut(writer);

            await formatter.WriteAsync(result, null);

            var sarif = JsonSerializer.Deserialize<SarifLog>(writer.ToString());

            Assert.NotNull(sarif);
            Assert.Empty(sarif.Runs[0].Results);
            Assert.Empty(sarif.Runs[0].Tool.Driver.Rules);
        }
    }
}
```

---

## Validation

### Validate SARIF Output

Use the Microsoft SARIF SDK or online validators:

```bash
# Install SARIF tools
npm install -g @microsoft/sarif-multitool

# Validate SARIF file
sarif validate results.sarif

# Or use online validator
# https://sarifweb.azurewebsites.net/
```

### Common Validation Errors

| Error | Cause | Fix |
|-------|-------|-----|
| Invalid schema | Wrong schema URL | Use official OASIS URL |
| Missing required field | Null required property | Ensure all required fields set |
| Invalid level | Wrong severity string | Use error/warning/note/none |
| Invalid URI | Backslashes in path | Convert to forward slashes |

---

## Deliverable Checklist

- [ ] Create SARIF model classes in `Models/Sarif/`
- [ ] Implement `SarifFormatter.cs`
- [ ] Add rule metadata database
- [ ] Implement severity mapping
- [ ] Implement relative path conversion
- [ ] Add SARIF format to CLI options
- [ ] Write formatter tests (~5 tests)
- [ ] Validate output against SARIF schema
- [ ] Test with GitHub Actions
- [ ] Test with Azure DevOps
- [ ] Document CI/CD integration examples
