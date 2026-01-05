# CCS0036: UseNullCoalescing

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0036 |
| Category | BestPractices |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects patterns that can be simplified using the null-coalescing operator (`??`) or null-coalescing assignment operator (`??=`).

## Patterns Detected

| Pattern | Verbose | Simplified |
|---------|---------|------------|
| Ternary null check | `x != null ? x : defaultValue` | `x ?? defaultValue` |
| If null assignment | `if (x == null) x = value;` | `x ??= value;` |
| Or pattern | `x == null \|\| x == ""` | Consider null check |

## Compliant Examples

```csharp
// Good - uses null-coalescing
var name = userName ?? "Guest";

var instance = _cache ?? CreateNew();

// Good - uses null-coalescing assignment
_lazyValue ??= ComputeValue();

items ??= new List<Item>();
```

## Non-Compliant Examples

```csharp
// CCS0036 - can use null-coalescing
var name = userName != null ? userName : "Guest";

var instance = _cache != null ? _cache : CreateNew();

// CCS0036 - can use null-coalescing assignment
if (_lazyValue == null)
{
    _lazyValue = ComputeValue();
}

if (items == null)
    items = new List<Item>();
```

## Implementation Specification

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.BestPractices
{
    /// <summary>
    /// Analyzer that suggests null-coalescing operators (?? and ??=).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseNullCoalescingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0036";

        private static readonly LocalizableString Title = "Use null-coalescing operator";
        private static readonly LocalizableString MessageFormat =
            "Use '{0}' instead of explicit null check";
        private static readonly LocalizableString Description =
            "The null-coalescing operator provides a more concise way to handle null defaults.";
        private const string Category = "BestPractices";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        }

        private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
        {
            var conditional = (ConditionalExpressionSyntax)context.Node;

            // Pattern: x != null ? x : defaultValue
            if (TryMatchNullCoalescePattern(conditional, out var identifier))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    conditional.GetLocation(),
                    "??");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            // Pattern: if (x == null) x = value;
            if (TryMatchNullCoalesceAssignmentPattern(ifStatement, out var identifier))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    ifStatement.GetLocation(),
                    "??=");
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool TryMatchNullCoalescePattern(
            ConditionalExpressionSyntax conditional,
            out string identifier)
        {
            identifier = "";

            // Check for: x != null ? x : default
            if (conditional.Condition is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.NotEqualsExpression))
            {
                // x != null
                if (binary.Right is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.NullLiteralExpression) &&
                    binary.Left is IdentifierNameSyntax id)
                {
                    // WhenTrue should be the same identifier
                    if (conditional.WhenTrue is IdentifierNameSyntax whenTrueId &&
                        whenTrueId.Identifier.Text == id.Identifier.Text)
                    {
                        identifier = id.Identifier.Text;
                        return true;
                    }
                }
            }

            // Check for: x == null ? default : x
            if (conditional.Condition is BinaryExpressionSyntax binary2 &&
                binary2.IsKind(SyntaxKind.EqualsExpression))
            {
                if (binary2.Right is LiteralExpressionSyntax literal2 &&
                    literal2.IsKind(SyntaxKind.NullLiteralExpression) &&
                    binary2.Left is IdentifierNameSyntax id2)
                {
                    // WhenFalse should be the same identifier
                    if (conditional.WhenFalse is IdentifierNameSyntax whenFalseId &&
                        whenFalseId.Identifier.Text == id2.Identifier.Text)
                    {
                        identifier = id2.Identifier.Text;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryMatchNullCoalesceAssignmentPattern(
            IfStatementSyntax ifStatement,
            out string identifier)
        {
            identifier = "";

            // Skip if there's an else clause
            if (ifStatement.Else != null)
                return false;

            // Check condition: x == null
            if (ifStatement.Condition is not BinaryExpressionSyntax binary ||
                !binary.IsKind(SyntaxKind.EqualsExpression))
                return false;

            if (binary.Right is not LiteralExpressionSyntax literal ||
                !literal.IsKind(SyntaxKind.NullLiteralExpression))
                return false;

            if (binary.Left is not IdentifierNameSyntax conditionId)
                return false;

            // Check body: x = value;
            StatementSyntax? statement = ifStatement.Statement;
            if (statement is BlockSyntax block)
            {
                if (block.Statements.Count != 1)
                    return false;
                statement = block.Statements[0];
            }

            if (statement is not ExpressionStatementSyntax exprStmt)
                return false;

            if (exprStmt.Expression is not AssignmentExpressionSyntax assignment)
                return false;

            if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                return false;

            if (assignment.Left is not IdentifierNameSyntax assignId)
                return false;

            // Verify same identifier
            if (assignId.Identifier.Text != conditionId.Identifier.Text)
                return false;

            identifier = conditionId.Identifier.Text;
            return true;
        }
    }
}
```

## Test Cases

| Test Name | Input | Expected |
|-----------|-------|----------|
| TernaryNotNull | `x != null ? x : default` | CCS0036: "??" |
| TernaryIsNull | `x == null ? default : x` | CCS0036: "??" |
| IfNullAssign | `if (x == null) x = value;` | CCS0036: "??=" |
| IfNullAssignBlock | `if (x == null) { x = value; }` | CCS0036: "??=" |
| AlreadyCoalesce | `x ?? default` | No diagnostic |
| AlreadyCoalesceAssign | `x ??= value;` | No diagnostic |
| IfWithElse | `if (x == null) x = a; else x = b;` | No diagnostic |
