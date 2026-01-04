# CCS0010: BracesRequired

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0010 |
| Category | Style |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Single-line control statements (`if`, `else`, `for`, `foreach`, `while`, `using`, `lock`) should always use braces, even for single statements. This prevents bugs when adding code and improves readability.

### Why This Rule?

1. **Bug Prevention**: Adding a second statement to a brace-less block is a common source of bugs
2. **Readability**: Braces make the scope of control statements explicit
3. **Maintainability**: Easier to add/remove statements without refactoring
4. **Consistency**: Uniform style across the codebase

---

## Compliant Examples

```csharp
// Good - braces used
if (condition)
{
    DoSomething();
}

foreach (var item in items)
{
    Process(item);
}

while (hasMore)
{
    ReadNext();
}

// Good - else-if pattern (no braces needed on else)
if (a)
{
    DoA();
}
else if (b)
{
    DoB();
}
else
{
    DoDefault();
}

// Good - nested using (common pattern)
using (var a = GetA())
using (var b = GetB())
{
    Process(a, b);
}
```

## Non-Compliant Examples

```csharp
// Bad - no braces on if
if (condition)
    DoSomething();    // CCS0010

// Bad - no braces on else
if (condition)
{
    DoA();
}
else
    DoB();            // CCS0010

// Bad - no braces on foreach
foreach (var item in items)
    Process(item);    // CCS0010

// Bad - no braces on while
while (hasMore)
    ReadNext();       // CCS0010

// Bad - no braces on for
for (int i = 0; i < 10; i++)
    Process(i);       // CCS0010

// Bad - no braces on lock
lock (syncObj)
    DoWork();         // CCS0010
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Style/
│       └── BracesRequiredAnalyzer.cs
└── CodeFixes/
    └── Style/
        └── BracesRequiredCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Style
{
    /// <summary>
    /// Analyzer that enforces braces on control statements.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0010
    /// Category: Style
    /// Severity: Warning
    ///
    /// This analyzer reports a diagnostic when control statements
    /// (if, else, for, foreach, while, using, lock) don't use braces.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BracesRequiredAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0010";

        private static readonly LocalizableString Title = "Control statement should use braces";
        private static readonly LocalizableString MessageFormat = "{0} statement should use braces";
        private static readonly LocalizableString Description =
            "Single-line control statements should always use braces for clarity and to prevent bugs.";
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

            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(AnalyzeForStatement, SyntaxKind.ForStatement);
            context.RegisterSyntaxNodeAction(AnalyzeForEachStatement, SyntaxKind.ForEachStatement);
            context.RegisterSyntaxNodeAction(AnalyzeWhileStatement, SyntaxKind.WhileStatement);
            context.RegisterSyntaxNodeAction(AnalyzeUsingStatement, SyntaxKind.UsingStatement);
            context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
        }

        private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            // Check the 'if' body
            if (ifStatement.Statement is not BlockSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, ifStatement.IfKeyword.GetLocation(), "if");
                context.ReportDiagnostic(diagnostic);
            }

            // Check the 'else' body (but allow else-if pattern)
            if (ifStatement.Else != null &&
                ifStatement.Else.Statement is not BlockSyntax &&
                ifStatement.Else.Statement is not IfStatementSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, ifStatement.Else.ElseKeyword.GetLocation(), "else");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeForStatement(SyntaxNodeAnalysisContext context)
        {
            var forStatement = (ForStatementSyntax)context.Node;
            if (forStatement.Statement is not BlockSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, forStatement.ForKeyword.GetLocation(), "for");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeForEachStatement(SyntaxNodeAnalysisContext context)
        {
            var foreachStatement = (ForEachStatementSyntax)context.Node;
            if (foreachStatement.Statement is not BlockSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, foreachStatement.ForEachKeyword.GetLocation(), "foreach");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeWhileStatement(SyntaxNodeAnalysisContext context)
        {
            var whileStatement = (WhileStatementSyntax)context.Node;
            if (whileStatement.Statement is not BlockSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, whileStatement.WhileKeyword.GetLocation(), "while");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeUsingStatement(SyntaxNodeAnalysisContext context)
        {
            var usingStatement = (UsingStatementSyntax)context.Node;

            // Allow nested using statements without braces (common pattern)
            if (usingStatement.Statement is UsingStatementSyntax)
            {
                return;
            }

            if (usingStatement.Statement is not BlockSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, usingStatement.UsingKeyword.GetLocation(), "using");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatement = (LockStatementSyntax)context.Node;
            if (lockStatement.Statement is not BlockSyntax)
            {
                var diagnostic = Diagnostic.Create(Rule, lockStatement.LockKeyword.GetLocation(), "lock");
                context.ReportDiagnostic(diagnostic);
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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Style
{
    /// <summary>
    /// Code fix provider for CCS0010 (BracesRequired).
    /// Adds braces around single-line statements.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BracesRequiredCodeFixProvider)), Shared]
    public class BracesRequiredCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(BracesRequiredAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);
            var statement = token.Parent?.AncestorsAndSelf()
                .FirstOrDefault(n => n is IfStatementSyntax || n is ForStatementSyntax ||
                                      n is ForEachStatementSyntax || n is WhileStatementSyntax ||
                                      n is UsingStatementSyntax || n is LockStatementSyntax ||
                                      n is ElseClauseSyntax);

            if (statement == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add braces",
                    createChangedDocument: c => AddBracesAsync(context.Document, statement, c),
                    equivalenceKey: "AddBraces"),
                diagnostic);
        }

        private async Task<Document> AddBracesAsync(
            Document document,
            SyntaxNode statement,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            StatementSyntax? bodyStatement = statement switch
            {
                IfStatementSyntax ifs => ifs.Statement,
                ForStatementSyntax fors => fors.Statement,
                ForEachStatementSyntax foreachs => foreachs.Statement,
                WhileStatementSyntax whiles => whiles.Statement,
                UsingStatementSyntax usings => usings.Statement,
                LockStatementSyntax locks => locks.Statement,
                ElseClauseSyntax elses => elses.Statement,
                _ => null
            };

            if (bodyStatement == null || bodyStatement is BlockSyntax)
                return document;

            var block = SyntaxFactory.Block(bodyStatement)
                .WithLeadingTrivia(bodyStatement.GetLeadingTrivia())
                .WithTrailingTrivia(bodyStatement.GetTrailingTrivia());

            SyntaxNode newStatement = statement switch
            {
                IfStatementSyntax ifs => ifs.WithStatement(block),
                ForStatementSyntax fors => fors.WithStatement(block),
                ForEachStatementSyntax foreachs => foreachs.WithStatement(block),
                WhileStatementSyntax whiles => whiles.WithStatement(block),
                UsingStatementSyntax usings => usings.WithStatement(block),
                LockStatementSyntax locks => locks.WithStatement(block),
                ElseClauseSyntax elses => elses.WithStatement(block),
                _ => statement
            };

            var newRoot = root.ReplaceNode(statement, newStatement);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is it a control statement?         │
│ (if/else/for/foreach/while/using/  │
│  lock)                             │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does the statement body have       │
│ braces (BlockSyntax)?              │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (valid)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Special case: else-if pattern?     │
│ (else followed by if statement)    │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (valid pattern)
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Special case: nested using?        │
│ (using followed by using)          │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (valid pattern)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0010
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| If_NoBraces | `if (x) y();` | CCS0010 at 'if' |
| Else_NoBraces | `if (x) { } else y();` | CCS0010 at 'else' |
| For_NoBraces | `for (;;) x();` | CCS0010 at 'for' |
| ForEach_NoBraces | `foreach (var x in y) z();` | CCS0010 at 'foreach' |
| While_NoBraces | `while (x) y();` | CCS0010 at 'while' |
| Using_NoBraces | `using (var x = y) z();` | CCS0010 at 'using' |
| Lock_NoBraces | `lock (x) y();` | CCS0010 at 'lock' |
| DoWhile_NoBraces | `do x(); while (y);` | CCS0010 at 'do' |
| NestedIf_NoBraces | `if (a) if (b) x();` | CCS0010 on both |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| If_WithBraces | `if (x) { y(); }` | No diagnostic |
| Else_WithBraces | `if (x) { } else { y(); }` | No diagnostic |
| ElseIf_Pattern | `if (x) { } else if (y) { }` | No diagnostic |
| For_WithBraces | `for (;;) { x(); }` | No diagnostic |
| ForEach_WithBraces | `foreach (var x in y) { z(); }` | No diagnostic |
| While_WithBraces | `while (x) { y(); }` | No diagnostic |
| Using_Nested | `using (a) using (b) { }` | No diagnostic |
| Lock_WithBraces | `lock (x) { y(); }` | No diagnostic |
| EmptyBlock | `if (x) { }` | No diagnostic |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|-------|
| If_AddBraces | `if (x) y();` | `if (x) { y(); }` |
| Else_AddBraces | `if (x) { } else y();` | `if (x) { } else { y(); }` |
| For_AddBraces | `for (;;) x();` | `for (;;) { x(); }` |
| ForEach_AddBraces | `foreach (var x in y) z();` | `foreach (var x in y) { z(); }` |
| While_AddBraces | `while (x) y();` | `while (x) { y(); }` |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Style.BracesRequiredAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    CodeCop.Sharp.Analyzers.Style.BracesRequiredAnalyzer,
    CodeCop.Sharp.CodeFixes.Style.BracesRequiredCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Style
{
    public class BracesRequiredAnalyzerTests
    {
        #region Analyzer Tests - Should Trigger

        [Fact]
        public async Task If_NoBraces_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        {|#0:if|} (true)
            DoSomething();
    }
    private void DoSomething() { }
}";

            var expected = VerifyCS.Diagnostic("CCS0010").WithLocation(0).WithArguments("if");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task If_WithBraces_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        if (true)
        {
            DoSomething();
        }
    }
    private void DoSomething() { }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        #endregion

        #region Code Fix Tests

        [Fact]
        public async Task CodeFix_AddsBlockToIf()
        {
            var testCode = @"
public class MyClass
{
    public void Test()
    {
        {|#0:if|} (true)
            DoSomething();
    }
    private void DoSomething() { }
}";

            var fixedCode = @"
public class MyClass
{
    public void Test()
    {
        if (true)
        {
            DoSomething();
        }
    }
    private void DoSomething() { }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0010").WithLocation(0).WithArguments("if");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        #endregion
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| `if (x) { }` | Skip | Has braces (even if empty) |
| `if (x) { y(); z(); }` | Skip | Has braces (multiple statements) |
| `else if (y) { }` | Skip | Else-if pattern is valid |
| `using (a) using (b) { }` | Skip | Nested using pattern is common |
| `if (x) if (y) z();` | Report both | Nested ifs both need braces |
| `do x(); while (y);` | Report | Do-while also needs braces |

---

## Deliverable Checklist

- [ ] Create `Analyzers/Style/BracesRequiredAnalyzer.cs`
- [ ] Create `CodeFixes/Style/BracesRequiredCodeFixProvider.cs`
- [ ] Handle all 7 control statements (if, else, for, foreach, while, using, lock)
- [ ] Handle else-if pattern exception
- [ ] Handle nested using exception
- [ ] Write analyzer tests (~12 tests)
- [ ] Write code fix tests (~5 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
