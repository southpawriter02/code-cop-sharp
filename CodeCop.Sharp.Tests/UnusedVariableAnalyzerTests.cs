using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.UnusedVariableAnalyzer>;

namespace CodeCop.Sharp.Tests
{
    public class UnusedVariableAnalyzerTests
    {
        [Fact]
        public async Task UnusedVariable_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void MyMethod()
        {
            int {|#0:unusedVar|};
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("unusedVar");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task AssignedButUnusedVariable_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void MyMethod()
        {
            int {|#0:unusedVar|} = 5;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("unusedVar");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task UsedVariable_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public int MyMethod()
        {
            int usedVar = 5;
            return usedVar;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task UnderscoreVariable_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void MyMethod()
        {
            int _ = 5;
            int _unused = 10;
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task UsedVariableInExpressionBody_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
using System;
namespace MyCode
{
    public class MyClass
    {
        public int MyMethod() => new Func<int>(() => { int x = 1; return x; })();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task UnusedVariableInExpressionBody_ShouldTriggerDiagnostic()
        {
            var testCode = @"
using System;
namespace MyCode
{
    public class MyClass
    {
        public int MyMethod() => new Func<int>(() => { int {|#0:x|} = 1; return 1; })();
    }
}";
            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("x");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
