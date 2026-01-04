# CCS0022: EmptyCatchBlock

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0022 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects empty catch blocks that silently swallow exceptions. Empty catch blocks hide errors and make debugging difficult. If exceptions should be intentionally ignored, a comment explaining the reason is required.

### Why This Rule?

1. **Silent Failures**: Empty catches hide errors completely
2. **Debugging Difficulty**: Hard to trace issues when exceptions are swallowed
3. **Hidden Bugs**: Masks problems that should be addressed
4. **Intentional Documentation**: If intentional, a comment explains why
5. **Best Practice**: Never silently swallow exceptions without documentation

### What Counts as "Empty"?

A catch block is considered empty if it has:
- No statements at all
- Only whitespace/newlines
- No comments explaining why it's empty

A catch block is **NOT** empty if it has:
- Any executable statement
- A comment (single-line `//` or multi-line `/* */`)
- A `throw` or `throw;` statement

---

## Compliant Examples

```csharp
// Good - exception is logged
try
{
    DoSomething();
}
catch (SpecificException ex)
{
    _logger.LogError(ex, "Operation failed");
}

// Good - exception is rethrown after logging
try
{
    Process();
}
catch (Exception ex)
{
    LogError(ex);
    throw;
}

// Good - intentional ignore with explanatory comment
try
{
    TryCleanup();
}
catch (IOException)
{
    // Cleanup failures are non-critical - file may already be deleted
}

// Good - multi-line comment explanation
try
{
    OptionalOperation();
}
catch
{
    /* This operation is optional and failures should not
       prevent the main workflow from completing. */
}

// Good - has statement (even if minimal)
try
{
    DoWork();
}
catch (Exception)
{
    Debug.WriteLine("Operation failed");
}
```

## Non-Compliant Examples

```csharp
// Bad - completely empty catch
try
{
    DoSomething();
}
catch (Exception ex)
{
}                        // CCS0022

// Bad - catch-all with no handling
try
{
    Process();
}
catch
{
}                        // CCS0022

// Bad - only whitespace/newlines
try
{
    Load();
}
catch (IOException)
{

}                        // CCS0022

// Bad - multiple empty catches
try
{
    DoWork();
}
catch (ArgumentException)
{
}                        // CCS0022
catch (InvalidOperationException)
{
}                        // CCS0022
catch
{
}                        // CCS0022
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Quality/
│       └── EmptyCatchBlockAnalyzer.cs
└── CodeFixes/
    └── Quality/
        └── EmptyCatchBlockCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.Quality
{
    /// <summary>
    /// Analyzer that detects empty catch blocks without comments.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0022
    /// Category: Quality
    /// Severity: Warning
    ///
    /// This analyzer reports catch blocks that have no statements
    /// and no comments explaining why they're empty.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0022";

        private static readonly LocalizableString Title = "Empty catch block";
        private static readonly LocalizableString MessageFormat =
            "Empty catch block hides exceptions. Add handling or a comment explaining why it's empty.";
        private static readonly LocalizableString Description =
            "Catch blocks should not be empty. Either handle the exception or add a comment explaining why it's safe to ignore.";
        private const string Category = "Quality";

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

            context.RegisterSyntaxNodeAction(AnalyzeCatchClause, SyntaxKind.CatchClause);
        }

        private void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
        {
            var catchClause = (CatchClauseSyntax)context.Node;
            var block = catchClause.Block;

            if (block == null)
                return;

            // Check if block has any statements
            if (block.Statements.Any())
                return;

            // Check for comments inside the block
            if (HasCommentInside(block))
                return;

            var diagnostic = Diagnostic.Create(
                Rule,
                catchClause.CatchKeyword.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Checks if the block contains any comments.
        /// </summary>
        private static bool HasCommentInside(BlockSyntax block)
        {
            // Check all trivia in descendant tokens
            var allTrivia = block.DescendantTrivia();

            foreach (var trivia in allTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                {
                    return true;
                }
            }

            // Also check trivia attached to the braces
            var braceTrivia = block.OpenBraceToken.TrailingTrivia
                .Concat(block.CloseBraceToken.LeadingTrivia);

            foreach (var trivia in braceTrivia)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                    trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    return true;
                }
            }

            return false;
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

namespace CodeCop.Sharp.CodeFixes.Quality
{
    /// <summary>
    /// Code fix provider for CCS0022 (EmptyCatchBlock).
    /// Adds a TODO comment to the empty catch block.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyCatchBlockCodeFixProvider)), Shared]
    public class EmptyCatchBlockCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(EmptyCatchBlockAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var catchKeyword = root.FindToken(diagnosticSpan.Start);
            var catchClause = catchKeyword.Parent as CatchClauseSyntax;

            if (catchClause == null)
                return;

            // Option 1: Add TODO comment
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add TODO comment",
                    createChangedDocument: c => AddTodoCommentAsync(context.Document, catchClause, c),
                    equivalenceKey: "AddTodoComment"),
                diagnostic);

            // Option 2: Add throw statement
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Rethrow exception",
                    createChangedDocument: c => AddThrowStatementAsync(context.Document, catchClause, c),
                    equivalenceKey: "AddThrow"),
                diagnostic);
        }

        private async Task<Document> AddTodoCommentAsync(
            Document document,
            CatchClauseSyntax catchClause,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Determine indentation from the catch keyword
            var catchLeadingTrivia = catchClause.CatchKeyword.LeadingTrivia;
            var indentation = "";
            foreach (var trivia in catchLeadingTrivia)
            {
                if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    indentation = trivia.ToString();
                    break;
                }
            }

            // Create comment with proper indentation
            var comment = SyntaxFactory.Comment("// TODO: Handle exception or document why it's safe to ignore");
            var newTrivia = SyntaxFactory.TriviaList(
                SyntaxFactory.EndOfLine("\n"),
                SyntaxFactory.Whitespace(indentation + "    "),
                comment,
                SyntaxFactory.EndOfLine("\n"),
                SyntaxFactory.Whitespace(indentation)
            );

            var newBlock = catchClause.Block.WithOpenBraceToken(
                catchClause.Block.OpenBraceToken.WithTrailingTrivia(newTrivia));

            var newCatch = catchClause.WithBlock(newBlock);
            var newRoot = root.ReplaceNode(catchClause, newCatch);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddThrowStatementAsync(
            Document document,
            CatchClauseSyntax catchClause,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Create throw; statement
            var throwStatement = SyntaxFactory.ThrowStatement()
                .WithLeadingTrivia(SyntaxFactory.Whitespace("    "))
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

            var newBlock = catchClause.Block.WithStatements(
                SyntaxFactory.SingletonList<StatementSyntax>(throwStatement));

            var newCatch = catchClause.WithBlock(newBlock);
            var newRoot = root.ReplaceNode(catchClause, newCatch);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is this a catch clause?            │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does the catch have a block?       │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (syntax error)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Does the block have any statements?│
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (has handling code)
          └───────┬───────┘
                  │ NO (empty)
                  ▼
┌────────────────────────────────────┐
│ Does the block contain any         │
│ comments (// or /* */)?            │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (developer explained)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0022
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| EmptyCatch | `catch (Exception ex) { }` | CCS0022 |
| CatchAllEmpty | `catch { }` | CCS0022 |
| WhitespaceOnly | `catch { \n \n }` | CCS0022 |
| SpecificExceptionEmpty | `catch (IOException) { }` | CCS0022 |
| MultipleEmpty | Multiple empty catches in same try | CCS0022 x each |
| NewlinesOnly | `catch {\n\n\n}` | CCS0022 |
| TabsOnly | `catch {\t\t}` | CCS0022 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| HasStatement | `catch { Log(); }` | No diagnostic |
| HasThrow | `catch { throw; }` | No diagnostic |
| HasThrowNew | `catch (Exception ex) { throw new Exception("msg", ex); }` | No diagnostic |
| HasSingleLineComment | `catch { // ignore }` | No diagnostic |
| HasMultiLineComment | `catch { /* reason */ }` | No diagnostic |
| HasAssignment | `catch { _failed = true; }` | No diagnostic |
| HasReturn | `catch { return; }` | No diagnostic |
| HasBreak | `catch { break; }` (in loop) | No diagnostic |

### Code Fix Tests

| Test Name | Before | After (TODO) | After (Throw) |
|-----------|--------|--------------|---------------|
| AddTodoComment | `catch { }` | `catch { // TODO: ... }` | - |
| AddThrowStatement | `catch { }` | - | `catch { throw; }` |
| PreservesExceptionVariable | `catch (Exception ex) { }` | `catch (Exception ex) { // TODO: ... }` | `catch (Exception ex) { throw; }` |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.EmptyCatchBlockAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    CodeCop.Sharp.Analyzers.Quality.EmptyCatchBlockAnalyzer,
    CodeCop.Sharp.CodeFixes.Quality.EmptyCatchBlockCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class EmptyCatchBlockAnalyzerTests
    {
        [Fact]
        public async Task EmptyCatch_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        {|#0:catch|} (System.Exception ex)
        {
        }
    }

    private void DoSomething() { }
}";

            var expected = VerifyCS.Diagnostic("CCS0022").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task CatchAllEmpty_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        {|#0:catch|}
        {
        }
    }

    private void DoSomething() { }
}";

            var expected = VerifyCS.Diagnostic("CCS0022").WithLocation(0);
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task CatchWithStatement_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

    private void DoSomething() { }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CatchWithThrow_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        catch
        {
            throw;
        }
    }

    private void DoSomething() { }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CatchWithComment_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        catch
        {
            // Intentionally ignored - cleanup operation
        }
    }

    private void DoSomething() { }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CatchWithMultiLineComment_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        catch
        {
            /* This operation is optional and
               failures don't affect the main flow */
        }
    }

    private void DoSomething() { }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CodeFix_AddsTodoComment()
        {
            var testCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        {|#0:catch|}
        {
        }
    }

    private void DoSomething() { }
}";

            var fixedCode = @"
public class MyClass
{
    public void Method()
    {
        try
        {
            DoSomething();
        }
        catch
        {
            // TODO: Handle exception or document why it's safe to ignore
        }
    }

    private void DoSomething() { }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0022").WithLocation(0);
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Comment outside block | Report | Comment must be inside the catch block |
| Comment before catch | Report | Not documenting the catch block itself |
| `// TODO` comment | Skip | Any comment is acceptable |
| `/* */` empty comment | Skip | Still counts as documentation attempt |
| Nested try-catch | Analyze each | Each catch is independent |
| When clause catch | Analyze block | `catch (Exception e) when (e is...)` |
| Filter catch | Analyze block | Exception filters don't exempt from rule |

---

## Performance Considerations

1. **Single Node Analysis**: Analyzes each catch clause independently
2. **Early Exit**: Checks for statements before scanning trivia
3. **Trivia Scanning**: Only scans trivia if block has no statements
4. **Localized Analysis**: Diagnostic location is on the `catch` keyword

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/EmptyCatchBlockAnalyzer.cs`
- [ ] Create `CodeFixes/Quality/EmptyCatchBlockCodeFixProvider.cs`
- [ ] Implement catch clause detection
- [ ] Implement empty block detection (no statements)
- [ ] Implement comment detection in block
- [ ] Implement "Add TODO comment" code fix
- [ ] Implement "Rethrow exception" code fix
- [ ] Write analyzer tests (~10 tests)
- [ ] Write code fix tests (~4 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
