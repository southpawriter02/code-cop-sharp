using System.Threading.Tasks;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<CodeCop.Sharp.Analyzers.Naming.InterfacePrefixIAnalyzer>;
using VerifyCodeFix = Microsoft.CodeAnalysis.CSharp.Testing.XUnit.CodeFixVerifier<CodeCop.Sharp.Analyzers.Naming.InterfacePrefixIAnalyzer, CodeCop.Sharp.CodeFixes.Naming.InterfacePrefixICodeFixProvider>;

namespace CodeCop.Sharp.Tests.Analyzers.Naming
{
    /// <summary>
    /// Tests for CCS0003 (InterfacePrefixI) analyzer and code fix.
    /// </summary>
    public class InterfacePrefixIAnalyzerTests
    {
        #region Analyzer Tests - Should Trigger Diagnostic

        [Fact]
        public async Task InterfaceName_NoIPrefix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:MyService|}
    {
        void DoWork();
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("MyService", "IMyService");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InterfaceName_LowercaseIPrefix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:iService|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("iService", "IService");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InterfaceName_IFollowedByLowercase_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:Iservice|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("Iservice", "IIservice");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InterfaceName_LowercaseNoPrefix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:service|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("service", "IService");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InterfaceName_UnderscorePrefix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:_IService|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("_IService", "I_IService");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InterfaceName_SingleLowercaseI_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:i|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("i", "I");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task MultipleViolations_ShouldTriggerMultipleDiagnostics()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:FirstService|}
    {
    }

    public interface {|#1:SecondService|}
    {
    }
}";

            var expected1 = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("FirstService", "IFirstService");
            var expected2 = VerifyCS.Diagnostic("CCS0003").WithLocation(1).WithArguments("SecondService", "ISecondService");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected1, expected2);
        }

        [Fact]
        public async Task NestedInterface_NoIPrefix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class OuterClass
    {
        public interface {|#0:Inner|}
        {
        }
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("Inner", "IInner");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task GenericInterface_NoIPrefix_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:Repository|}<T>
    {
        T Get(int id);
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("Repository", "IRepository");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        [Fact]
        public async Task InterfaceName_AllCaps_ShouldTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:API|}
    {
    }
}";

            var expected = VerifyCS.Diagnostic("CCS0003").WithLocation(0).WithArguments("API", "IAPI");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }

        #endregion

        #region Analyzer Tests - Should NOT Trigger Diagnostic

        [Fact]
        public async Task InterfaceName_ValidIPrefix_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface IMyService
    {
        void DoWork();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task InterfaceName_SingleI_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface I
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task InterfaceName_IFollowedByNumber_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface I2Service
    {
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task InterfaceName_IDisposable_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface IDisposable
    {
        void Dispose();
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task GenericInterface_ValidIPrefix_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public interface IRepository<T>
    {
        T Get(int id);
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Fact]
        public async Task NestedInterface_ValidIPrefix_ShouldNotTriggerDiagnostic()
        {
            var testCode = @"
namespace MyCode
{
    public class OuterClass
    {
        public interface IInner
        {
        }
    }
}";

            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        #endregion

        #region Code Fix Tests

        [Fact]
        public async Task CodeFix_AddsIPrefixToInterface()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:MyService|}
    {
        void DoWork();
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public interface IMyService
    {
        void DoWork();
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0003").WithLocation(0).WithArguments("MyService", "IMyService");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_FixesLowercaseI()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:iService|}
    {
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public interface IService
    {
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0003").WithLocation(0).WithArguments("iService", "IService");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_FixesIFollowedByLowercase()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:Iservice|}
    {
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public interface IIservice
    {
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0003").WithLocation(0).WithArguments("Iservice", "IIservice");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesInterfaceAndUpdatesImplementors()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:Service|}
    {
        void DoWork();
    }

    public class MyService : Service
    {
        public void DoWork() { }
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public interface IService
    {
        void DoWork();
    }

    public class MyService : IService
    {
        public void DoWork() { }
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0003").WithLocation(0).WithArguments("Service", "IService");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_RenamesGenericInterface()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:Repository|}<T>
    {
        T Get(int id);
    }

    public class UserRepository : Repository<string>
    {
        public string Get(int id) => null;
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public interface IRepository<T>
    {
        T Get(int id);
    }

    public class UserRepository : IRepository<string>
    {
        public string Get(int id) => null;
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0003").WithLocation(0).WithArguments("Repository", "IRepository");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        [Fact]
        public async Task CodeFix_HandlesLowercaseName()
        {
            var testCode = @"
namespace MyCode
{
    public interface {|#0:service|}
    {
    }
}";

            var fixedCode = @"
namespace MyCode
{
    public interface IService
    {
    }
}";

            var expected = VerifyCodeFix.Diagnostic("CCS0003").WithLocation(0).WithArguments("service", "IService");
            await VerifyCodeFix.VerifyCodeFixAsync(testCode, expected, fixedCode);
        }

        #endregion

        #region SuggestInterfaceName Unit Tests

        [Theory]
        [InlineData("Service", "IService")]
        [InlineData("service", "IService")]
        [InlineData("iService", "IService")]
        [InlineData("Iservice", "IIservice")]
        [InlineData("i", "I")]
        [InlineData("iA", "IA")]
        [InlineData("API", "IAPI")]
        [InlineData("_Service", "I_Service")]
        [InlineData("Inner", "IInner")]
        [InlineData("Image", "IImage")]
        public void SuggestInterfaceName_ReturnsExpectedName(string input, string expected)
        {
            var result = CodeCop.Sharp.Analyzers.Naming.InterfacePrefixIAnalyzer.SuggestInterfaceName(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SuggestInterfaceName_HandlesNullOrEmpty(string input)
        {
            var result = CodeCop.Sharp.Analyzers.Naming.InterfacePrefixIAnalyzer.SuggestInterfaceName(input);
            Assert.Equal(input, result);
        }

        #endregion
    }
}
