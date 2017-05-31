using HotCommands;
using Xunit;
using Microsoft.CodeAnalysis.CodeRefactorings;
using TestHelper;

namespace HotRefactorings.Tests
{
    public class ChangeModifierTests : CodeRefactoringVerifier
    {
        [Theory]
        [InlineData("public", "protected", "To Protected")]
        [InlineData("public", "internal", "To Internal")]
        [InlineData("public", "private", "To Private")]
        [InlineData("public", "protected internal", "To Protected Internal")]
        public void FromToModifierTheory(string fromModifier, string toModifier, string refactoringTitle)
        {
            var oldSource = CreateClassWithModifier(fromModifier);
            var newSource = CreateClassWithModifier(toModifier);

            VerifyRefactoring(oldSource, newSource, 0, refactoringTitle);
        }

        [Theory]
        [InlineData("public", new[] { "To Protected", "To Internal", "To Private", "To Protected Internal" })]
        [InlineData("protected", new[] { "To Public", "To Internal", "To Private", "To Protected Internal" })]
        [InlineData("internal", new[] { "To Public", "To Protected", "To Private", "To Protected Internal" })]
        [InlineData("private", new[] { "To Public", "To Protected", "To Internal", "To Protected Internal" })]
        [InlineData("protected internal", new[] { "To Public", "To Protected (only)", "To Internal (only)", "To Private" })]
        public void GivenAccessibilityThenActionsPresentTheory(string fromModifier, string[] expectedRefactorings)
        {
            var source = CreateClassWithModifier(fromModifier);

            VerifyRefactoringPresent(source, 0, expectedRefactorings);
        }

        [Fact]
        public void ProtectedInternalToPublicTest()
        {
            var oldSource = CreateClassWithModifier("protected internal");
            var newSource = CreateClassWithModifier("public");
            VerifyRefactoring(oldSource, newSource, 0, "To Public");
        }

        [Fact]
        public void ProtectedInternalToProtectedOnlyTest()
        {
            var oldSource = CreateClassWithModifier("protected internal");
            var newSource = CreateClassWithModifier("protected");
            VerifyRefactoring(oldSource, newSource, 0, "To Protected (only)");
        }

        [Fact]
        public void ToPublicFromRedundantModifiersTest()
        {
            var oldSource = CreateClassWithModifier("public private");
            var newSource = CreateClassWithModifier("public");
            VerifyRefactoring(oldSource, newSource, 0, "To Public (Remove redundant modifiers)");
        }

        [Fact]
        public void PublicStaticClassToInternalShouldKeepStatic()
        {
            var oldSource = CreateClassWithModifier("public static");
            var newSource = CreateClassWithModifier("internal static");
            VerifyRefactoring(oldSource, newSource, 0, "To Internal");
        }

        [Fact]
        public void ClassWithCustomFormattingShouldNotBeLostAfterApplyRefactoring()
        {
            var oldSource = "public class Class1 { }";
            var newSource = "internal class Class1 { }";

            VerifyRefactoring(oldSource, newSource, 0, "To Internal");
        }

        [Fact]
        public void ToInternalOnOuterClass()
        {
            var oldSource =
@"public class OuterClass
{
    private class InnerClass
    {
    }
}";

            var newSource =
@"internal class OuterClass
{
    private class InnerClass
    {
    }
}";

            VerifyRefactoring(oldSource, newSource, 0, "To Internal");
        }

        [Fact]
        public void ToInternalOnInnerClass()
        {
            var oldSource =
@"public class OuterClass
{
    private class InnerClass
    {
    }
}";

            var newSource =
@"public class OuterClass
{
    internal class InnerClass
    {
    }
}";

            VerifyRefactoring(oldSource, newSource, 30, "To Internal");
        }

        [Theory]
        [InlineData("enum")]
        [InlineData("struct")]
        [InlineData("interface")]
        public void ToInternalOnTypeDeclarationTheory(string typeKeyword)
        {
            var typeName = typeKeyword + "1";

            var fromModifier = "internal";
            var toModifier = "public";

            var oldSource = CreateTypeWithModifier(typeKeyword, typeName, fromModifier);
            var newSource = CreateTypeWithModifier(typeKeyword, typeName, toModifier);

            VerifyRefactoring(oldSource, newSource, 0, "To Public");
        }

        private static string CreateClassWithModifier(string modifier)
            => CreateTypeWithModifier("class", "Class1", modifier);

        private static string CreateTypeWithModifier(string typeKeyword, string typeName, string modifier)
        {
            return $@"{modifier} {typeKeyword} {typeName}
{{
}}";
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new ChangeModifier();
        }
    }
}