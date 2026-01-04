# CCS0012: SingleLineStatements

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0012 |
| Category | Style |
| Severity | Warning |
| Has Code Fix | No |
| Enabled by Default | Yes |

## Description

Detects multiple statements on a single line. Each statement should be on its own line for readability, easier debugging, and cleaner diffs.

### Why This Rule?

1. **Readability**: One statement per line is easier to scan
2. **Debugging**: Breakpoints target entire lines
3. **Diffs**: Single-line changes are clearer in version control
4. **Maintainability**: Easier to add/remove statements

---

## Compliant Examples

```csharp
// Good - one statement per line
int x = 1;
int y = 2;

if (condition)
{
    x++;
    y++;
}

for (int i = 0; i < 10; i++)
{
    Process(i);
    Log(i);
}
```

## Non-Compliant Examples

```csharp
// Bad - multiple statements on one line
int x = 1; int y = 2;                    // CCS0012

// Bad - multiple statements in block on one line
if (condition) { x++; y++; }             // CCS0012 (on y++)

// Bad - chained statements
a = 1; b = 2; c = 3;                     // CCS0012 (on b and c)

// Bad - statement after closing brace
if (x) { DoA(); } DoB();                 // CCS0012 (on DoB())
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
└── Analyzers/
    └── Style/
        └── SingleLineStatementsAnalyzer.cs
```

**Note**: No code fix provider for this rule. Manual refactoring is required because automatic reformatting could break intended formatting in some edge cases.

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.Style
{
    /// <summary>
    /// Analyzer that detects multiple statements on a single line.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0012
    /// Category: Style
    /// Severity: Warning
    ///
    /// This analyzer reports a diagnostic when multiple statements
    /// appear on the same line.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SingleLineStatementsAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0012";

        private static readonly LocalizableString Title = "Multiple statements on single line";
        private static readonly LocalizableString MessageFormat =
            "Move this statement to a new line";
        private static readonly LocalizableString Description =
            "Each statement should be on its own line for readability.";
        private const string Category = "Style";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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

            // Analyze at syntax tree level to find all statements
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);

            // Get all statements in the tree
            var statements = root.DescendantNodes()
                .OfType<StatementSyntax>()
                .Where(s => !IsExcludedStatement(s))
                .ToList();

            // Group statements by their starting line
            var statementsByLine = new Dictionary<int, List<StatementSyntax>>();

            foreach (var statement in statements)
            {
                var lineSpan = statement.GetLocation().GetLineSpan();
                var startLine = lineSpan.StartLinePosition.Line;

                if (!statementsByLine.ContainsKey(startLine))
                {
                    statementsByLine[startLine] = new List<StatementSyntax>();
                }

                statementsByLine[startLine].Add(statement);
            }

            // Report diagnostics for lines with multiple statements
            foreach (var kvp in statementsByLine)
            {
                var statementsOnLine = kvp.Value;

                // Skip if there's only one statement on this line
                if (statementsOnLine.Count <= 1)
                {
                    continue;
                }

                // Filter out nested statements (e.g., if body inside if)
                var topLevelStatements = FilterToTopLevel(statementsOnLine);

                if (topLevelStatements.Count <= 1)
                {
                    continue;
                }

                // Report on all statements except the first one
                foreach (var statement in topLevelStatements.Skip(1))
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        statement.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Determines if a statement should be excluded from analysis.
        /// </summary>
        private bool IsExcludedStatement(StatementSyntax statement)
        {
            // Exclude block statements (they contain other statements)
            if (statement is BlockSyntax)
                return true;

            // Exclude empty statements (just semicolons)
            if (statement is EmptyStatementSyntax)
                return true;

            return false;
        }

        /// <summary>
        /// Filters statements to only include top-level ones
        /// (excludes statements that are nested inside another statement on the list).
        /// </summary>
        private List<StatementSyntax> FilterToTopLevel(List<StatementSyntax> statements)
        {
            var result = new List<StatementSyntax>();

            foreach (var statement in statements)
            {
                // Check if this statement is contained within any other statement in the list
                var isNested = statements.Any(other =>
                    other != statement &&
                    other.Span.Contains(statement.Span));

                if (!isNested)
                {
                    result.Add(statement);
                }
            }

            return result;
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Get all statements in syntax tree  │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Exclude BlockSyntax and           │
│ EmptyStatementSyntax              │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Group statements by start line    │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ For each line:                    │
│ - Count non-nested statements     │
│ - If count > 1, report on 2nd+    │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ Report CCS0012 on each            │
│ additional statement (not first)  │
└────────────────────────────────────┘
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| TwoStatements_SameLine | `int x = 1; int y = 2;` | CCS0012 on `int y` |
| ThreeStatements_SameLine | `a = 1; b = 2; c = 3;` | CCS0012 on `b` and `c` |
| StatementAfterBlock | `if (x) { } y = 1;` | CCS0012 on `y = 1` |
| StatementsInSingleLineBlock | `{ x = 1; y = 2; }` | CCS0012 on `y = 2` |
| MethodCallsOnSameLine | `DoA(); DoB();` | CCS0012 on `DoB()` |
| MixedStatements | `int x; x = 1;` | CCS0012 on `x = 1` |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| SingleStatement | `int x = 1;` | No diagnostic |
| StatementsOnSeparateLines | `int x = 1;\nint y = 2;` | No diagnostic |
| ForLoopHeader | `for (int i = 0; i < 10; i++)` | No diagnostic (not statements) |
| EmptyStatements | `;;` | No diagnostic (empty excluded) |
| BlockWithStatements | `{ x = 1; }` | No diagnostic (single statement in block) |
| LambdaExpression | `items.Select(x => x.Value);` | No diagnostic (expression, not statement) |
| MultiLineStatement | `var x = GetValue()\n    .Process();` | No diagnostic (single statement) |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Style.SingleLineStatementsAnalyzer>;

namespace CodeCop.Sharp.Tests.Analyzers.Style
{
    public class SingleLineStatementsAnalyzerTests
    {
        [Fact]
        public async Task TwoStatements_SameLine_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        int x = 1; {|#0:int y = 2;|}
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0012").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ThreeStatements_SameLine_ShouldTriggerTwoDiagnostics()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        int a = 1; {|#0:int b = 2;|} {|#1:int c = 3;|}
    }
}";

            var expected1 = VerifyCS.Diagnostic("CCS0012").WithLocation(0);
            var expected2 = VerifyCS.Diagnostic("CCS0012").WithLocation(1);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected1, expected2);
        }

        [Fact]
        public async Task StatementsOnSeparateLines_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        int x = 1;
        int y = 2;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ForLoopHeader_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        for (int i = 0; i < 10; i++)
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task SingleStatementInBlock_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        if (true) { int x = 1; }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `for (int i = 0; i < 10; i++)` | Skip | For loop header is not multiple statements |
| `{ x = 1; }` | Skip | Single statement in block |
| `{ x = 1; y = 2; }` | Report on `y` | Multiple statements in single-line block |
| `using (a) using (b) { }` | Skip | Nested using is a pattern, not multiple statements |
| `;;` | Skip | Empty statements are excluded |
| `if (x) y++;` | Skip | Single-line if (handled by CCS0010) |
| `x = y = z = 1;` | Skip | Single statement (chained assignment) |

---

## Why No Code Fix?

This analyzer does not provide an automatic code fix for several reasons:

1. **Formatting Complexity**: Automatic line breaks can disrupt intentional formatting
2. **Indentation**: Determining correct indentation requires broader context
3. **IDE Integration**: Most IDEs have "Format Document" features that handle this better
4. **Manual Review**: Encourages developers to think about statement organization

Users should use their IDE's formatting feature or manually split statements.

---

## Deliverable Checklist

- [ ] Create `Analyzers/Style/SingleLineStatementsAnalyzer.cs`
- [ ] Implement statement grouping by line
- [ ] Handle nested statement filtering
- [ ] Exclude block and empty statements
- [ ] Write analyzer tests (~8 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
