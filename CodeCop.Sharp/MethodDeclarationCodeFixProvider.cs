using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MethodDeclarationCodeFixProvider)), Shared]
    public class MethodDeclarationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(MethodDeclarationAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            var methodName = declaration.Identifier.ValueText;
            var newName = MethodDeclarationAnalyzer.ToPascalCase(methodName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{newName}'",
                    createChangedSolution: c => RenameMethodAsync(context.Document, declaration, newName, c),
                    equivalenceKey: nameof(MethodDeclarationCodeFixProvider)),
                diagnostic);
        }

        private async Task<Solution> RenameMethodAsync(Document document, MethodDeclarationSyntax methodDeclaration, string newName, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);

            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, newName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
