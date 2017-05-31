using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace HotCommands
{
    internal sealed class ChangeModifierAction : CodeAction
    {
        private readonly CodeRefactoringContext _context;
        private readonly Accessibility _newAccessibility;
        private readonly ChangeModifierContext _legacyContext;

        public override string Title
        {
            get
            {
                return _legacyContext.Title;
            }
        }

        public ChangeModifierAction(
            CodeRefactoringContext context,
            Accessibility newAccessibility,
            ChangeModifierContext legacyContext)
        {
            _context = context;
            _newAccessibility = newAccessibility;

            _legacyContext = legacyContext;
        }

        protected override async Task<Document> GetChangedDocumentAsync (CancellationToken cancellationToken)
        {
            var document = _context.Document;

            var rootNode = await document.GetSyntaxRootAsync(_context.CancellationToken).ConfigureAwait(false);
            var node = GetTypeNode(rootNode);

            if (node == null) return document;

            var classNode = node as ClassDeclarationSyntax;
            if (classNode != null)
            {
                var supportedAccessibilities = new[] { Accessibility.Private };
                if (supportedAccessibilities.Contains(_newAccessibility))
                {
                    var modifiers = classNode.Modifiers;
                    modifiers = modifiers.Replace(modifiers[0], SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

                    return document.WithSyntaxRoot(rootNode.ReplaceNode(classNode, classNode.WithModifiers(modifiers)));
                }
            }

            // First, remove all but the first MainModifier
            while (HasMoreThanOneMainModifier(node))
            {
                // Remove the last MainModifier
                rootNode = rootNode.ReplaceToken(GetLastMainModifier(node), SyntaxFactory.Token(SyntaxKind.None));
                node = GetTypeNode(rootNode);
            }

            // Second, replace the MainModifier with the NewModifiers
            rootNode = rootNode.ReplaceToken(node.Modifiers.First(), _legacyContext.NewModifiers);

            // Cleanup additional modifiers (ie. "internal" left behind)
            return document.WithSyntaxRoot(rootNode);
        }

        private BaseTypeDeclarationSyntax GetTypeNode(SyntaxNode rootNode)
        {
            return rootNode.FindNode(_context.Span) as BaseTypeDeclarationSyntax;
        }

        private bool HasMoreThanOneMainModifier(BaseTypeDeclarationSyntax node)
        {
            var mainModifierCount = node.Modifiers.Count(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                                  m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                                  m.IsKind(SyntaxKind.InternalKeyword) ||
                                                  m.IsKind(SyntaxKind.PrivateKeyword));
            return mainModifierCount > 1;
        }

        private static SyntaxToken GetLastMainModifier(BaseTypeDeclarationSyntax node)
        {
            return node.Modifiers.Last(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                            m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                            m.IsKind(SyntaxKind.InternalKeyword) ||
                                            m.IsKind(SyntaxKind.PrivateKeyword));
        }
    }
}