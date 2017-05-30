using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotCommands.RefactoringProviders;
using Microsoft.CodeAnalysis.CodeRefactorings;
using TestHelper;
using Xunit;

namespace HotRefactorings.Tests
{
    public class AddNewlineBetweenUsingsGroupsTests : CodeRefactoringVerifier
    {
        [Fact]
        public void NamespacesWithoutDots()
        {
            var oldSource =
@"using System;
using Microsoft;

class Class1
{
}";
            var newSource =
@"using System;

using Microsoft;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        [Fact]
        public void NamespaceWithSingleDot()
        {
            var oldSource =
@"using System;
using System.Text;
using Microsoft;

class Class1
{
}";
            var newSource =
@"using System;
using System.Text;

using Microsoft;

class Class1
{
}";

            VerifyRefactoring(oldSource, newSource, 0, "Add newline betweeen using groups");
        }

        protected override CodeRefactoringProvider GetCodeRefactoringProvider()
        {
            return new AddNewlineBetweenUsingsGroups();
        }
    }
}