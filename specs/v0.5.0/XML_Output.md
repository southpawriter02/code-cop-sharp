# XML Output Formatter

## Overview

XML output format for CI/CD tools that require XML-based reporting.

## XML Schema

```xml
<?xml version="1.0" encoding="utf-8"?>
<CodeCopReport version="0.5.0" timestamp="2024-01-15T10:30:00Z">
  <Target>./MySolution.sln</Target>
  <Duration>00:00:02.340</Duration>
  <Summary>
    <Total>6</Total>
    <Errors>2</Errors>
    <Warnings>3</Warnings>
    <Info>1</Info>
  </Summary>
  <Diagnostics>
    <Diagnostic>
      <Id>CCS0030</Id>
      <Severity>Warning</Severity>
      <Message>Async method 'LoadData' should end with 'Async' suffix</Message>
      <File>src/Services/DataService.cs</File>
      <Line>42</Line>
      <Column>20</Column>
      <Category>BestPractices</Category>
    </Diagnostic>
    <!-- More diagnostics -->
  </Diagnostics>
  <Rules>
    <Rule id="CCS0030" name="AsyncMethodNaming" category="BestPractices" />
    <!-- More rules -->
  </Rules>
</CodeCopReport>
```

## Implementation

```csharp
using System.Xml.Linq;
using CodeCop.CLI.Models;

namespace CodeCop.CLI.Formatters
{
    public class XmlFormatter : IOutputFormatter
    {
        public async Task WriteAsync(AnalysisResult result, string? outputPath)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("CodeCopReport",
                    new XAttribute("version", result.Version),
                    new XAttribute("timestamp", result.Timestamp.ToString("O")),
                    new XElement("Target", result.Target),
                    new XElement("Duration", result.Duration.ToString()),
                    new XElement("Summary",
                        new XElement("Total", result.TotalCount),
                        new XElement("Errors", result.ErrorCount),
                        new XElement("Warnings", result.WarningCount),
                        new XElement("Info", result.InfoCount)),
                    new XElement("Diagnostics",
                        result.Diagnostics.Select(d =>
                            new XElement("Diagnostic",
                                new XElement("Id", d.Id),
                                new XElement("Severity", d.Severity),
                                new XElement("Message", d.Message),
                                new XElement("File", d.File),
                                new XElement("Line", d.Line),
                                new XElement("Column", d.Column),
                                new XElement("Category", d.Category)))),
                    new XElement("Rules",
                        result.Diagnostics
                            .Select(d => d.Id)
                            .Distinct()
                            .Select(id =>
                                new XElement("Rule",
                                    new XAttribute("id", id),
                                    new XAttribute("name", GetRuleName(id)),
                                    new XAttribute("category", GetCategory(id)))))));

            var xml = doc.ToString();

            if (outputPath != null)
            {
                await File.WriteAllTextAsync(outputPath, xml);
            }
            else
            {
                Console.WriteLine(xml);
            }
        }

        private static string GetRuleName(string id) => id switch
        {
            "CCS0030" => "AsyncMethodNaming",
            "CCS0031" => "AvoidAsyncVoid",
            "CCS0032" => "ConfigureAwaitFalse",
            "CCS0033" => "PreferLinqMethod",
            "CCS0034" => "SimplifyLinq",
            "CCS0035" => "UseNullConditional",
            "CCS0036" => "UseNullCoalescing",
            _ => "Unknown"
        };

        private static string GetCategory(string id)
        {
            if (id.StartsWith("CCS003"))
                return "BestPractices";
            return "Unknown";
        }
    }
}
```

## CLI Integration

```bash
# Generate XML report
codecop analyze --target ./MySolution.sln --format Xml --output report.xml
```
