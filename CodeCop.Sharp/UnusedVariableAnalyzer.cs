using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCop.Sharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnusedVariableAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0002";

        private static readonly LocalizableString Title = "Unused variable";
        private static readonly LocalizableString MessageFormat = "Variable '{0}' is declared but never used";
        private static readonly LocalizableString Description = "Unused variables should be removed.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeCodeBody, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeCodeBody, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(AnalyzeCodeBody, SyntaxKind.SimpleLambdaExpression);
        }

        private void AnalyzeCodeBody(SyntaxNodeAnalysisContext context)
        {
            var body = GetBody(context.Node);

            if (body == null)
            {
                return;
            }

            var dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(body);

            if (!dataFlowAnalysis.Succeeded)
            {
                return;
            }

            var unusedVariables = dataFlowAnalysis.VariablesDeclared.Except(dataFlowAnalysis.ReadInside);

            foreach (var variable in unusedVariables)
            {
                if (variable.Name.StartsWith("_"))
                {
                    continue;
                }

                // If we are analyzing a method, and the unused variable is declared inside a lambda,
                // we skip it. The analysis of the lambda itself will report it.
                // This prevents duplicate diagnostics.
                if (context.Node is MethodDeclarationSyntax)
                {
                    var variableLocation = variable.Locations.First();
                    var containingLambda = variableLocation.SourceTree.GetRoot(context.CancellationToken)
                        .FindNode(variableLocation.SourceSpan)
                        .AncestorsAndSelf().OfType<LambdaExpressionSyntax>().FirstOrDefault();

                    if (containingLambda != null)
                    {
                        continue;
                    }
                }

                var diagnostic = Diagnostic.Create(Rule, variable.Locations.First(), variable.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private SyntaxNode GetBody(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax method)
            {
                return (SyntaxNode)method.Body ?? method.ExpressionBody?.Expression;
            }
            if (node is LambdaExpressionSyntax lambda)
            {
                return lambda.Body;
            }
            return null;
        }
    }
}
