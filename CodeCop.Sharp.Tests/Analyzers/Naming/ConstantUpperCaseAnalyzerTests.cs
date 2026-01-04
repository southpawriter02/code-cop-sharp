using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.Analyzers.Naming.ConstantUpperCaseAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<CodeCop.Sharp.Analyzers.Naming.ConstantUpperCaseAnalyzer, CodeCop.Sharp.CodeFixes.Naming.ConstantUpperCaseCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Naming
{
    /// <summary>
    /// Tests for CCS0005 (ConstantUpperCase) analyzer and code fix.
    /// </summary>
    public class ConstantUpperCaseAnalyzerTests
    {
        #region Analyzer Tests - Should Trigger Diagnostic

        [Fact]
        public async Task Const_CamelCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int {|#0:maxSize|} = 100;
}";

            var expected = VerifyCS.Diagnostic("CCS0005").WithLocation(0).WithArguments("maxSize", "MaxSize");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task Const_SingleLowercaseLetter_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int {|#0:x|} = 1;
}";

            var expected = VerifyCS.Diagnostic("CCS0005").WithLocation(0).WithArguments("x", "X");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task PublicConst_CamelCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public const string {|#0:apiUrl|} = ""https://api.example.com"";
}";

            var expected = VerifyCS.Diagnostic("CCS0005").WithLocation(0).WithArguments("apiUrl", "ApiUrl");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InternalConst_CamelCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    internal const int {|#0:defaultTimeout|} = 30;
}";

            var expected = VerifyCS.Diagnostic("CCS0005").WithLocation(0).WithArguments("defaultTimeout", "DefaultTimeout");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MultipleConsts_MixedCase_ShouldTriggerForViolations()
        {
            var testCode = @"
public class MyClass
{
    private const int {|#0:a|} = 1, B = 2, {|#1:c|} = 3;
}";

            var expected1 = VerifyCS.Diagnostic("CCS0005").WithLocation(0).WithArguments("a", "A");
            var expected2 = VerifyCS.Diagnostic("CCS0005").WithLocation(1).WithArguments("c", "C");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected1, expected2);
        }

        [Fact]
        public async Task NestedClass_Const_CamelCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class OuterClass
{
    public class InnerClass
    {
        private const int {|#0:innerConst|} = 42;
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0005").WithLocation(0).WithArguments("innerConst", "InnerConst");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        #endregion

        #region Analyzer Tests - Should NOT Trigger Diagnostic

        [Fact]
        public async Task Const_PascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int MaxSize = 100;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task Const_UpperSnakeCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int MAX_SIZE = 100;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task Const_SingleUppercaseLetter_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int X = 1;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task Const_MixedCase_StartsWithUpper_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int HTTPCode = 200;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task StaticReadonly_CamelCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private static readonly int maxSize = 100;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PrivateField_CamelCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int count = 0;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MultipleConsts_AllValid_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int A = 1, B = 2, C = 3;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task Const_AllUpperCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const string API_KEY = ""secret"";
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        #endregion

        #region Code Fix Tests - PascalCase

        [Fact]
        public async Task CodeFix_RenamesConstToPascalCase()
        {
            var testCode = @"
public class MyClass
{
    private const int {|#0:maxSize|}  = 100;
}";

            var fixedCode = @"
public class MyClass
{
    private const int MaxSize  = 100;
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0005").WithLocation(0).WithArguments("maxSize", "MaxSize");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesConstAndUpdatesUsages()
        {
            var testCode = @"
public class MyClass
{
    private const int {|#0:maxSize|} = 100;

    public int GetMax()
    {
        return maxSize;
    }
}";

            var fixedCode = @"
public class MyClass
{
    private const int MaxSize = 100;

    public int GetMax()
    {
        return MaxSize;
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0005").WithLocation(0).WithArguments("maxSize", "MaxSize");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesPublicConst()
        {
            var testCode = @"
public class MyClass
{
    public const string {|#0:apiKey|} = ""key"";
}";

            var fixedCode = @"
public class MyClass
{
    public const string ApiKey = ""key"";
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0005").WithLocation(0).WithArguments("apiKey", "ApiKey");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        #endregion
    }
}
