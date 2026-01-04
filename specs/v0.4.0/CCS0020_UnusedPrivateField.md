# CCS0020: UnusedPrivateField

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0020 |
| Category | Quality |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects private fields that are declared but never read. Unused fields indicate dead code that should be removed to improve maintainability.

### Why This Rule?

1. **Dead Code**: Unused fields are unnecessary code weight
2. **Confusion**: Developers may wonder why a field exists
3. **Maintenance**: Removing unused code reduces complexity
4. **Memory**: Unused fields waste memory (minor but measurable)

### What Counts as "Used"?

A field is considered **used** if it is **read** anywhere in the code. Fields that are only assigned (written to) but never read are still flagged as unused.

---

## Compliant Examples

```csharp
public class MyClass
{
    private int _counter;          // Used - read in method
    private readonly string _name; // Used - read in property

    public MyClass(string name)
    {
        _name = name;
    }

    public void Increment()
    {
        _counter++;                // Read (compound assignment)
        Console.WriteLine(_counter); // Read
    }

    public string Name => _name;   // Read
}

// Field used in lambda
public class EventHandler
{
    private Action _callback;

    public void Register(Action action)
    {
        _callback = action;
    }

    public void Execute()
    {
        _callback?.Invoke();       // Read
    }
}

// Field used in LINQ
public class DataProcessor
{
    private int _threshold;

    public IEnumerable<int> Filter(IEnumerable<int> data)
    {
        return data.Where(x => x > _threshold); // Read in lambda
    }
}
```

## Non-Compliant Examples

```csharp
public class MyClass
{
    private int _unusedField;           // CCS0020 - never read
    private string _writeOnly;          // CCS0020 - only written, never read
    private static int _unusedStatic;   // CCS0020 - static unused

    public void Method()
    {
        _writeOnly = "value";           // Write doesn't count as "used"
    }
}

// Only initialized, never read
public class Config
{
    private int _maxRetries = 5;        // CCS0020 - initialized but never read
}

// Only passed as out parameter
public class Parser
{
    private int _result;                // CCS0020

    public bool TryParse(string s)
    {
        return int.TryParse(s, out _result); // out = write only
    }
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Quality/
│       └── UnusedPrivateFieldAnalyzer.cs
└── CodeFixes/
    └── Quality/
        └── UnusedPrivateFieldCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.Quality
{
    /// <summary>
    /// Analyzer that detects unused private fields.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0020
    /// Category: Quality
    /// Severity: Warning
    ///
    /// A private field is considered "unused" if it is never read.
    /// Fields that are only written to are still flagged as unused.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnusedPrivateFieldAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic ID for this analyzer.
        /// </summary>
        public const string DiagnosticId = "CCS0020";

        private static readonly LocalizableString Title = "Unused private field";
        private static readonly LocalizableString MessageFormat =
            "Private field '{0}' is never used";
        private static readonly LocalizableString Description =
            "Private fields that are never read should be removed.";
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

            // Use compilation-level analysis to track usage across the compilation
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Thread-safe collections for concurrent execution
                var privateFields = new Dictionary<IFieldSymbol, Location>(SymbolEqualityComparer.Default);
                var readFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
                var lockObj = new object();

                // Phase 1: Collect all private fields
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var fieldSymbol = (IFieldSymbol)symbolContext.Symbol;

                    // Only analyze private fields
                    if (fieldSymbol.DeclaredAccessibility != Accessibility.Private)
                        return;

                    // Skip compiler-generated fields (backing fields for auto-properties)
                    if (fieldSymbol.IsImplicitlyDeclared)
                        return;

                    // Skip const fields (compile-time values, always "used")
                    if (fieldSymbol.IsConst)
                        return;

                    var location = fieldSymbol.Locations.FirstOrDefault();
                    if (location != null)
                    {
                        lock (lockObj)
                        {
                            privateFields[fieldSymbol] = location;
                        }
                    }
                }, SymbolKind.Field);

                // Phase 2: Track field reads
                compilationContext.RegisterSyntaxNodeAction(nodeContext =>
                {
                    var identifier = (IdentifierNameSyntax)nodeContext.Node;
                    var symbolInfo = nodeContext.SemanticModel.GetSymbolInfo(identifier);

                    if (symbolInfo.Symbol is IFieldSymbol fieldSymbol &&
                        fieldSymbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        // Check if this is a read access (not just assignment target)
                        if (IsReadAccess(identifier))
                        {
                            lock (lockObj)
                            {
                                readFields.Add(fieldSymbol);
                            }
                        }
                    }
                }, SyntaxKind.IdentifierName);

                // Phase 3: Report unused fields
                compilationContext.RegisterCompilationEndAction(endContext =>
                {
                    foreach (var kvp in privateFields)
                    {
                        if (!readFields.Contains(kvp.Key))
                        {
                            var diagnostic = Diagnostic.Create(
                                Rule,
                                kvp.Value,
                                kvp.Key.Name);
                            endContext.ReportDiagnostic(diagnostic);
                        }
                    }
                });
            });
        }

        /// <summary>
        /// Determines if an identifier is being read (not just written to).
        /// </summary>
        private static bool IsReadAccess(IdentifierNameSyntax identifier)
        {
            var parent = identifier.Parent;

            // Check if it's the left side of a simple assignment
            if (parent is AssignmentExpressionSyntax assignment)
            {
                // It's a read if it's on the right side
                if (assignment.Right == identifier)
                    return true;

                // It's a read if it's a compound assignment (+=, -=, etc.)
                if (assignment.Left == identifier &&
                    assignment.Kind() != SyntaxKind.SimpleAssignmentExpression)
                {
                    return true;
                }

                // Pure write (simple assignment on left side)
                if (assignment.Left == identifier)
                    return false;
            }

            // Check if it's being passed as an out parameter (write only)
            if (parent is ArgumentSyntax argument)
            {
                if (argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
                {
                    return false;
                }
            }

            // Check if it's a prefix/postfix increment/decrement (read + write)
            if (parent is PrefixUnaryExpressionSyntax prefix)
            {
                if (prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                    prefix.IsKind(SyntaxKind.PreDecrementExpression))
                {
                    return true;
                }
            }

            if (parent is PostfixUnaryExpressionSyntax postfix)
            {
                if (postfix.IsKind(SyntaxKind.PostIncrementExpression) ||
                    postfix.IsKind(SyntaxKind.PostDecrementExpression))
                {
                    return true;
                }
            }

            // All other uses are reads
            return true;
        }
    }
}
```

### Code Fix Provider Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Quality
{
    /// <summary>
    /// Code fix provider for CCS0020 (UnusedPrivateField).
    /// Removes the unused field declaration.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedPrivateFieldCodeFixProvider)), Shared]
    public class UnusedPrivateFieldCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(UnusedPrivateFieldAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);
            var variableDeclarator = node.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();

            if (variableDeclarator == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove unused field",
                    createChangedDocument: c => RemoveFieldAsync(context.Document, variableDeclarator, c),
                    equivalenceKey: "RemoveUnusedField"),
                diagnostic);
        }

        private async Task<Document> RemoveFieldAsync(
            Document document,
            VariableDeclaratorSyntax variableDeclarator,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var fieldDeclaration = variableDeclarator.Ancestors().OfType<FieldDeclarationSyntax>().FirstOrDefault();
            if (fieldDeclaration == null)
                return document;

            SyntaxNode newRoot;

            // If this is the only variable in the declaration, remove the whole field
            if (fieldDeclaration.Declaration.Variables.Count == 1)
            {
                newRoot = root.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepLeadingTrivia);
            }
            else
            {
                // Multiple variables - just remove this one
                var newDeclaration = fieldDeclaration.Declaration.RemoveNode(
                    variableDeclarator,
                    SyntaxRemoveOptions.KeepLeadingTrivia);
                var newField = fieldDeclaration.WithDeclaration(newDeclaration);
                newRoot = root.ReplaceNode(fieldDeclaration, newField);
            }

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

---

## Decision Tree

```
┌────────────────────────────────────┐
│ Is the symbol a field?             │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Is the field private?              │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      NO       │──────────► SKIP (public/protected/internal)
          └───────┬───────┘
                  │ YES
                  ▼
┌────────────────────────────────────┐
│ Is the field compiler-generated?   │
│ (auto-property backing field)      │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Is the field const?                │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP
          └───────┬───────┘
                  │ NO
                  ▼
┌────────────────────────────────────┐
│ Collect field declaration          │
└─────────────────┬──────────────────┘
                  │
                  ▼
┌────────────────────────────────────┐
│ At compilation end:                │
│ Is field ever read?                │
└─────────────────┬──────────────────┘
                  │
          ┌───────▼───────┐
          │      YES      │──────────► SKIP (field is used)
          └───────┬───────┘
                  │ NO
                  ▼
            REPORT CCS0020
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| NeverUsed | `private int _unused;` | CCS0020 |
| WriteOnly | `private int _x; void M() { _x = 1; }` | CCS0020 |
| StaticUnused | `private static int _unused;` | CCS0020 |
| ReadonlyUnused | `private readonly int _unused;` | CCS0020 |
| MultipleUnused | `private int _a, _b;` (both unused) | CCS0020 x2 |
| InitializedButNotRead | `private int _x = 5;` | CCS0020 |
| OutParameterOnly | `private int _x; void M() { int.TryParse("1", out _x); }` | CCS0020 |
| OnlyInAssignmentLeft | `private int _x; void M() { _x = GetValue(); }` | CCS0020 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| FieldRead | `private int _x; int Get() => _x;` | No diagnostic |
| FieldUsedInExpression | `private int _x; void M() { var y = _x + 1; }` | No diagnostic |
| FieldUsedInCondition | `private bool _flag; void M() { if (_flag) {} }` | No diagnostic |
| FieldInCompoundAssignment | `private int _x; void M() { _x += 1; }` | No diagnostic |
| FieldIncremented | `private int _x; void M() { _x++; }` | No diagnostic |
| FieldPassedByRef | `private int _x; void M(ref int y) { M(ref _x); }` | No diagnostic |
| FieldUsedInLambda | `private int _x; void M() { Action a = () => Console.WriteLine(_x); }` | No diagnostic |
| FieldUsedInLinq | `private int _x; IEnumerable<int> M(int[] arr) => arr.Where(i => i > _x);` | No diagnostic |
| PublicField | `public int Unused;` | No diagnostic (not private) |
| ProtectedField | `protected int Unused;` | No diagnostic |
| InternalField | `internal int Unused;` | No diagnostic |
| ConstField | `private const int X = 1;` | No diagnostic |
| BackingField | Auto-property backing field | No diagnostic |

### Code Fix Tests

| Test Name | Before | After |
|-----------|--------|----------|
| RemoveSingleField | `private int _unused;` | (field removed) |
| RemoveFromMultiple | `private int _used, _unused;` | `private int _used;` |
| RemoveWithInitializer | `private int _unused = 5;` | (field removed) |
| PreserveUsedField | `private int _used, _unused; int Get() => _used;` | `private int _used; int Get() => _used;` |

---

## Test Code Template

```csharp
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
    CodeCop.Sharp.Analyzers.Quality.UnusedPrivateFieldAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<
    CodeCop.Sharp.Analyzers.Quality.UnusedPrivateFieldAnalyzer,
    CodeCop.Sharp.CodeFixes.Quality.UnusedPrivateFieldCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Quality
{
    public class UnusedPrivateFieldAnalyzerTests
    {
        [Fact]
        public async Task FieldNeverUsed_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:_unused|};
}";

            var expected = VerifyCS.Diagnostic("CCS0020")
                .WithLocation(0)
                .WithArguments("_unused");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task FieldWriteOnly_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:_writeOnly|};

    public void Set(int value)
    {
        _writeOnly = value;
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0020")
                .WithLocation(0)
                .WithArguments("_writeOnly");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task FieldReadInMethod_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int _counter;

    public int GetCount() => _counter;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task FieldUsedInCompoundAssignment_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int _counter;

    public void Increment()
    {
        _counter += 1;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task FieldIncremented_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int _counter;

    public void Increment()
    {
        _counter++;
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task FieldUsedInLambda_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System;

public class MyClass
{
    private int _value;

    public Action GetAction()
    {
        return () => Console.WriteLine(_value);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ConstField_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int MaxValue = 100;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PublicField_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int PublicField;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task CodeFix_RemovesUnusedField()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:_unused|};
    private int _used;

    public int Get() => _used;
}";

            var fixedCode = @"
public class MyClass
{
    private int _used;

    public int Get() => _used;
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0020")
                .WithLocation(0)
                .WithArguments("_unused");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RemovesFromMultipleDeclaration()
        {
            var testCode = @"
public class MyClass
{
    private int _used, {|#0:_unused|};

    public int Get() => _used;
}";

            var fixedCode = @"
public class MyClass
{
    private int _used;

    public int Get() => _used;
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0020")
                .WithLocation(0)
                .WithArguments("_unused");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }
    }
}
```

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Field used in lambda | NOT flagged | Lambda captures are reads |
| Field used in LINQ | NOT flagged | LINQ expressions read fields |
| Field passed to method | NOT flagged | Passing as argument is a read |
| Field in string interpolation | NOT flagged | Interpolation reads value |
| Field in nameof() | NOT flagged | nameof reads symbol name |
| Field with `[field:]` attribute | Consider flagging | May need serialization exception |
| Partial class field | Analyze whole compilation | Field might be used in other part |
| Field used via reflection | Flagged | Analyzer cannot detect reflection usage |
| Field in conditional expression | NOT flagged | `condition ? _field : 0` is a read |

---

## Performance Considerations

1. **Compilation-Level Analysis**: Uses `RegisterCompilationStartAction` for cross-file analysis
2. **Thread Safety**: Uses locks for concurrent execution support
3. **Symbol Comparison**: Uses `SymbolEqualityComparer.Default` for proper symbol comparison
4. **Early Filtering**: Skips non-private, const, and compiler-generated fields early

---

## Deliverable Checklist

- [ ] Create `Analyzers/Quality/UnusedPrivateFieldAnalyzer.cs`
- [ ] Create `CodeFixes/Quality/UnusedPrivateFieldCodeFixProvider.cs`
- [ ] Implement field collection in compilation start
- [ ] Implement read access tracking
- [ ] Implement unused field reporting at compilation end
- [ ] Handle all read access patterns (compound assignment, increment, etc.)
- [ ] Write analyzer tests (~15 tests)
- [ ] Write code fix tests (~4 tests)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
