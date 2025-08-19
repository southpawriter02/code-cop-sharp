using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.MethodDeclarationAnalyzer>;

namespace CodeCop.Sharp.Tests
{
    public class MethodDeclarationAnalyzerTests
    {
        [Fact]
        public async Task MethodDeclaration_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void {|#0:MyMethod|}()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("MyMethod");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
    }
}
