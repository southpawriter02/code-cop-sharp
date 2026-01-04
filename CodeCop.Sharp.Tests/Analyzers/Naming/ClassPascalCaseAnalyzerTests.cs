using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.Analyzers.Naming.ClassPascalCaseAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<CodeCop.Sharp.Analyzers.Naming.ClassPascalCaseAnalyzer, CodeCop.Sharp.CodeFixes.Naming.ClassPascalCaseCodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Naming
{
    /// <summary>
    /// Tests for CCS0002 (ClassPascalCase) analyzer and code fix.
    /// </summary>
    public class ClassPascalCaseAnalyzerTests
    {
        #region Analyzer Tests - Should Trigger Diagnostic

        [Fact]
        public async Task ClassName_StartsWithLowercase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:myClass|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("myClass", "MyClass");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ClassName_SingleLowercaseLetter_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:c|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("c", "C");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task ClassName_WithNumbers_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:user2FAHandler|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("user2FAHandler", "User2FAHandler");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MultipleViolations_ShouldTriggerMultipleDiagnostics()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:firstClass|}
    {
    }

    public class {|#1:secondClass|}
    {
    }
}";

            var expected1 = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("firstClass", "FirstClass");
            var expected2 = VerifyCS.Diagnostic("CCS0002").WithLocation(1).WithArguments("secondClass", "SecondClass");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected1, expected2);
        }

        [Fact]
        public async Task NestedClass_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class OuterClass
    {
        public class {|#0:innerClass|}
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("innerClass", "InnerClass");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task GenericClass_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:genericContainer|}<T>
    {
        public T Value { get; set; }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("genericContainer", "GenericContainer");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task AbstractClass_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public abstract class {|#0:baseHandler|}
    {
        public abstract void Handle();
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("baseHandler", "BaseHandler");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task StaticClass_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public static class {|#0:utilities|}
    {
        public static void DoWork() { }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("utilities", "Utilities");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task PartialClass_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public partial class {|#0:dataModel|}
    {
        public int Id { get; set; }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("dataModel", "DataModel");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task SealedClass_LowercaseStart_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public sealed class {|#0:singletonService|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0002").WithLocation(0).WithArguments("singletonService", "SingletonService");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        #endregion

        #region Analyzer Tests - Should NOT Trigger Diagnostic

        [Fact]
        public async Task ClassName_StartsWithUppercase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class MyClass
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ClassName_SingleUppercaseLetter_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class C
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ClassName_StartsWithUnderscore_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    internal class _InternalClass
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ClassName_WithNumbers_UppercaseStart_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class User2FAHandler
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task ClassName_AllUppercase_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class API
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task GenericClass_UppercaseStart_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class GenericContainer<T>
    {
        public T Value { get; set; }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task NestedClass_UppercaseStart_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class OuterClass
    {
        public class InnerClass
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        #endregion

        #region Code Fix Tests

        [Fact]
        public async Task CodeFix_RenamesClassToPascalCase()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:myClass|}
    {
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public class MyClass
    {
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0002").WithLocation(0).WithArguments("myClass", "MyClass");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesClassAndUpdatesUsages()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:myClass|}
    {
        public int Value { get; set; }
    }

    public class Consumer
    {
        public void Use()
        {
            var instance = new myClass();
            instance.Value = 42;
        }
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public class MyClass
    {
        public int Value { get; set; }
    }

    public class Consumer
    {
        public void Use()
        {
            var instance = new MyClass();
            instance.Value = 42;
        }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0002").WithLocation(0).WithArguments("myClass", "MyClass");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesClassAndUpdatesInheritance()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:baseClass|}
    {
        public virtual void DoWork() { }
    }

    public class DerivedClass : baseClass
    {
        public override void DoWork() { }
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public class BaseClass
    {
        public virtual void DoWork() { }
    }

    public class DerivedClass : BaseClass
    {
        public override void DoWork() { }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0002").WithLocation(0).WithArguments("baseClass", "BaseClass");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesGenericClass()
        {
            var testCode = @"
namespace MyCode
{
    public class {|#0:container|}<T>
    {
        public T Value { get; set; }
    }

    public class Usage
    {
        public void Use()
        {
            var c = new container<int>();
        }
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public class Container<T>
    {
        public T Value { get; set; }
    }

    public class Usage
    {
        public void Use()
        {
            var c = new Container<int>();
        }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0002").WithLocation(0).WithArguments("container", "Container");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesNestedClass()
        {
            var testCode = @"
namespace MyCode
{
    public class Outer
    {
        public class {|#0:inner|}
        {
        }

        public void Use()
        {
            var x = new inner();
        }
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public class Outer
    {
        public class Inner
        {
        }

        public void Use()
        {
            var x = new Inner();
        }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0002").WithLocation(0).WithArguments("inner", "Inner");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        #endregion
    }
}
