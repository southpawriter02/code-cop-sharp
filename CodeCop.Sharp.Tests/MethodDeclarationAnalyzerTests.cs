using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.MethodDeclarationAnalyzer>;

namespace CodeCop.Sharp.Tests
{
    public class MethodDeclarationAnalyzerTests
    {
        [Fact]
        public async Task MethodName_ShouldNotBePascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void {|#0:myMethod|}()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("myMethod");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MethodName_ShouldBePascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void MyMethod()
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }
    }
}
