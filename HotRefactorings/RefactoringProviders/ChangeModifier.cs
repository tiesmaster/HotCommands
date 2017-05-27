using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;
using System.Composition;

namespace HotCommands
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(ChangeModifier)), Shared]
    public class ChangeModifier : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync (CodeRefactoringContext context)
        {
            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = rootNode.FindNode(context.Span);

            if (!IsTypeDeclaration(node))
            {
                return;
            }

            var modifiers = node.GetModifierTokens();

            // Activate if modifier is Public, Private, Protected or Internal
            var mainModifierCount = modifiers.Count(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                                              m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                                              m.IsKind(SyntaxKind.InternalKeyword) ||
                                                              m.IsKind(SyntaxKind.PrivateKeyword));
            // Don't offer this refactoring if there are no modifiers (only because I don't know how to add new modifiers without "ReplaceToken")
            if (mainModifierCount == 0) return;

            var hasPublicKeyword = modifiers.Any(SyntaxKind.PublicKeyword);
            var hasProtectedKeyword = modifiers.Any(SyntaxKind.ProtectedKeyword);
            var hasInternalKeyword = modifiers.Any(SyntaxKind.InternalKeyword);
            var hasPrivateKeyword = modifiers.Any(SyntaxKind.PrivateKeyword);
            var hasProtectedInternalKeywords = hasProtectedKeyword && hasInternalKeyword;

            var hasRedundantModifiers = (hasProtectedInternalKeywords && mainModifierCount > 2) ||
                                        (!hasProtectedInternalKeywords && mainModifierCount > 1);

            if (mainModifierCount > 1 || !hasPublicKeyword)
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Public" + (hasRedundantModifiers ? " (Remove redundant modifiers)" : ""),
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.PublicKeyword)}
                }));
            }

            if (mainModifierCount > 1 || !hasProtectedKeyword || hasInternalKeyword)
            {
                var title = "To Protected";
                if (hasRedundantModifiers) title += " (Remove redundant modifiers)";
                else if (hasProtectedInternalKeywords) title += " (only)";

                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = title,
                    NewModifiers = new[] { SyntaxFactory.Token(SyntaxKind.ProtectedKeyword) }
                }));
            }

            if (mainModifierCount > 1 || !hasInternalKeyword || hasProtectedKeyword)
            {
                var title = "To Internal";
                if (hasRedundantModifiers) title += " (Remove redundant modifiers)";
                else if (hasProtectedInternalKeywords) title += " (only)";

                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = title,
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.InternalKeyword)}
                }));
            }

            if (mainModifierCount > 1 || !hasPrivateKeyword)
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Private" + (hasRedundantModifiers ? " (Remove redundant modifiers)" : ""),
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.PrivateKeyword)}
                }));
            }

            if (mainModifierCount > 2 || !hasProtectedInternalKeywords)
            {
                context.RegisterRefactoring(new ChangeModifierAction(new ChangeModifierContext
                {
                    Context = context,
                    Title = "To Protected Internal" + (hasRedundantModifiers ? " (Remove redundant modifiers)" : ""),
                    NewModifiers = new[] {SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.InternalKeyword)}
                }));
            }
        }

        private bool IsTypeDeclaration(SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                    return true;
                default:
                    return false;
            }
        }
    }
}
