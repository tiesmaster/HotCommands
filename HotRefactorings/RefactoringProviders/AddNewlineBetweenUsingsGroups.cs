using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotCommands.RefactoringProviders
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(AddNewlineBetweenUsingsGroups)), Shared]
    public class AddNewlineBetweenUsingsGroups : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var rootCompilation = root as CompilationUnitSyntax;

            var listOfUsings = rootCompilation.Usings;

            if (listOfUsings.Count < 2)
            {
                return;
            }

            var firstUsing = listOfUsings[0];

            var nodesMissingNewline = new List<SyntaxNode>();
            for (int i = 1; i < rootCompilation.Usings.Count; i++)
            {
                var previousUsing = rootCompilation.Usings[i - 1];
                var currentUsing = rootCompilation.Usings[i];

                if (!TopLevelNamespaceEquals(previousUsing, currentUsing) && !HasLeadingNewline(currentUsing))
                {
                    nodesMissingNewline.Add(previousUsing);
                }
            }

            if (nodesMissingNewline.Any())
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        "Add newline betweeen using groups",
                        _ => AddNewlinesToNodes(context.Document, root, nodesMissingNewline)));
            }
        }

        private bool TopLevelNamespaceEquals(UsingDirectiveSyntax left, UsingDirectiveSyntax right)
        {
            var leftToplevel = GetToplevelNamespaceName(right);
            var rightToplevel = GetToplevelNamespaceName(left);

            return leftToplevel == rightToplevel;
        }

        private string GetToplevelNamespaceName(UsingDirectiveSyntax usingNode)
            => GetFirstNamespaceName(usingNode.Name);

        private string GetFirstNamespaceName(NameSyntax nameNode)
        {
            var identifierNode = nameNode as IdentifierNameSyntax;
            if (identifierNode != null)
            {
                return identifierNode.ToString();
            }
            else
            {
                var qualifiedNode = (QualifiedNameSyntax)nameNode;
                return GetFirstNamespaceName(qualifiedNode.Left);
            }
        }

        private static bool HasLeadingNewline(SyntaxNode node)
        {
            if (!node.HasLeadingTrivia)
            {
                return false;
            }

            return node.GetLeadingTrivia().First() != SyntaxFactory.CarriageReturnLineFeed;
        }

        private Task<Document> AddNewlinesToNodes(
            Document document,
            SyntaxNode root,
            IEnumerable<SyntaxNode> nodesMissingNewline)
        {
            var newRoot = root.ReplaceNodes(nodesMissingNewline, (oldNode, _) => AddTrailingNewline(oldNode));
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static SyntaxNode AddTrailingNewline(SyntaxNode node)
        {
            var oldTrivia = node.GetTrailingTrivia();
            return node.WithTrailingTrivia(oldTrivia.Add(SyntaxFactory.CarriageReturnLineFeed));
        }
    }
}