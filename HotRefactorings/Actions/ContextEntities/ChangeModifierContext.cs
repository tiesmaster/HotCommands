using Microsoft.CodeAnalysis;

namespace HotCommands
{
    internal sealed class ChangeModifierContext
    {
        public SyntaxToken[] NewModifiers { get; set; }

        public string Title { get; set; }
    }
}