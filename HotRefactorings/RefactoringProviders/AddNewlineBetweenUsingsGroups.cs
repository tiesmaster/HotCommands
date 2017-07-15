using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
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
            var rootCompilation = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false) as CompilationUnitSyntax;

            var firstUsing = rootCompilation.Usings[0];

            var usingPositionsMissingNewline = new List<int>();
            for (int i = 1; i < rootCompilation.Usings.Count; i++)
            {
                var previousUsing = rootCompilation.Usings[i - 1];
                var currentUsing = rootCompilation.Usings[i];

                if (!TopLevelNamespaceEquals(previousUsing, currentUsing) && !HasLeadingNewline(currentUsing))
                {
                    usingPositionsMissingNewline.Add(i - 1);
                }
            }

            if (usingPositionsMissingNewline.Any())
            {
                context.RegisterRefactoring(new AddNewlineBetweenUsingsGroupsAction(context, usingPositionsMissingNewline));
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
    }

    internal class AddNewlineBetweenUsingsGroupsAction : CodeAction
    {
        private readonly CodeRefactoringContext _context;
        private readonly IEnumerable<int> _usingPositionsMissingNewline;

        public AddNewlineBetweenUsingsGroupsAction(
            CodeRefactoringContext context,
            IEnumerable<int> usingPositionsMissingNewline)
        {
            _context = context;
            _usingPositionsMissingNewline = usingPositionsMissingNewline;
        }

        public override string Title => "Add newline betweeen using groups";

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var document = _context.Document;
            var rootCompilation = await document.GetSyntaxRootAsync(_context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            var listOfUsings = rootCompilation.Usings;
            foreach (var usingPosition in _usingPositionsMissingNewline)
            {
                var usingMissingNewline = listOfUsings[usingPosition];
                listOfUsings = listOfUsings.Replace(usingMissingNewline, AddTrailingNewline(usingMissingNewline));
            }

            return document.WithSyntaxRoot(rootCompilation.WithUsings(listOfUsings));
        }

        private static TNode AddTrailingNewline<TNode>(TNode node) where TNode : SyntaxNode
        {
            var oldTrivia = node.GetTrailingTrivia();
            return node.WithTrailingTrivia(oldTrivia.Add(SyntaxFactory.CarriageReturnLineFeed));
        }
    }
}