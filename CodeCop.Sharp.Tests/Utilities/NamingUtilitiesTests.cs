using CodeCop.Sharp.Utilities;
using Xunit;

namespace CodeCop.Sharp.Tests.Utilities
{
    /// <summary>
    /// Unit tests for <see cref="NamingUtilities"/>.
    /// </summary>
    public class NamingUtilitiesTests
    {
        #region ToPascalCase Tests

        [Theory]
        [InlineData("myMethod", "MyMethod")]
        [InlineData("myField", "MyField")]
        [InlineData("m", "M")]
        [InlineData("myMethod123", "MyMethod123")]
        [InlineData("get2FACode", "Get2FACode")]
        public void ToPascalCase_LowercaseStart_ReturnsUppercaseStart(string input, string expected)
        {
            var result = NamingUtilities.ToPascalCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MyMethod", "MyMethod")]
        [InlineData("MyField", "MyField")]
        [InlineData("M", "M")]
        [InlineData("XMLParser", "XMLParser")]
        public void ToPascalCase_AlreadyPascalCase_ReturnsSame(string input, string expected)
        {
            var result = NamingUtilities.ToPascalCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("_myField", "_myField")]
        [InlineData("_MyField", "_MyField")]
        [InlineData("123abc", "123abc")]
        public void ToPascalCase_NonLetterStart_ReturnsSame(string input, string expected)
        {
            var result = NamingUtilities.ToPascalCase(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToPascalCase_Null_ReturnsNull()
        {
            Assert.Null(NamingUtilities.ToPascalCase(null));
        }

        [Fact]
        public void ToPascalCase_Empty_ReturnsEmpty()
        {
            Assert.Equal("", NamingUtilities.ToPascalCase(""));
        }

        #endregion

        #region ToCamelCase Tests

        [Theory]
        [InlineData("MyField", "myField")]
        [InlineData("MyMethod", "myMethod")]
        [InlineData("M", "m")]
        [InlineData("MyField123", "myField123")]
        [InlineData("XMLParser", "xMLParser")]
        public void ToCamelCase_UppercaseStart_ReturnsLowercaseStart(string input, string expected)
        {
            var result = NamingUtilities.ToCamelCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("myField", "myField")]
        [InlineData("myMethod", "myMethod")]
        [InlineData("m", "m")]
        public void ToCamelCase_AlreadyCamelCase_ReturnsSame(string input, string expected)
        {
            var result = NamingUtilities.ToCamelCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("_MyField", "_MyField")]
        [InlineData("_myField", "_myField")]
        [InlineData("123Abc", "123Abc")]
        public void ToCamelCase_NonLetterStart_ReturnsSame(string input, string expected)
        {
            var result = NamingUtilities.ToCamelCase(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToCamelCase_Null_ReturnsNull()
        {
            Assert.Null(NamingUtilities.ToCamelCase(null));
        }

        [Fact]
        public void ToCamelCase_Empty_ReturnsEmpty()
        {
            Assert.Equal("", NamingUtilities.ToCamelCase(""));
        }

        #endregion

        #region ToUpperSnakeCase Tests

        [Theory]
        [InlineData("maxSize", "MAX_SIZE")]
        [InlineData("MaxSize", "MAX_SIZE")]
        [InlineData("apiKey", "API_KEY")]
        [InlineData("myHTTPClient", "MY_HTTPCLIENT")]
        [InlineData("getUser", "GET_USER")]
        public void ToUpperSnakeCase_CamelOrPascal_ConvertsToUpperSnake(string input, string expected)
        {
            var result = NamingUtilities.ToUpperSnakeCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("HTTPClient", "HTTPCLIENT")]
        [InlineData("XMLParser", "XMLPARSER")]
        [InlineData("URL", "URL")]
        public void ToUpperSnakeCase_Acronyms_NoExtraUnderscores(string input, string expected)
        {
            var result = NamingUtilities.ToUpperSnakeCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MAX_SIZE", "MAX_SIZE")]
        [InlineData("API_KEY", "API_KEY")]
        public void ToUpperSnakeCase_AlreadyUpperSnake_ReturnsSame(string input, string expected)
        {
            var result = NamingUtilities.ToUpperSnakeCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("max_size", "MAX_SIZE")]
        [InlineData("api_key", "API_KEY")]
        public void ToUpperSnakeCase_LowerSnake_ConvertsToUpper(string input, string expected)
        {
            var result = NamingUtilities.ToUpperSnakeCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("x", "X")]
        [InlineData("X", "X")]
        public void ToUpperSnakeCase_SingleChar_ConvertsToUpper(string input, string expected)
        {
            var result = NamingUtilities.ToUpperSnakeCase(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToUpperSnakeCase_Null_ReturnsNull()
        {
            Assert.Null(NamingUtilities.ToUpperSnakeCase(null));
        }

        [Fact]
        public void ToUpperSnakeCase_Empty_ReturnsEmpty()
        {
            Assert.Equal("", NamingUtilities.ToUpperSnakeCase(""));
        }

        #endregion

        #region StartsWithUpperCase Tests

        [Theory]
        [InlineData("MyClass", true)]
        [InlineData("XMLParser", true)]
        [InlineData("A", true)]
        public void StartsWithUpperCase_UppercaseFirst_ReturnsTrue(string input, bool expected)
        {
            var result = NamingUtilities.StartsWithUpperCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("myClass", false)]
        [InlineData("a", false)]
        public void StartsWithUpperCase_LowercaseFirst_ReturnsFalse(string input, bool expected)
        {
            var result = NamingUtilities.StartsWithUpperCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("_MyClass", false)]
        [InlineData("123Class", false)]
        [InlineData("_", false)]
        public void StartsWithUpperCase_NonLetterFirst_ReturnsFalse(string input, bool expected)
        {
            var result = NamingUtilities.StartsWithUpperCase(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void StartsWithUpperCase_Null_ReturnsFalse()
        {
            Assert.False(NamingUtilities.StartsWithUpperCase(null));
        }

        [Fact]
        public void StartsWithUpperCase_Empty_ReturnsFalse()
        {
            Assert.False(NamingUtilities.StartsWithUpperCase(""));
        }

        #endregion

        #region StartsWithLowerCase Tests

        [Theory]
        [InlineData("myClass", true)]
        [InlineData("a", true)]
        [InlineData("xmlParser", true)]
        public void StartsWithLowerCase_LowercaseFirst_ReturnsTrue(string input, bool expected)
        {
            var result = NamingUtilities.StartsWithLowerCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("MyClass", false)]
        [InlineData("A", false)]
        [InlineData("XMLParser", false)]
        public void StartsWithLowerCase_UppercaseFirst_ReturnsFalse(string input, bool expected)
        {
            var result = NamingUtilities.StartsWithLowerCase(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("_myClass", false)]
        [InlineData("123class", false)]
        [InlineData("_", false)]
        public void StartsWithLowerCase_NonLetterFirst_ReturnsFalse(string input, bool expected)
        {
            var result = NamingUtilities.StartsWithLowerCase(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void StartsWithLowerCase_Null_ReturnsFalse()
        {
            Assert.False(NamingUtilities.StartsWithLowerCase(null));
        }

        [Fact]
        public void StartsWithLowerCase_Empty_ReturnsFalse()
        {
            Assert.False(NamingUtilities.StartsWithLowerCase(""));
        }

        #endregion
    }
}
