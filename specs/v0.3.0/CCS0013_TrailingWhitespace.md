# CCS0013: TrailingWhitespace

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0013 |
| Category | Style |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects trailing whitespace at the end of lines. Trailing whitespace is invisible, creates noise in diffs, and can cause issues with version control.

### Why This Rule?

1. **Clean Diffs**: Trailing whitespace changes show up as meaningless diff lines
2. **Consistency**: Maintains clean source files
3. **Version Control**: Prevents accidental "whitespace-only" commits
4. **Editor Compatibility**: Some editors/tools strip trailing whitespace automatically

---

## Compliant Examples

```csharp
// Good - no trailing whitespace
public class MyClass
{
    public void Method()
    {
        DoSomething();
    }
}
```

## Non-Compliant Examples

```csharp
// Bad - trailing spaces (shown as · for visibility)
public class MyClass··
{
    public void Method()····
    {
        DoSomething();·
    }··
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Style/
│       └── TrailingWhitespaceAnalyzer.cs
└── CodeFixes/
    └── Style/
        └── TrailingWhitespaceCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Style
{
    /// <summary>
    /// Analyzer that detects trailing whitespace at the end of lines.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0013
    /// Category: Style
    /// Severity: Info
    ///
    /// This analyzer reports a diagnostic when a line ends with
    /// whitespace (spaces or tabs).
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TrailingWhitespaceAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0013";

        private static readonly LocalizableString Title = "Trailing whitespace detected";
        private static readonly LocalizableString MessageFormat =
            "Remove trailing whitespace";
        private static readonly LocalizableString Description =
            "Lines should not end with trailing whitespace.";
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

            foreach (var line in text.Lines)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Skip empty lines
                if (line.Span.Length == 0)
                {
                    continue;
                }

                var lineText = text.ToString(line.Span);

                // Check if line ends with whitespace
                if (lineText.Length > 0 && char.IsWhiteSpace(lineText[lineText.Length - 1]))
                {
                    // Find the start of trailing whitespace
                    int trailingStart = lineText.Length - 1;
                    while (trailingStart > 0 && char.IsWhiteSpace(lineText[trailingStart - 1]))
                    {
                        trailingStart--;
                    }

                    // Create span for the trailing whitespace
                    var trailingSpan = new TextSpan(
                        line.Span.Start + trailingStart,
                        lineText.Length - trailingStart);

                    var location = Location.Create(context.Tree, trailingSpan);
                    var diagnostic = Diagnostic.Create(Rule, location);
                    context.ReportDiagnostic(diagnostic);
                }
            }
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
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Style
{
    /// <summary>
    /// Code fix provider for CCS0013 (TrailingWhitespace).
    /// Removes trailing whitespace from lines.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TrailingWhitespaceCodeFixProvider)), Shared]
    public class TrailingWhitespaceCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(TrailingWhitespaceAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove trailing whitespace",
                    createChangedDocument: c => RemoveTrailingWhitespaceAsync(document, diagnostic, c),
                    equivalenceKey: "RemoveTrailingWhitespace"),
                diagnostic);
        }

        private async Task<Document> RemoveTrailingWhitespaceAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // The diagnostic span is the trailing whitespace
            var span = diagnostic.Location.SourceSpan;

            // Remove the trailing whitespace
            var newText = text.Replace(span, string.Empty);

            return document.WithText(newText);
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Get all lines in the source file  │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ For each line:                    │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │ Is line empty │──────────► SKIP
          │ (length = 0)? │
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Does line end with whitespace?    │
│ (space, tab, etc.)                │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (valid)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Find start of trailing whitespace │
│ (walk backwards from end)         │
└─────────────────┬──────────────────┘
                  │
                  ▼
            REPORT CCS0013
     (span covers trailing whitespace)
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| SingleTrailingSpace | `int x = 1; ` | CCS0013 |
| MultipleTrailingSpaces | `int x = 1;   ` | CCS0013 |
| TrailingTab | `int x = 1;\t` | CCS0013 |
| TrailingMixed | `int x = 1; \t ` | CCS0013 |
| EmptyLineWithSpaces | `  ` (only spaces) | CCS0013 |
| MultipleLines | Multiple lines with trailing spaces | CCS0013 on each |
| AfterComment | `// comment ` | CCS0013 |
| AfterStringLiteral | `var s = "test"; ` | CCS0013 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| NoTrailingWhitespace | `int x = 1;` | No diagnostic |
| EmptyLine | `` (empty line) | No diagnostic |
| WhitespaceInString | `var s = "hello ";` | No diagnostic |
| IndentedCode | `    int x = 1;` | No diagnostic |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| RemoveSingleSpace | `int x = 1; ` | `int x = 1;` |
| RemoveMultipleSpaces | `int x = 1;   ` | `int x = 1;` |
| RemoveTab | `int x = 1;\t` | `int x = 1;` |
| RemoveMixed | `int x = 1; \t ` | `int x = 1;` |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Style.TrailingWhitespaceAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    CodeCop.Sharp.Analyzers.Style.TrailingWhitespaceAnalyzer,
    CodeCop.Sharp.CodeFixes.Style.TrailingWhitespaceCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Style
{
    public class TrailingWhitespaceAnalyzerTests
    {
        [Fact]
        public async Task TrailingSpace_ShouldTriggerDiagnostic()
        {
            // Note: The trailing space is significant here
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        int x = 1;{|#0: |}
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0013").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task NoTrailingWhitespace_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        int x = 1;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CodeFix_RemovesTrailingWhitespace()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        int x = 1;{|#0: |}
    }
}";

            var fixedCode = @"
public class MyClass
{
    public void Test()
    {
        int x = 1;
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0013").WithLocation(0);
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `"hello "` (string with space) | Skip | Whitespace inside string literal |
| `@"hello "` (verbatim string) | Skip | Whitespace inside string literal |
| `// comment ` | Report | Trailing whitespace after comment |
| Empty line | Skip | No content, no trailing whitespace |
| `   ` (whitespace-only line) | Report | Line has trailing whitespace |
| `\r\n` at end | Skip | Line endings are not trailing whitespace |
| Tab character | Report | Tabs are whitespace |

---

## Performance Considerations

1. **Line-by-Line Analysis**: Analyze each line independently for efficiency
2. **Early Exit**: Check line length before analyzing
3. **Text Span**: Report exact span of trailing whitespace for precise fixing
4. **Cancellation**: Support cancellation token for large files

---

## Deliverable Checklist

- [ ] Create `Analyzers/Style/TrailingWhitespaceAnalyzer.cs`
- [ ] Create `CodeFixes/Style/TrailingWhitespaceCodeFixProvider.cs`
- [ ] Implement line-by-line analysis
- [ ] Calculate exact span of trailing whitespace
- [ ] Write analyzer tests (~8 tests)
- [ ] Write code fix tests (~4 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
