using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.Analyzers.Naming.PrivateFieldCamelCaseAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<CodeCop.Sharp.Analyzers.Naming.PrivateFieldCamelCaseAnalyzer, CodeCop.Sharp.CodeFixes.Naming.PrivateFieldCamelCaseCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Naming
{
    /// <summary>
    /// Tests for CCS0004 (PrivateFieldCamelCase) analyzer and code fix.
    /// </summary>
    public class PrivateFieldCamelCaseAnalyzerTests
    {
        #region Analyzer Tests - Should Trigger Diagnostic

        [Fact]
        public async Task PrivateField_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:Count|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task FieldNoModifier_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    int {|#0:Count|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InternalField_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    internal int {|#0:Count|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task PrivateProtectedField_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private protected int {|#0:Count|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task PrivateStaticField_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private static int {|#0:Count|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task PrivateReadonlyField_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private readonly int {|#0:Count|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task PrivateField_AllCaps_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:NUMBER|};
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("NUMBER", "nUMBER");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MultipleVariables_MixedCase_ShouldTriggerForViolations()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:A|}, b, {|#1:C|};
}";

            var expected1 = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("A", "a");
            var expected2 = VerifyCS.Diagnostic("CCS0004").WithLocation(1).WithArguments("C", "c");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected1, expected2);
        }

        [Fact]
        public async Task NestedClass_PrivateField_PascalCase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
public class OuterClass
{
    public class InnerClass
    {
        private int {|#0:Count|};
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        #endregion

        #region Analyzer Tests - Should NOT Trigger Diagnostic

        [Fact]
        public async Task PrivateField_CamelCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PrivateField_UnderscorePrefix_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int _count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PrivateField_UnderscorePascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int _Count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PublicField_PascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    public int Count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ProtectedField_PascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    protected int Count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ProtectedInternalField_PascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    protected internal int Count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PrivateConstField_PascalCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int MaxCount = 100;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PrivateConstField_UpperSnakeCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private const int MAX_COUNT = 100;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task MultipleVariables_AllCamelCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private int a, b, c;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task PrivateReadonlyField_CamelCase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
public class MyClass
{
    private readonly int count;
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        #endregion

        #region Code Fix Tests

        [Fact]
        public async Task CodeFix_RenamesFieldToCamelCase()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:Count|};
}";

            var fixedCode = @"
public class MyClass
{
    private int count;
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesFieldAndUpdatesUsages()
        {
            var testCode = @"
public class MyClass
{
    private int {|#0:Count|};

    public void Increment()
    {
        Count++;
    }

    public int GetCount()
    {
        return Count;
    }
}";

            var fixedCode = @"
public class MyClass
{
    private int count;

    public void Increment()
    {
        count++;
    }

    public int GetCount()
    {
        return count;
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0004").WithLocation(0).WithArguments("Count", "count");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesStaticField()
        {
            var testCode = @"
public class MyClass
{
    private static int {|#0:InstanceCount|};

    public static void Increment()
    {
        InstanceCount++;
    }
}";

            var fixedCode = @"
public class MyClass
{
    private static int instanceCount;

    public static void Increment()
    {
        instanceCount++;
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0004").WithLocation(0).WithArguments("InstanceCount", "instanceCount");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesInternalField()
        {
            var testCode = @"
public class MyClass
{
    internal int {|#0:Value|};
}";

            var fixedCode = @"
public class MyClass
{
    internal int value;
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0004").WithLocation(0).WithArguments("Value", "value");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        #endregion
    }
}
