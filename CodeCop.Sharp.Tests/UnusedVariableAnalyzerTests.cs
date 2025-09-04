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
            int {|#0:x|} = 0;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("x");
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
        public void MyMethod()
        {
            int x = 0;
            System.Console.WriteLine(x);
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }
    }
}
