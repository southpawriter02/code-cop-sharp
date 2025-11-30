using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.MethodDeclarationAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<CodeCop.Sharp.MethodDeclarationAnalyzer, CodeCop.Sharp.MethodDeclarationCodeFixProvider>;

namespace CodeCop.Sharp.Tests
{
    public class MethodDeclarationAnalyzerTests
    {
        [Fact]
        public async Task MethodName_StartsWithLowercase_ShouldTriggerDiagnostic()
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

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("myMethod", "MyMethod");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MethodName_StartsWithUppercase_ShouldNotTriggerDiagnostic()
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

        [Fact]
        public async Task MethodName_SingleLowercaseLetter_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void {|#0:m|}()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("m", "M");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MethodName_SingleUppercaseLetter_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void M()
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MethodName_StartsWithUnderscore_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        private void _PrivateMethod()
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MethodName_WithNumbers_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void {|#0:get2FACode|}()
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("get2FACode", "Get2FACode");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MethodName_WithNumbers_UppercaseStart_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void Get2FACode()
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MultipleViolations_ShouldTriggerMultipleDiagnostics()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void {|#0:firstMethod|}()
        {
        }

        public void {|#1:secondMethod|}()
        {
        }
    }
}";

            var expected1 = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("firstMethod", "FirstMethod");
            var expected2 = VerifyCS.Diagnostic("CCS0001").WithLocation(1).WithArguments("secondMethod", "SecondMethod");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected1, expected2);
        }

        [Fact]
        public async Task CodeFix_RenamesMethodToPascalCase()
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

            var fixedCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void MyMethod()
        {
        }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0001").WithLocation(0).WithArguments("myMethod", "MyMethod");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesMethodAndUpdatesCallers()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void {|#0:myMethod|}()
        {
        }

        public void Caller()
        {
            myMethod();
        }
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void MyMethod()
        {
        }

        public void Caller()
        {
            MyMethod();
        }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0001").WithLocation(0).WithArguments("myMethod", "MyMethod");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public void ToPascalCase_LowercaseStart_ReturnsUppercaseStart()
        {
            Assert.Equal("MyMethod", MethodDeclarationAnalyzer.ToPascalCase("myMethod"));
        }

        [Fact]
        public void ToPascalCase_SingleLetter_ReturnsUppercase()
        {
            Assert.Equal("M", MethodDeclarationAnalyzer.ToPascalCase("m"));
        }

        [Fact]
        public void ToPascalCase_EmptyString_ReturnsEmpty()
        {
            Assert.Equal("", MethodDeclarationAnalyzer.ToPascalCase(""));
        }

        [Fact]
        public void ToPascalCase_Null_ReturnsNull()
        {
            Assert.Null(MethodDeclarationAnalyzer.ToPascalCase(null));
        }
    }
}
