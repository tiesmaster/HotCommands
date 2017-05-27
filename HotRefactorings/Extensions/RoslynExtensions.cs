using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotCommands
{
    internal static class RoslynExtensions
    {
        internal static bool IsNested (this SyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException("Object reference not set to an instance of an object");

            return node.Ancestors().Any() && node.Ancestors().Any(n => n.IsKind(SyntaxKind.ClassDeclaration) || n.IsKind(SyntaxKind.StructDeclaration));
        }

        internal static async Task<Document> RemoveUnusedUsings (this Document document, CancellationToken cancellationToken)
        {
            if (document == null)
                throw new ArgumentNullException("Object reference not set to an instance of an object");

            SyntaxNode root = await document.GetSyntaxRootAsync();
            SemanticModel semanticModel = await document.GetSemanticModelAsync();

            root = RemoveUnusedUsings(semanticModel, root, cancellationToken);

            document = document.WithSyntaxRoot(root);
            return document;
        }

        internal static Document FindDocument (this Project project, string name, string[] folders)
        {
            return project.Documents.Where(d => d.Name.ToLowerInvariant() == name.ToLowerInvariant() && d.Folders.Count == folders.Length)
                .FirstOrDefault(d => d.Folders.Zip(folders,
                (f, s) => string.Compare(f, s, StringComparison.OrdinalIgnoreCase) == 0).All(f => f));
        }

        internal static string Namespace (this BaseTypeDeclarationSyntax node)
        {
            var ns = node.Ancestors().OfType<NamespaceDeclarationSyntax>();
            if (!ns.Any()) return string.Empty;

            return ns.Reverse().ToList().Select(n => n.Name.ToString()).Join(".");
        }
        
        private static SyntaxNode RemoveUnusedUsings (SemanticModel semanticModel, SyntaxNode root, CancellationToken cancellationToken = default(CancellationToken))
        {
            // find unused usings
            IEnumerable<SyntaxNode> unusedUsings = GetUnusedUsings(semanticModel);

            // find old and new usings
            IEnumerable<SyntaxNode> oldUsings = root.DescendantNodesAndSelf().Where(s => s is UsingDirectiveSyntax);
            SyntaxTriviaList leadingTrivia = root.GetLeadingTrivia();

            // remove ununsed usings
            root = root.RemoveNodes(oldUsings, SyntaxRemoveOptions.KeepNoTrivia);
            SyntaxList<SyntaxNode> newUsings = SyntaxFactory.List(oldUsings.Except(unusedUsings));

            root = ((CompilationUnitSyntax)root).WithUsings(newUsings).WithLeadingTrivia(leadingTrivia);

            return root;
        }

        private static IEnumerable<SyntaxNode> GetUnusedUsings (SemanticModel model)
        {
            SyntaxNode root = model.SyntaxTree.GetRoot();

            // find unused Usings with the help of compiler warning code
            List<SyntaxNode> unusedUsings = new List<SyntaxNode>();
            foreach (var diagnostic in model.GetDiagnostics(null).Where(d => d.Id == "CS8019" || d.Id == "CS0105"))
            {
                var usingSyntax = root.FindNode(diagnostic.Location.SourceSpan, false, false) as UsingDirectiveSyntax;
                if (usingSyntax != null)
                {
                    unusedUsings.Add(usingSyntax);
                }
            }

            return unusedUsings;
        }

        public static SyntaxTokenList GetModifierTokens(this SyntaxNode declaration)
        {
            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return ((ClassDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.StructDeclaration:
                    return ((StructDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.InterfaceDeclaration:
                    return ((InterfaceDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.MethodDeclaration:
                    return ((MethodDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.OperatorDeclaration:
                    return ((OperatorDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.ConversionOperatorDeclaration:
                    return ((ConversionOperatorDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.ConstructorDeclaration:
                    return ((ConstructorDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.DestructorDeclaration:
                    return ((DestructorDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.FieldDeclaration:
                    return ((FieldDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.PropertyDeclaration:
                    return ((PropertyDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.IndexerDeclaration:
                    return ((IndexerDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.EventFieldDeclaration:
                    return ((EventFieldDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.EventDeclaration:
                    return ((EventDeclarationSyntax)declaration).Modifiers;
                case SyntaxKind.Parameter:
                    return ((ParameterSyntax)declaration).Modifiers;
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)declaration).Modifiers;
                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.VariableDeclarator:
                    if (declaration.Parent != null)
                    {
                        return GetModifierTokens(declaration.Parent);
                    }
                    break;
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return ((AccessorDeclarationSyntax)declaration).Modifiers;
            }

            return default(SyntaxTokenList);
        }
    }
}
