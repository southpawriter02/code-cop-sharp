# CCS0014: ConsistentNewlines

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0014 |
| Category | Style |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects inconsistent line endings (mixed CRLF and LF) within a single file. Files should use consistent line endings throughout to prevent version control issues and ensure cross-platform compatibility.

### Why This Rule?

1. **Version Control**: Mixed line endings cause spurious diffs
2. **Cross-Platform**: Different platforms have different default line endings
3. **Consistency**: Uniform files are easier to work with
4. **Tool Compatibility**: Some tools behave differently with mixed line endings

### Line Ending Types

| Type | Characters | Description |
|------|------------|-------------|
| CRLF | `\r\n` | Windows standard |
| LF | `\n` | Unix/Linux/macOS standard |
| CR | `\r` | Legacy Mac (rare) |

---

## Compliant Examples

```
// Good - all CRLF (Windows)
line1\r\n
line2\r\n
line3\r\n

// Good - all LF (Unix)
line1\n
line2\n
line3\n
```

## Non-Compliant Examples

```
// Bad - mixed CRLF and LF
line1\r\n      // CRLF
line2\n        // LF - CCS0014
line3\r\n      // CRLF
line4\n        // LF - CCS0014
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Style/
│       └── ConsistentNewlinesAnalyzer.cs
└── CodeFixes/
    └── Style/
        └── ConsistentNewlinesCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Style
{
    /// <summary>
    /// Analyzer that detects inconsistent line endings (mixed CRLF/LF).
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0014
    /// Category: Style
    /// Severity: Info
    ///
    /// This analyzer reports a diagnostic when a file contains
    /// both CRLF and LF line endings.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConsistentNewlinesAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0014";

        private static readonly LocalizableString Title = "Inconsistent line endings";
        private static readonly LocalizableString MessageFormat =
            "Line uses {0} but file predominantly uses {1}";
        private static readonly LocalizableString Description =
            "All lines in a file should use the same line ending style (CRLF or LF).";
        private const string Category = "Style";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var text = context.Tree.GetText(context.CancellationToken);
            var sourceText = text.ToString();

            // Find all line endings
            var lineEndings = FindLineEndings(sourceText);

            if (lineEndings.Count == 0)
            {
                return; // No line endings (single line file)
            }

            // Count CRLF vs LF
            int crlfCount = 0;
            int lfCount = 0;

            foreach (var (position, isCrlf) in lineEndings)
            {
                if (isCrlf)
                    crlfCount++;
                else
                    lfCount++;
            }

            // If all same type, no problem
            if (crlfCount == 0 || lfCount == 0)
            {
                return;
            }

            // Determine dominant style
            bool dominantIsCrlf = crlfCount >= lfCount;
            string dominantStyle = dominantIsCrlf ? "CRLF" : "LF";
            string minorityStyle = dominantIsCrlf ? "LF" : "CRLF";

            // Report on minority line endings
            foreach (var (position, isCrlf) in lineEndings)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (isCrlf != dominantIsCrlf)
                {
                    // Create span for the line ending
                    var span = new TextSpan(position, isCrlf ? 2 : 1);
                    var location = Location.Create(context.Tree, span);

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        location,
                        minorityStyle,
                        dominantStyle);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Finds all line endings in the source text.
        /// Returns a list of (position, isCrlf) tuples.
        /// </summary>
        private List<(int position, bool isCrlf)> FindLineEndings(string text)
        {
            var result = new List<(int, bool)>();

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    // Check if CRLF
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        result.Add((i, true)); // CRLF
                        i++; // Skip the \n
                    }
                    else
                    {
                        // Standalone \r (legacy Mac, treat as LF equivalent)
                        result.Add((i, false));
                    }
                }
                else if (text[i] == '\n')
                {
                    result.Add((i, false)); // LF
                }
            }

            return result;
        }
    }
}
```

### Code Fix Provider Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Style
{
    /// <summary>
    /// Code fix provider for CCS0014 (ConsistentNewlines).
    /// Normalizes line endings to the dominant style.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConsistentNewlinesCodeFixProvider)), Shared]
    public class ConsistentNewlinesCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ConsistentNewlinesAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            // Parse the message to determine what to convert to
            var message = diagnostic.GetMessage();
            var targetStyle = message.Contains("CRLF") && message.EndsWith("CRLF") ? "CRLF" : "LF";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Convert to {targetStyle}",
                    createChangedDocument: c => NormalizeLineEndingsAsync(document, diagnostic, c),
                    equivalenceKey: "NormalizeLineEndings"),
                diagnostic);
        }

        private async Task<Document> NormalizeLineEndingsAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = text.ToString();

            // Determine target line ending from the diagnostic
            var message = diagnostic.GetMessage();
            var targetIsCrlf = message.EndsWith("CRLF");
            var targetLineEnding = targetIsCrlf ? "\r\n" : "\n";

            // Normalize all line endings
            var normalized = NormalizeAllLineEndings(sourceText, targetLineEnding);

            var newText = SourceText.From(normalized, text.Encoding);
            return document.WithText(newText);
        }

        /// <summary>
        /// Normalizes all line endings in the text to the target style.
        /// </summary>
        private string NormalizeAllLineEndings(string text, string targetLineEnding)
        {
            var result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\r')
                {
                    // CRLF or standalone CR
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        result.Append(targetLineEnding);
                        i++; // Skip the \n
                    }
                    else
                    {
                        result.Append(targetLineEnding);
                    }
                }
                else if (text[i] == '\n')
                {
                    result.Append(targetLineEnding);
                }
                else
                {
                    result.Append(text[i]);
                }
            }

            return result.ToString();
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Scan file for all line endings    │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Count CRLF vs LF occurrences      │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │ Only one type │──────────► SKIP (consistent)
          │ (CRLF=0 or   │
          │  LF=0)?       │
          └───────┬───────┘
                  │ MIXED
                  ▼
┌────────────────────────────────────┐
│ Determine dominant style:         │
│ - CRLF if CRLF >= LF              │
│ - LF if LF > CRLF                 │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Report CCS0014 on each            │
│ minority-style line ending        │
└────────────────────────────────────┘
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| MixedEndings_CRLFDominant | 3 CRLF + 1 LF | CCS0014 on LF line |
| MixedEndings_LFDominant | 1 CRLF + 3 LF | CCS0014 on CRLF line |
| MixedEndings_Equal | 2 CRLF + 2 LF | CCS0014 on LF lines (CRLF wins ties) |
| MixedEndings_Multiple | Multiple minority lines | CCS0014 on each |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| AllCRLF | All lines use CRLF | No diagnostic |
| AllLF | All lines use LF | No diagnostic |
| SingleLine | No line endings | No diagnostic |
| EmptyFile | Empty file | No diagnostic |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| NormalizeToCRLF | Mixed with CRLF dominant | All CRLF |
| NormalizeToLF | Mixed with LF dominant | All LF |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Style.ConsistentNewlinesAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    CodeCop.Sharp.Analyzers.Style.ConsistentNewlinesAnalyzer,
    CodeCop.Sharp.CodeFixes.Style.ConsistentNewlinesCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Style
{
    public class ConsistentNewlinesAnalyzerTests
    {
        [Fact]
        public async Task MixedEndings_CRLFDominant_ShouldTriggerDiagnostic()
        {
            // Line 1: CRLF, Line 2: CRLF, Line 3: LF (minority)
            var testCode = "public class A\r\n{\r\n    int x;\n}\r\n";

            // The LF on line 3 should trigger the diagnostic
            var expected = VerifyCS.Diagnostic("CCS0014")
                .WithSpan(3, 11, 3, 12)
                .WithArguments("LF", "CRLF");

            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task AllCRLF_ShouldNotTriggerDiagnostic()
        {
            var testCode = "public class A\r\n{\r\n    int x;\r\n}\r\n";
            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task AllLF_ShouldNotTriggerDiagnostic()
        {
            var testCode = "public class A\n{\n    int x;\n}\n";
            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| All CRLF | Skip | Consistent |
| All LF | Skip | Consistent |
| Single line (no endings) | Skip | Nothing to check |
| `\r` alone (legacy Mac) | Treat as LF equivalent | Rare, but handle gracefully |
| Empty file | Skip | No content |
| 50/50 split | CRLF wins ties | Windows is more common, arbitrary but consistent |

---

## Performance Considerations

1. **Single Pass**: Scan file once to find all line endings
2. **Memory Efficient**: Store only positions, not full lines
3. **Cancellation**: Support cancellation token for large files
4. **Fix All**: BatchFixer handles multiple diagnostics efficiently

---

## Configuration (Future)

```ini
# .editorconfig
[*.cs]
# CCS0014: Consistent newlines
end_of_line = crlf  # or 'lf' - could influence dominant style detection

# Disable rule
dotnet_diagnostic.CCS0014.severity = none
```

---

## Deliverable Checklist

- [ ] Create `Analyzers/Style/ConsistentNewlinesAnalyzer.cs`
- [ ] Create `CodeFixes/Style/ConsistentNewlinesCodeFixProvider.cs`
- [ ] Implement line ending detection
- [ ] Implement dominant style detection
- [ ] Write analyzer tests (~6 tests)
- [ ] Write code fix tests (~3 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
