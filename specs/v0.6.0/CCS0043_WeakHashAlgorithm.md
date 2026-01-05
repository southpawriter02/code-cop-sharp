# CCS0043: WeakHashAlgorithm

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0043 |
| Category | Security |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |
| CWE References | [CWE-327](https://cwe.mitre.org/data/definitions/327.html), [CWE-328](https://cwe.mitre.org/data/definitions/328.html) |

## Description

Detects usage of weak or broken cryptographic hash algorithms (MD5, SHA1) in security-sensitive contexts. These algorithms have known vulnerabilities and should not be used for security purposes like password hashing, digital signatures, or integrity verification.

### Why This Rule?

1. **Collision Attacks**: MD5 and SHA1 have practical collision attacks
2. **Pre-image Attacks**: Weaknesses allow faster than brute-force attacks
3. **Industry Standards**: NIST deprecated MD5 (2010) and SHA1 (2011) for security use
4. **Compliance**: PCI-DSS, HIPAA, and other standards prohibit weak hashes
5. **Password Security**: Weak hashes enable faster password cracking

### What This Rule Detects

- Direct instantiation of `MD5`, `SHA1` classes
- Factory method calls: `MD5.Create()`, `SHA1.Create()`
- `HashAlgorithm.Create("MD5")`, `HashAlgorithm.Create("SHA1")`
- HMAC variants: `HMACMD5`, `HMACSHA1` (in security contexts)

### Acceptable Uses (Not Flagged)

- Checksum verification (non-security)
- Legacy system interoperability (with suppression)
- File integrity for non-adversarial scenarios

---

## Configuration

```ini
# .editorconfig

# Enable/disable rule
dotnet_diagnostic.CCS0043.severity = warning

# Flag HMAC variants (HMACSHA1 may be acceptable in some protocols)
dotnet_code_quality.CCS0043.flag_hmac = true

# Suggested replacement algorithm
dotnet_code_quality.CCS0043.suggested_algorithm = SHA256
```

---

## Detection Patterns

### Flagged Types

| Type | Namespace | Severity |
|------|-----------|----------|
| `MD5` | System.Security.Cryptography | Warning |
| `MD5CryptoServiceProvider` | System.Security.Cryptography | Warning |
| `MD5Cng` | System.Security.Cryptography | Warning |
| `SHA1` | System.Security.Cryptography | Warning |
| `SHA1CryptoServiceProvider` | System.Security.Cryptography | Warning |
| `SHA1Cng` | System.Security.Cryptography | Warning |
| `SHA1Managed` | System.Security.Cryptography | Warning |
| `HMACMD5` | System.Security.Cryptography | Warning |
| `HMACSHA1` | System.Security.Cryptography | Info (configurable) |

### Detection Methods

| Method | Example | Detected |
|--------|---------|----------|
| Constructor | `new MD5CryptoServiceProvider()` | Yes |
| Factory Create() | `MD5.Create()` | Yes |
| HashAlgorithm.Create | `HashAlgorithm.Create("MD5")` | Yes |
| KeyedHashAlgorithm.Create | `KeyedHashAlgorithm.Create("HMACMD5")` | Yes |
| Type parameter | `ComputeHash<MD5>()` | Yes |

---

## Compliant Examples

```csharp
using System.Security.Cryptography;

// Good - using SHA256
public byte[] ComputeHash(byte[] data)
{
    using var sha256 = SHA256.Create();
    return sha256.ComputeHash(data);
}

// Good - using SHA384
public byte[] ComputeSecureHash(byte[] data)
{
    using var sha384 = SHA384.Create();
    return sha384.ComputeHash(data);
}

// Good - using SHA512
public byte[] ComputeStrongHash(byte[] data)
{
    using var sha512 = SHA512.Create();
    return sha512.ComputeHash(data);
}

// Good - using SHA3 (when available)
public byte[] ComputeSha3Hash(byte[] data)
{
    using var sha3 = SHA3_256.Create();
    return sha3.ComputeHash(data);
}

// Good - using HMACSHA256 for message authentication
public byte[] ComputeHmac(byte[] key, byte[] data)
{
    using var hmac = new HMACSHA256(key);
    return hmac.ComputeHash(data);
}

// Good - using Argon2/bcrypt for passwords (via library)
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password);
}

// Good - MD5 for non-security checksum (with documented suppression)
#pragma warning disable CCS0043 // Legacy checksum compatibility
public string ComputeChecksum(byte[] data)
{
    using var md5 = MD5.Create();
    return Convert.ToHexString(md5.ComputeHash(data));
}
#pragma warning restore CCS0043
```

## Non-Compliant Examples

```csharp
using System.Security.Cryptography;

// CCS0043 - MD5 instantiation
public byte[] HashData(byte[] data)
{
    using var md5 = MD5.Create();  // Weak hash algorithm
    return md5.ComputeHash(data);
}

// CCS0043 - SHA1 instantiation
public byte[] ComputeSha1(byte[] data)
{
    using var sha1 = SHA1.Create();  // Weak hash algorithm
    return sha1.ComputeHash(data);
}

// CCS0043 - MD5 via constructor
public byte[] LegacyHash(byte[] data)
{
    using var md5 = new MD5CryptoServiceProvider();  // Weak hash algorithm
    return md5.ComputeHash(data);
}

// CCS0043 - SHA1 via constructor
public byte[] OldHash(byte[] data)
{
    using var sha1 = new SHA1Managed();  // Weak hash algorithm
    return sha1.ComputeHash(data);
}

// CCS0043 - HashAlgorithm.Create with string
public byte[] CreateHash(byte[] data, string algorithm)
{
    using var hash = HashAlgorithm.Create("MD5");  // Weak hash algorithm
    return hash.ComputeHash(data);
}

// CCS0043 - HMACMD5
public byte[] ComputeMac(byte[] key, byte[] data)
{
    using var hmac = new HMACMD5(key);  // Weak HMAC algorithm
    return hmac.ComputeHash(data);
}

// CCS0043 - Password hashing with MD5
public string HashPassword(string password)
{
    using var md5 = MD5.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    return Convert.ToBase64String(md5.ComputeHash(bytes));
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/Security/WeakHashAlgorithmAnalyzer.cs
└── CodeFixes/Security/WeakHashAlgorithmCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCop.Sharp.Analyzers.Security
{
    /// <summary>
    /// Analyzer that detects usage of weak hash algorithms (MD5, SHA1).
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0043
    /// Category: Security
    /// Severity: Warning
    /// CWE: CWE-327, CWE-328
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WeakHashAlgorithmAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0043";

        private static readonly LocalizableString Title = "Weak hash algorithm detected";
        private static readonly LocalizableString MessageFormat =
            "'{0}' is a weak hash algorithm. Use SHA256 or stronger for security purposes.";
        private static readonly LocalizableString Description =
            "MD5 and SHA1 have known vulnerabilities and should not be used for security purposes.";
        private const string Category = "Security";
        private const string HelpLinkUri = "https://cwe.mitre.org/data/definitions/327.html";

        // Weak hash algorithm types
        private static readonly string[] WeakHashTypes = new[]
        {
            "System.Security.Cryptography.MD5",
            "System.Security.Cryptography.MD5CryptoServiceProvider",
            "System.Security.Cryptography.MD5Cng",
            "System.Security.Cryptography.SHA1",
            "System.Security.Cryptography.SHA1CryptoServiceProvider",
            "System.Security.Cryptography.SHA1Cng",
            "System.Security.Cryptography.SHA1Managed",
            "System.Security.Cryptography.HMACMD5",
            "System.Security.Cryptography.HMACSHA1"
        };

        // Weak algorithm names for HashAlgorithm.Create()
        private static readonly string[] WeakAlgorithmNames = new[]
        {
            "MD5", "SHA1", "HMACMD5", "HMACSHA1",
            "System.Security.Cryptography.MD5",
            "System.Security.Cryptography.SHA1"
        };

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLinkUri,
            customTags: new[] { "Security", "CWE-327", "CWE-328" });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Analyze object creation (new MD5CryptoServiceProvider())
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);

            // Analyze invocation (MD5.Create(), HashAlgorithm.Create("MD5"))
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            var typeInfo = context.SemanticModel.GetTypeInfo(creation);
            if (typeInfo.Type == null)
                return;

            var typeName = typeInfo.Type.ToDisplayString();

            if (IsWeakHashType(typeName))
            {
                var shortName = typeInfo.Type.Name;
                ReportDiagnostic(context, creation.Type.GetLocation(), shortName);
            }
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol method)
                return;

            // Check for Type.Create() patterns (e.g., MD5.Create())
            if (method.Name == "Create" && method.Parameters.Length == 0)
            {
                var containingType = method.ContainingType?.ToDisplayString();
                if (IsWeakHashType(containingType))
                {
                    var shortName = method.ContainingType.Name;
                    ReportDiagnostic(context, invocation.GetLocation(), shortName);
                    return;
                }
            }

            // Check for HashAlgorithm.Create("MD5") pattern
            if (method.Name == "Create" &&
                method.Parameters.Length == 1 &&
                method.ContainingType?.Name == "HashAlgorithm")
            {
                if (invocation.ArgumentList.Arguments.Count == 1 &&
                    invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var algorithmName = literal.Token.ValueText;
                    if (IsWeakAlgorithmName(algorithmName))
                    {
                        ReportDiagnostic(context, invocation.GetLocation(), algorithmName);
                    }
                }
            }

            // Check for KeyedHashAlgorithm.Create("HMACMD5") pattern
            if (method.Name == "Create" &&
                method.Parameters.Length == 1 &&
                method.ContainingType?.Name == "KeyedHashAlgorithm")
            {
                if (invocation.ArgumentList.Arguments.Count == 1 &&
                    invocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
                    literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var algorithmName = literal.Token.ValueText;
                    if (IsWeakAlgorithmName(algorithmName))
                    {
                        ReportDiagnostic(context, invocation.GetLocation(), algorithmName);
                    }
                }
            }
        }

        private static bool IsWeakHashType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            foreach (var weakType in WeakHashTypes)
            {
                if (typeName == weakType)
                    return true;
            }
            return false;
        }

        private static bool IsWeakAlgorithmName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            foreach (var weakName in WeakAlgorithmNames)
            {
                if (string.Equals(name, weakName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, string algorithmName)
        {
            var diagnostic = Diagnostic.Create(Rule, location, algorithmName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

### Code Fix Provider

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCop.Sharp.CodeFixes.Security
{
    /// <summary>
    /// Code fix provider for CCS0043 (WeakHashAlgorithm).
    /// Replaces weak hash algorithms with SHA256.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WeakHashAlgorithmCodeFixProvider)), Shared]
    public class WeakHashAlgorithmCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(WeakHashAlgorithmAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);

            // Register code fix for object creation
            if (node.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>() is ObjectCreationExpressionSyntax creation)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Replace with SHA256",
                        createChangedDocument: c => ReplaceWithSha256Async(context.Document, creation, c),
                        equivalenceKey: "ReplaceWithSHA256"),
                    diagnostic);
            }

            // Register code fix for invocation (Create method)
            if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is InvocationExpressionSyntax invocation)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Replace with SHA256",
                        createChangedDocument: c => ReplaceInvocationWithSha256Async(context.Document, invocation, c),
                        equivalenceKey: "ReplaceWithSHA256"),
                    diagnostic);
            }
        }

        private async Task<Document> ReplaceWithSha256Async(
            Document document,
            ObjectCreationExpressionSyntax creation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Replace with SHA256.Create() invocation
            var newInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("SHA256"),
                    SyntaxFactory.IdentifierName("Create")))
                .WithLeadingTrivia(creation.GetLeadingTrivia())
                .WithTrailingTrivia(creation.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(creation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ReplaceInvocationWithSha256Async(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Replace with SHA256.Create()
            var newInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("SHA256"),
                    SyntaxFactory.IdentifierName("Create")))
                .WithLeadingTrivia(invocation.GetLeadingTrivia())
                .WithTrailingTrivia(invocation.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

---

## Decision Tree

```
                    ┌─────────────────────────────┐
                    │ Is it object creation or    │
                    │ method invocation?          │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
        ┌───────────────────────────────────────────────────┐
        │ OBJECT CREATION                                   │
        │ Is it MD5*, SHA1*, HMACMD5, or HMACSHA1?         │
        └─────────────────────────┬─────────────────────────┘
                                  │
            ┌─────────────────────┼─────────────────────┐
            │                     │                     │
    ┌───────▼───────┐     ┌───────▼───────┐     ┌───────▼───────┐
    │ new MD5*()    │     │ new SHA1*()   │     │ new HMAC*()   │
    └───────┬───────┘     └───────┬───────┘     └───────┬───────┘
            │                     │                     │
            ▼                     ▼                     ▼
      REPORT CCS0043        REPORT CCS0043        REPORT CCS0043

        ┌───────────────────────────────────────────────────┐
        │ METHOD INVOCATION                                 │
        │ Is it Type.Create() or HashAlgorithm.Create()?   │
        └─────────────────────────┬─────────────────────────┘
                                  │
            ┌─────────────────────┼─────────────────────┐
            │                     │                     │
    ┌───────▼───────┐     ┌───────▼───────┐     ┌───────▼───────┐
    │ MD5.Create()  │     │ SHA1.Create() │     │ Create("MD5") │
    └───────┬───────┘     └───────┬───────┘     └───────┬───────┘
            │                     │                     │
            ▼                     ▼                     ▼
      REPORT CCS0043        REPORT CCS0043        REPORT CCS0043
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| MD5Create | `MD5.Create()` | CCS0043 |
| SHA1Create | `SHA1.Create()` | CCS0043 |
| NewMD5CryptoServiceProvider | `new MD5CryptoServiceProvider()` | CCS0043 |
| NewSHA1Managed | `new SHA1Managed()` | CCS0043 |
| NewSHA1Cng | `new SHA1Cng()` | CCS0043 |
| HashAlgorithmCreateMD5 | `HashAlgorithm.Create("MD5")` | CCS0043 |
| HashAlgorithmCreateSHA1 | `HashAlgorithm.Create("SHA1")` | CCS0043 |
| NewHMACMD5 | `new HMACMD5(key)` | CCS0043 |
| NewHMACSHA1 | `new HMACSHA1(key)` | CCS0043 |
| KeyedHashCreateHMAC | `KeyedHashAlgorithm.Create("HMACMD5")` | CCS0043 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| SHA256Create | `SHA256.Create()` | No diagnostic |
| SHA384Create | `SHA384.Create()` | No diagnostic |
| SHA512Create | `SHA512.Create()` | No diagnostic |
| NewHMACSHA256 | `new HMACSHA256(key)` | No diagnostic |
| NewHMACSHA512 | `new HMACSHA512(key)` | No diagnostic |
| HashAlgorithmCreateSHA256 | `HashAlgorithm.Create("SHA256")` | No diagnostic |
| SuppressedMD5 | `#pragma warning disable CCS0043` | No diagnostic |

### Code Fix Tests

| Test Name | Input | Expected Output |
|-----------|-------|-----------------|
| FixMD5Create | `MD5.Create()` | `SHA256.Create()` |
| FixSHA1Create | `SHA1.Create()` | `SHA256.Create()` |
| FixNewMD5 | `new MD5CryptoServiceProvider()` | `SHA256.Create()` |
| FixNewSHA1 | `new SHA1Managed()` | `SHA256.Create()` |

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Generic type parameter | Analyze if constrained | `where T : MD5` should flag |
| Reflection-based creation | Not detected | `Activator.CreateInstance` bypasses |
| Third-party wrappers | Not detected | Only detects BCL types |
| ASP.NET Identity SHA1 | Detected | Legacy but should be updated |
| Git SHA1 interop | Use suppression | Non-security use case |
| HMACSHA1 for TOTP | Configurable | RFC 6238 specifies HMAC-SHA1 |
| Certificate SHA1 | Detected | Should use SHA256+ for certs |

---

## Related Rules

| Rule | Relationship |
|------|--------------|
| CCS0044 | InsecureRandom - Both relate to cryptographic security |
| CA5350 | Do Not Use Weak Cryptographic Algorithms (Roslyn built-in) |

---

## References

- [CWE-327: Use of a Broken or Risky Cryptographic Algorithm](https://cwe.mitre.org/data/definitions/327.html)
- [CWE-328: Reversible One-Way Hash](https://cwe.mitre.org/data/definitions/328.html)
- [NIST SP 800-131A: Transitioning Use of Cryptographic Algorithms](https://csrc.nist.gov/publications/detail/sp/800-131a/rev-2/final)
- [RFC 6151: Updated Security Considerations for MD5](https://www.rfc-editor.org/rfc/rfc6151)

---

## Deliverable Checklist

- [ ] Create `Analyzers/Security/WeakHashAlgorithmAnalyzer.cs`
- [ ] Create `CodeFixes/Security/WeakHashAlgorithmCodeFixProvider.cs`
- [ ] Write analyzer tests (~15 tests)
- [ ] Write code fix tests (~5 tests)
- [ ] Add .editorconfig options support (flag_hmac, suggested_algorithm)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
