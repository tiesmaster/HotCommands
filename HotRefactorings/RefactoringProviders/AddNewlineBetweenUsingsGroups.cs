using System;
using System.Composition;
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
            var rootCompilation = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            var eolTrivia = SyntaxFactory.CarriageReturnLineFeed;

            var firstUsing = rootCompilation.Usings[0];

            var lastGroupName = GetFirstName(firstUsing.Name);
            for (int i = 1; i < rootCompilation.Usings.Count; i++)
            {
                var nextUsing = rootCompilation.Usings[i];
                var nextGroupName = GetFirstName(nextUsing.Name);
                if (lastGroupName != nextGroupName && nextUsing.GetTrailingTrivia().Last() != eolTrivia)
                {
                    context.RegisterRefactoring(new AddNewlineBetweenUsingsGroupsAction(context, i - 1));
                }
            }
        }

        private string GetFirstName(NameSyntax nameNode)
        {
            var identifierNode = nameNode as IdentifierNameSyntax;
            if (identifierNode != null)
            {
                return identifierNode.ToString();
            }
            else
            {
                var qualifiedNode = (QualifiedNameSyntax)nameNode;
                return GetFirstName(qualifiedNode.Left);
            }
        }
    }

    internal class AddNewlineBetweenUsingsGroupsAction : CodeAction
    {
        private readonly CodeRefactoringContext _context;
        private readonly int _positionToFixup;

        public AddNewlineBetweenUsingsGroupsAction(CodeRefactoringContext context, int positionToFixup)
        {
            _context = context;
            _positionToFixup = positionToFixup;
        }

        public override string Title => "Add newline betweeen using groups";

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            var document = _context.Document;
            var rootCompilation = await document.GetSyntaxRootAsync(_context.CancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;

            var usings = rootCompilation.Usings;

            var usingsToFixup = usings[_positionToFixup];
            var oldTrivia = usingsToFixup.GetTrailingTrivia();
            var newNode = usingsToFixup.WithTrailingTrivia(oldTrivia.Add(SyntaxFactory.CarriageReturnLineFeed));

            usings = usings.Replace(usingsToFixup, newNode);
            rootCompilation = rootCompilation.WithUsings(usings);
            return document.WithSyntaxRoot(rootCompilation);
        }
    }
}