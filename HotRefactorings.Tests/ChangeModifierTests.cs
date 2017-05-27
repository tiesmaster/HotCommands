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

        private static string CreateClassWithModifier(string modifier)
        {
            return $@"{modifier} class Class1
{{
}}";
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new ChangeModifier();
        }
    }
}