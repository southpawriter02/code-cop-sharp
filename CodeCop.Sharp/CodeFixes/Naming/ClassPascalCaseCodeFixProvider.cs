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
    /// Code fix provider for CCS0002 (ClassPascalCase).
    /// Renames classes to PascalCase and updates all references.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassPascalCaseCodeFixProvider)), Shared]
    public class ClassPascalCaseCodeFixProvider : CodeFixProvider
    {
        /// <inheritdoc/>
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ClassPascalCaseAnalyzer.DiagnosticId);

        /// <inheritdoc/>
        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<ClassDeclarationSyntax>()
                .First();

            var className = declaration.Identifier.ValueText;
            var newName = NamingUtilities.ToPascalCase(className);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Rename to '{newName}'",
                    createChangedSolution: c => RenameClassAsync(context.Document, declaration, newName, c),
                    equivalenceKey: nameof(ClassPascalCaseCodeFixProvider)),
                diagnostic);
        }

        private async Task<Solution> RenameClassAsync(
            Document document,
            ClassDeclarationSyntax classDeclaration,
            string newName,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);

            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(
                solution,
                classSymbol,
                newName,
                solution.Workspace.Options,
                cancellationToken).ConfigureAwait(false);

            return newSolution;
        }
    }
}
