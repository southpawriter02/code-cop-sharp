using CodeCop.Sharp.Analyzers.Naming;
using CodeCop.Sharp.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Naming
{
    /// <summary>
    /// Code fix provider for CCS0004 (PrivateFieldCamelCase).
    /// Renames private fields to camelCase and updates all references.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PrivateFieldCamelCaseCodeFixProvider)), Shared]
    public class PrivateFieldCamelCaseCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(PrivateFieldCamelCaseAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var variableDeclarator = root.FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<VariableDeclaratorSyntax>()
                .First();

            var fieldName = variableDeclarator.Identifier.ValueText;
            var newName = NamingUtilities.ToCamelCase(fieldName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{newName}'",
                    createChangedSolution: c => RenameFieldAsync(context.Document, variableDeclarator, newName, c),
                    equivalenceKey: nameof(PrivateFieldCamelCaseCodeFixProvider)),
                diagnostic);
        }

        private async Task<Solution> RenameFieldAsync(
            Document document,
            VariableDeclaratorSyntax variableDeclarator,
            string newName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var fieldSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(
                solution,
                fieldSymbol,
                newName,
                solution.Workspace.Options,
                cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
