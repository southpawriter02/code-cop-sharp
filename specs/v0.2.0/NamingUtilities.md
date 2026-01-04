# NamingUtilities - Shared Naming Conversion Utilities

## Overview

| Property | Value |
|----------|-------|
| Location | `CodeCop.Sharp/Utilities/NamingUtilities.cs` |
| Type | Static utility class |
| Purpose | Shared naming convention conversion methods |

## Description

NamingUtilities provides static methods for converting identifiers between different naming conventions (PascalCase, camelCase, UPPER_SNAKE_CASE). These methods are used by multiple analyzers and code fix providers.

---

## API Reference

### ToPascalCase

```csharp
/// <summary>
/// Converts a string to PascalCase by capitalizing the first character.
/// </summary>
/// <param name="name">The identifier name to convert.</param>
/// <returns>The name with the first character uppercased.</returns>
/// <example>
/// ToPascalCase("myMethod") returns "MyMethod"
/// ToPascalCase("MyMethod") returns "MyMethod"
/// ToPascalCase("m") returns "M"
/// ToPascalCase(null) returns null
/// ToPascalCase("") returns ""
/// </example>
public static string ToPascalCase(string name)
```

### ToCamelCase

```csharp
/// <summary>
/// Converts a string to camelCase by lowercasing the first character.
/// </summary>
/// <param name="name">The identifier name to convert.</param>
/// <returns>The name with the first character lowercased.</returns>
/// <example>
/// ToCamelCase("MyField") returns "myField"
/// ToCamelCase("myField") returns "myField"
/// ToCamelCase("M") returns "m"
/// ToCamelCase(null) returns null
/// ToCamelCase("") returns ""
/// </example>
public static string ToCamelCase(string name)
```

### ToUpperSnakeCase

```csharp
/// <summary>
/// Converts a string to UPPER_SNAKE_CASE.
/// Inserts underscores before uppercase letters (except at the start)
/// and converts all characters to uppercase.
/// </summary>
/// <param name="name">The identifier name to convert.</param>
/// <returns>The name in UPPER_SNAKE_CASE format.</returns>
/// <example>
/// ToUpperSnakeCase("maxSize") returns "MAX_SIZE"
/// ToUpperSnakeCase("MaxSize") returns "MAX_SIZE"
/// ToUpperSnakeCase("apiKey") returns "API_KEY"
/// ToUpperSnakeCase("HTTPClient") returns "HTTPCLIENT"
/// ToUpperSnakeCase("MAX_SIZE") returns "MAX_SIZE"
/// ToUpperSnakeCase(null) returns null
/// ToUpperSnakeCase("") returns ""
/// </example>
public static string ToUpperSnakeCase(string name)
```

### StartsWithUpperCase

```csharp
/// <summary>
/// Checks if a string starts with an uppercase letter.
/// </summary>
/// <param name="name">The identifier name to check.</param>
/// <returns>True if the first character is uppercase; otherwise, false.</returns>
/// <example>
/// StartsWithUpperCase("MyClass") returns true
/// StartsWithUpperCase("myClass") returns false
/// StartsWithUpperCase("_myClass") returns false
/// StartsWithUpperCase("") returns false
/// StartsWithUpperCase(null) returns false
/// </example>
public static bool StartsWithUpperCase(string name)
```

### StartsWithLowerCase

```csharp
/// <summary>
/// Checks if a string starts with a lowercase letter.
/// </summary>
/// <param name="name">The identifier name to check.</param>
/// <returns>True if the first character is lowercase; otherwise, false.</returns>
/// <example>
/// StartsWithLowerCase("myClass") returns true
/// StartsWithLowerCase("MyClass") returns false
/// StartsWithLowerCase("_myClass") returns false
/// StartsWithLowerCase("123abc") returns false
/// StartsWithLowerCase("") returns false
/// StartsWithLowerCase(null) returns false
/// </example>
public static bool StartsWithLowerCase(string name)
```

---

## Implementation

```csharp
using System;
using System.Text;

namespace CodeCop.Sharp.Utilities
{
    /// <summary>
    /// Provides utility methods for converting between naming conventions.
    /// </summary>
    public static class NamingUtilities
    {
        /// <summary>
        /// Converts a string to PascalCase by capitalizing the first character.
        /// </summary>
        public static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (char.IsUpper(name[0]))
            {
                return name; // Already PascalCase
            }

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Converts a string to camelCase by lowercasing the first character.
        /// </summary>
        public static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (char.IsLower(name[0]))
            {
                return name; // Already camelCase
            }

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// Converts a string to UPPER_SNAKE_CASE.
        /// </summary>
        public static string ToUpperSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var result = new StringBuilder(name.Length + 5); // Extra capacity for underscores

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                // Insert underscore before uppercase letters (except first char)
                // but not if the previous character was already uppercase (handles acronyms)
                if (i > 0 && char.IsUpper(c))
                {
                    char prev = name[i - 1];
                    if (!char.IsUpper(prev) && prev != '_')
                    {
                        result.Append('_');
                    }
                }

                result.Append(char.ToUpperInvariant(c));
            }

            return result.ToString();
        }

        /// <summary>
        /// Checks if a string starts with an uppercase letter.
        /// </summary>
        public static bool StartsWithUpperCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return char.IsUpper(name[0]);
        }

        /// <summary>
        /// Checks if a string starts with a lowercase letter.
        /// </summary>
        public static bool StartsWithLowerCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            return char.IsLower(name[0]);
        }
    }
}
```

---

## Test Cases

### ToPascalCase Tests

| Input | Expected Output | Test Name |
|-------|-----------------|-----------|
| `"myMethod"` | `"MyMethod"` | LowercaseFirst_ReturnsUppercaseFirst |
| `"MyMethod"` | `"MyMethod"` | AlreadyPascalCase_ReturnsSame |
| `"m"` | `"M"` | SingleLowerChar_ReturnsUpperChar |
| `"M"` | `"M"` | SingleUpperChar_ReturnsSame |
| `"myMethod123"` | `"MyMethod123"` | WithNumbers_PreservesNumbers |
| `null` | `null` | Null_ReturnsNull |
| `""` | `""` | Empty_ReturnsEmpty |
| `"_myField"` | `"_myField"` | UnderscorePrefix_PreservesUnderscore |

### ToCamelCase Tests

| Input | Expected Output | Test Name |
|-------|-----------------|-----------|
| `"MyField"` | `"myField"` | UppercaseFirst_ReturnsLowercaseFirst |
| `"myField"` | `"myField"` | AlreadyCamelCase_ReturnsSame |
| `"M"` | `"m"` | SingleUpperChar_ReturnsLowerChar |
| `"m"` | `"m"` | SingleLowerChar_ReturnsSame |
| `"MyField123"` | `"myField123"` | WithNumbers_PreservesNumbers |
| `null` | `null` | Null_ReturnsNull |
| `""` | `""` | Empty_ReturnsEmpty |
| `"XMLParser"` | `"xMLParser"` | Acronym_OnlyChangesFirst |

### ToUpperSnakeCase Tests

| Input | Expected Output | Test Name |
|-------|-----------------|-----------|
| `"maxSize"` | `"MAX_SIZE"` | CamelCase_ConvertsToUpperSnake |
| `"MaxSize"` | `"MAX_SIZE"` | PascalCase_ConvertsToUpperSnake |
| `"apiKey"` | `"API_KEY"` | ShortWords_ConvertsCorrectly |
| `"HTTPClient"` | `"HTTPCLIENT"` | Acronym_NoExtraUnderscores |
| `"myHTTPClient"` | `"MY_HTTPCLIENT"` | MixedAcronym_SingleUnderscore |
| `"MAX_SIZE"` | `"MAX_SIZE"` | AlreadyUpperSnake_ReturnsSame |
| `"max_size"` | `"MAX_SIZE"` | LowerSnake_ConvertsToUpper |
| `"x"` | `"X"` | SingleChar_ConvertsToUpper |
| `null` | `null` | Null_ReturnsNull |
| `""` | `""` | Empty_ReturnsEmpty |

### StartsWithUpperCase Tests

| Input | Expected Output | Test Name |
|-------|-----------------|-----------|
| `"MyClass"` | `true` | UppercaseFirst_ReturnsTrue |
| `"myClass"` | `false` | LowercaseFirst_ReturnsFalse |
| `"_MyClass"` | `false` | UnderscoreFirst_ReturnsFalse |
| `"123Class"` | `false` | DigitFirst_ReturnsFalse |
| `null` | `false` | Null_ReturnsFalse |
| `""` | `false` | Empty_ReturnsFalse |

### StartsWithLowerCase Tests

| Input | Expected Output | Test Name |
|-------|-----------------|-----------|
| `"myClass"` | `true` | LowercaseFirst_ReturnsTrue |
| `"MyClass"` | `false` | UppercaseFirst_ReturnsFalse |
| `"_myClass"` | `false` | UnderscoreFirst_ReturnsFalse |
| `"123class"` | `false` | DigitFirst_ReturnsFalse |
| `null` | `false` | Null_ReturnsFalse |
| `""` | `false` | Empty_ReturnsFalse |

---

## Test Implementation

```csharp
using CodeCop.Sharp.Utilities;
using Xunit;

namespace CodeCop.Sharp.Tests.Utilities
{
    public class NamingUtilitiesTests
    {
        #region ToPascalCase Tests

        [Theory]
        [InlineData("myMethod", "MyMethod")]
        [InlineData("MyMethod", "MyMethod")]
        [InlineData("m", "M")]
        [InlineData("M", "M")]
        [InlineData("myMethod123", "MyMethod123")]
        [InlineData("_myField", "_myField")]
        public void ToPascalCase_ValidInput_ReturnsExpected(string input, string expected)
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
        [InlineData("myField", "myField")]
        [InlineData("M", "m")]
        [InlineData("m", "m")]
        [InlineData("MyField123", "myField123")]
        [InlineData("XMLParser", "xMLParser")]
        public void ToCamelCase_ValidInput_ReturnsExpected(string input, string expected)
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
        [InlineData("HTTPClient", "HTTPCLIENT")]
        [InlineData("myHTTPClient", "MY_HTTPCLIENT")]
        [InlineData("MAX_SIZE", "MAX_SIZE")]
        [InlineData("max_size", "MAX_SIZE")]
        [InlineData("x", "X")]
        public void ToUpperSnakeCase_ValidInput_ReturnsExpected(string input, string expected)
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
        [InlineData("myClass", false)]
        [InlineData("_MyClass", false)]
        [InlineData("123Class", false)]
        public void StartsWithUpperCase_ValidInput_ReturnsExpected(string input, bool expected)
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
        [InlineData("MyClass", false)]
        [InlineData("_myClass", false)]
        [InlineData("123class", false)]
        public void StartsWithLowerCase_ValidInput_ReturnsExpected(string input, bool expected)
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
```

---

## Usage Examples

### In Analyzers

```csharp
// CCS0002: ClassPascalCaseAnalyzer
var suggestedName = NamingUtilities.ToPascalCase(className);
if (!NamingUtilities.StartsWithUpperCase(className))
{
    context.ReportDiagnostic(...);
}

// CCS0004: PrivateFieldCamelCaseAnalyzer
if (NamingUtilities.StartsWithUpperCase(fieldName))
{
    var suggestedName = NamingUtilities.ToCamelCase(fieldName);
    context.ReportDiagnostic(...);
}

// CCS0005: ConstantUpperCaseAnalyzer (dual fix)
var pascalName = NamingUtilities.ToPascalCase(constName);
var upperName = NamingUtilities.ToUpperSnakeCase(constName);
```

### In Code Fix Providers

```csharp
// ClassPascalCaseCodeFixProvider
var newName = NamingUtilities.ToPascalCase(className);
context.RegisterCodeFix(
    CodeAction.Create(
        title: $"Rename to '{newName}'",
        ...));
```

---

## Deliverable Checklist

- [ ] Create `Utilities/` directory in CodeCop.Sharp
- [ ] Implement `NamingUtilities.cs`
- [ ] Add XML documentation comments
- [ ] Create `NamingUtilitiesTests.cs` in Tests project
- [ ] Write all test cases (minimum 30 tests)
- [ ] Verify 100% code coverage
- [ ] Refactor existing analyzers to use NamingUtilities

---

## Migration from Existing Code

### Before (MethodDeclarationAnalyzer)

```csharp
public static string ToPascalCase(string name)
{
    if (string.IsNullOrEmpty(name))
    {
        return name;
    }
    return char.ToUpperInvariant(name[0]) + name.Substring(1);
}
```

### After

```csharp
// Remove ToPascalCase from MethodDeclarationAnalyzer
// Add using statement
using CodeCop.Sharp.Utilities;

// Use shared utility
var suggestedName = NamingUtilities.ToPascalCase(methodName);
```

### Update Tests

```csharp
// Before: Testing ToPascalCase in MethodDeclarationAnalyzerTests
[Fact]
public void ToPascalCase_LowercaseStart_ReturnsUppercaseStart()
{
    Assert.Equal("MyMethod", MethodDeclarationAnalyzer.ToPascalCase("myMethod"));
}

// After: Move to NamingUtilitiesTests
[Fact]
public void ToPascalCase_LowercaseStart_ReturnsUppercaseStart()
{
    Assert.Equal("MyMethod", NamingUtilities.ToPascalCase("myMethod"));
}
```
