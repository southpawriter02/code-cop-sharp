# CCS0033: PreferLinqMethod

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0033 |
| Category | BestPractices |
| Severity | Info |
| Has Code Fix | Yes |
| Enabled by Default | Yes |

## Description

Detects manual loops that could be replaced with LINQ methods. LINQ provides more declarative, readable code that expresses intent more clearly than imperative loops.

## Patterns Detected

| Pattern | Loop Code | LINQ Equivalent |
|---------|-----------|-----------------|
| Any | `foreach + if + return true` | `.Any(predicate)` |
| All | `foreach + if (!condition) return false` | `.All(predicate)` |
| Count | `foreach + counter++` | `.Count()` or `.Count(predicate)` |
| First | `foreach + if + return item` | `.First(predicate)` or `.FirstOrDefault(predicate)` |
| Contains | `foreach + if (item == x) return true` | `.Contains(x)` |

## Compliant Examples

```csharp
// Good - uses LINQ
bool hasExpired = items.Any(x => x.IsExpired);

int count = items.Count(x => x.IsActive);

var first = items.FirstOrDefault(x => x.Id == targetId);

bool exists = ids.Contains(searchId);
```

## Non-Compliant Examples

```csharp
// CCS0033 - could use Any()
bool hasExpired = false;
foreach (var item in items)
{
    if (item.IsExpired)
    {
        hasExpired = true;
        break;
    }
}

// CCS0033 - could use Count()
int count = 0;
foreach (var item in items)
{
    if (item.IsActive)
    {
        count++;
    }
}

// CCS0033 - could use Contains()
bool exists = false;
foreach (var id in ids)
{
    if (id == searchId)
    {
        exists = true;
        break;
    }
}
```

## Implementation Specification

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp.Analyzers.BestPractices
{
    /// <summary>
    /// Analyzer that suggests LINQ methods over manual loops.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferLinqMethodAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0033";

        private static readonly LocalizableString Title = "Prefer LINQ method";
        private static readonly LocalizableString MessageFormat =
            "This loop can be simplified using '{0}'";
        private static readonly LocalizableString Description =
            "Manual loops that match LINQ patterns should use the corresponding LINQ method for clarity.";
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

            context.RegisterSyntaxNodeAction(AnalyzeForEach, SyntaxKind.ForEachStatement);
        }

        private void AnalyzeForEach(SyntaxNodeAnalysisContext context)
        {
            var foreachStatement = (ForEachStatementSyntax)context.Node;

            // Check for Any pattern
            if (TryMatchAnyPattern(foreachStatement, out var linqMethod))
            {
                ReportDiagnostic(context, foreachStatement, linqMethod);
                return;
            }

            // Check for Count pattern
            if (TryMatchCountPattern(foreachStatement, out linqMethod))
            {
                ReportDiagnostic(context, foreachStatement, linqMethod);
                return;
            }

            // Check for Contains pattern
            if (TryMatchContainsPattern(foreachStatement, out linqMethod))
            {
                ReportDiagnostic(context, foreachStatement, linqMethod);
                return;
            }

            // Check for FirstOrDefault pattern
            if (TryMatchFirstOrDefaultPattern(foreachStatement, out linqMethod))
            {
                ReportDiagnostic(context, foreachStatement, linqMethod);
                return;
            }
        }

        private static bool TryMatchAnyPattern(ForEachStatementSyntax foreach_, out string linqMethod)
        {
            linqMethod = "Any()";

            // Pattern: foreach + if + return true/set flag + break
            var statement = foreach_.Statement;
            if (statement is not BlockSyntax block)
                return false;

            if (block.Statements.Count != 1)
                return false;

            if (block.Statements[0] is not IfStatementSyntax ifStatement)
                return false;

            // Check if body sets a bool flag and breaks, or returns true
            var ifBody = ifStatement.Statement;
            if (ifBody is BlockSyntax ifBlock)
            {
                // Look for: flag = true; break;
                if (ifBlock.Statements.Count == 2 &&
                    ifBlock.Statements[0] is ExpressionStatementSyntax expr &&
                    expr.Expression is AssignmentExpressionSyntax assignment &&
                    assignment.Right is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.TrueLiteralExpression) &&
                    ifBlock.Statements[1] is BreakStatementSyntax)
                {
                    return true;
                }
            }
            else if (ifBody is ReturnStatementSyntax returnStmt &&
                     returnStmt.Expression is LiteralExpressionSyntax retLiteral &&
                     retLiteral.IsKind(SyntaxKind.TrueLiteralExpression))
            {
                return true;
            }

            return false;
        }

        private static bool TryMatchCountPattern(ForEachStatementSyntax foreach_, out string linqMethod)
        {
            linqMethod = "Count()";

            var statement = foreach_.Statement;
            if (statement is not BlockSyntax block)
                return false;

            // Pattern: counter++ (possibly with condition)
            foreach (var stmt in block.Statements)
            {
                if (stmt is ExpressionStatementSyntax expr &&
                    expr.Expression is PostfixUnaryExpressionSyntax postfix &&
                    postfix.IsKind(SyntaxKind.PostIncrementExpression))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryMatchContainsPattern(ForEachStatementSyntax foreach_, out string linqMethod)
        {
            linqMethod = "Contains()";

            // Pattern: if (item == value) return true/set flag
            var statement = foreach_.Statement;
            if (statement is not BlockSyntax block)
                return false;

            if (block.Statements.Count != 1)
                return false;

            if (block.Statements[0] is not IfStatementSyntax ifStatement)
                return false;

            // Check for equality comparison
            if (ifStatement.Condition is not BinaryExpressionSyntax binary)
                return false;

            if (!binary.IsKind(SyntaxKind.EqualsExpression))
                return false;

            // One side should be the loop variable
            var loopVar = foreach_.Identifier.Text;
            var left = binary.Left.ToString();
            var right = binary.Right.ToString();

            return left == loopVar || right == loopVar;
        }

        private static bool TryMatchFirstOrDefaultPattern(ForEachStatementSyntax foreach_, out string linqMethod)
        {
            linqMethod = "FirstOrDefault()";

            // Pattern: if (condition) return item
            var statement = foreach_.Statement;
            if (statement is not BlockSyntax block)
                return false;

            if (block.Statements.Count != 1)
                return false;

            if (block.Statements[0] is not IfStatementSyntax ifStatement)
                return false;

            // Check if body returns the loop variable
            if (ifStatement.Statement is ReturnStatementSyntax returnStmt)
            {
                var loopVar = foreach_.Identifier.Text;
                return returnStmt.Expression?.ToString() == loopVar;
            }

            return false;
        }

        private static void ReportDiagnostic(
            SyntaxNodeAnalysisContext context,
            ForEachStatementSyntax foreach_,
            string linqMethod)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                foreach_.ForEachKeyword.GetLocation(),
                linqMethod);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

## Test Cases

| Test Name | Pattern | Expected |
|-----------|---------|----------|
| AnyPattern | foreach + if + return true | CCS0033: "Any()" |
| CountPattern | foreach + counter++ | CCS0033: "Count()" |
| ContainsPattern | foreach + if (item == x) | CCS0033: "Contains()" |
| FirstOrDefaultPattern | foreach + if + return item | CCS0033: "FirstOrDefault()" |
| ComplexLoop | Loop with multiple statements | No diagnostic |
| WhileLoop | while loop | No diagnostic |
