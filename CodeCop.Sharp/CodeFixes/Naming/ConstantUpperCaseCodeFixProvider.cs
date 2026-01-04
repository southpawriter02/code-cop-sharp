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
    /// Code fix provider for CCS0005 (ConstantUpperCase).
    /// Offers two rename options: PascalCase and UPPER_CASE.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstantUpperCaseCodeFixProvider)), Shared]
    public class ConstantUpperCaseCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ConstantUpperCaseAnalyzer.DiagnosticId);

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

            var constName = variableDeclarator.Identifier.ValueText;

            // Offer two fix options: PascalCase and UPPER_CASE
            var pascalName = NamingUtilities.ToPascalCase(constName);
            var upperName = NamingUtilities.ToUpperSnakeCase(constName);

            // Primary fix: PascalCase (more common in modern C#)
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{pascalName}' (PascalCase)",
                    createChangedSolution: c => RenameConstAsync(context.Document, variableDeclarator, pascalName, c),
                    equivalenceKey: "PascalCase"),
                diagnostic);

            // Alternative fix: UPPER_CASE (only if different from PascalCase)
            if (upperName != pascalName)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Rename to '{upperName}' (UPPER_CASE)",
                        createChangedSolution: c => RenameConstAsync(context.Document, variableDeclarator, upperName, c),
                        equivalenceKey: "UPPER_CASE"),
                    diagnostic);
            }
        }

        private async Task<Solution> RenameConstAsync(
            Document document,
            VariableDeclaratorSyntax variableDeclarator,
            string newName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var constSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator, cancellationToken);

            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(
                solution,
                constSymbol,
                newName,
                solution.Workspace.Options,
                cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
