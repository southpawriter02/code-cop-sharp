using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.Analyzers.Naming.MethodDeclarationAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<CodeCop.Sharp.Analyzers.Naming.MethodDeclarationAnalyzer, CodeCop.Sharp.CodeFixes.Naming.MethodDeclarationCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Naming
{
    /// <summary>
    /// Tests for CCS0001 (MethodPascalCase) analyzer and code fix.
    /// </summary>
    public class MethodDeclarationAnalyzerTests
    {
        #region Analyzer Tests - Should Trigger Diagnostic

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
        public async Task AsyncMethod_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
using System.Threading.Tasks;

namespace MyCode
{
    public class MyClass
    {
        public async Task {|#0:myMethodAsync|}()
        {
            await Task.CompletedTask;
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("myMethodAsync", "MyMethodAsync");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task GenericMethod_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public T {|#0:getValue|}<T>()
        {
            return default(T);
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("getValue", "GetValue");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ExtensionMethod_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public static class Extensions
    {
        public static string {|#0:toUpperCase|}(this string s)
        {
            return s.ToUpper();
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("toUpperCase", "ToUpperCase");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task NestedClassMethod_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class OuterClass
    {
        public class InnerClass
        {
            public void {|#0:innerMethod|}()
            {
            }
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0001").WithLocation(0).WithArguments("innerMethod", "InnerMethod");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        #endregion

        #region Analyzer Tests - Should NOT Trigger Diagnostic

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
        public async Task MethodName_AllUppercase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
        public void GET()
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        #endregion

        #region Code Fix Tests

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
        public async Task CodeFix_RenamesAsyncMethod()
        {
            var testCode = @"
using System.Threading.Tasks;

namespace MyCode
{
    public class MyClass
    {
        public async Task {|#0:loadDataAsync|}()
        {
            await Task.CompletedTask;
        }
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;

namespace MyCode
{
    public class MyClass
    {
        public async Task LoadDataAsync()
        {
            await Task.CompletedTask;
        }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0001").WithLocation(0).WithArguments("loadDataAsync", "LoadDataAsync");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        #endregion
    }
}
