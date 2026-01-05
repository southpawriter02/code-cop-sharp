# CCS0035: UseNullConditional

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0035 |
| Category | BestPractices |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects patterns that can be simplified using the null-conditional operator (`?.`). This operator provides a more concise and readable way to handle null checks.

## Patterns Detected

| Pattern | Verbose | Simplified |
|---------|---------|------------|
| Null check + member | `if (x != null) x.Method()` | `x?.Method()` |
| Null check + property | `x != null ? x.Property : null` | `x?.Property` |
| Null check in condition | `if (x != null && x.IsValid)` | `if (x?.IsValid == true)` |

## Compliant Examples

```csharp
// Good - uses null-conditional
var length = text?.Length;

customer?.SendNotification();

var name = person?.Address?.City;

handler?.Invoke(sender, args);
```

## Non-Compliant Examples

```csharp
// CCS0035 - can use null-conditional
var length = text != null ? text.Length : null;

if (customer != null)
{
    customer.SendNotification();
}

var city = person != null
    ? (person.Address != null ? person.Address.City : null)
    : null;

if (handler != null)
{
    handler(sender, args);
}
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
    /// Analyzer that suggests null-conditional operator (?.).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseNullConditionalAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0035";

        private static readonly LocalizableString Title = "Use null-conditional operator";
        private static readonly LocalizableString MessageFormat =
            "Use null-conditional operator '?.' instead of explicit null check";
        private static readonly LocalizableString Description =
            "The null-conditional operator provides a more concise way to handle null checks.";
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

            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
            context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);
        }

        private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (IfStatementSyntax)context.Node;

            // Pattern: if (x != null) x.Something()
            if (IsNullCheckPattern(ifStatement.Condition, out var identifier))
            {
                // Check if the body only accesses the same identifier
                if (ifStatement.Statement is BlockSyntax block &&
                    block.Statements.Count == 1 &&
                    block.Statements[0] is ExpressionStatementSyntax exprStmt &&
                    exprStmt.Expression is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression.ToString() == identifier)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        ifStatement.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
        {
            var conditional = (ConditionalExpressionSyntax)context.Node;

            // Pattern: x != null ? x.Property : null
            if (IsNullCheckPattern(conditional.Condition, out var identifier))
            {
                // Check if WhenTrue accesses the identifier
                if (conditional.WhenTrue is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression.ToString() == identifier)
                {
                    // Check if WhenFalse is null
                    if (conditional.WhenFalse is LiteralExpressionSyntax literal &&
                        literal.IsKind(SyntaxKind.NullLiteralExpression))
                    {
                        var diagnostic = Diagnostic.Create(
                            Rule,
                            conditional.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsNullCheckPattern(ExpressionSyntax condition, out string identifier)
        {
            identifier = "";

            if (condition is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.NotEqualsExpression))
            {
                // x != null
                if (binary.Right is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.NullLiteralExpression) &&
                    binary.Left is IdentifierNameSyntax id)
                {
                    identifier = id.Identifier.Text;
                    return true;
                }

                // null != x
                if (binary.Left is LiteralExpressionSyntax literal2 &&
                    literal2.IsKind(SyntaxKind.NullLiteralExpression) &&
                    binary.Right is IdentifierNameSyntax id2)
                {
                    identifier = id2.Identifier.Text;
                    return true;
                }
            }

            return false;
        }
    }
}
```

## Test Cases

| Test Name | Input | Expected |
|-----------|-------|----------|
| IfNullCheck | `if (x != null) x.Method();` | CCS0035 |
| TernaryNullCheck | `x != null ? x.Prop : null` | CCS0035 |
| ChainedNullCheck | `x != null && x.Y != null ? x.Y.Z : null` | CCS0035 |
| AlreadyNullConditional | `x?.Method()` | No diagnostic |
| NullCheckWithElse | `if (x != null) x.A(); else x = new X();` | No diagnostic |
