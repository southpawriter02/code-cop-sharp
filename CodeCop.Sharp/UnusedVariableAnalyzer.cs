using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            DataFlowAnalysis dataFlowAnalysis = null;

            if (methodDeclaration.Body != null)
            {
                dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(methodDeclaration.Body);
            }
            else if (methodDeclaration.ExpressionBody != null)
            {
                dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(methodDeclaration.ExpressionBody.Expression);
            }

            if (dataFlowAnalysis == null || !dataFlowAnalysis.Succeeded)
            {
                return;
            }

            foreach (var variable in dataFlowAnalysis.VariablesDeclared)
            {
                if (!dataFlowAnalysis.ReadInside.Contains(variable))
                {
                    var diagnostic = Diagnostic.Create(Rule, variable.Locations[0], variable.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
