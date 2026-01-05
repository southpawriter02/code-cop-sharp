# CCS0044: InsecureRandom

## Overview

| Property | Value |
|----------|-------|
| Rule ID | CCS0044 |
| Category | Security |
| Severity | Warning |
| Has Code Fix | Yes |
| Enabled by Default | Yes |
| CWE References | [CWE-330](https://cwe.mitre.org/data/definitions/330.html) |

## Description

Detects usage of `System.Random` in security-sensitive contexts. `System.Random` is a pseudorandom number generator (PRNG) that is not cryptographically secure and should not be used for security purposes like generating passwords, tokens, keys, or session identifiers.

### Why This Rule?

1. **Predictability**: `System.Random` output can be predicted if the seed is known
2. **Weak Seeding**: Default seed based on time makes sequences reproducible
3. **Not Thread-Safe**: Concurrent access produces degraded randomness
4. **Security Standards**: NIST and OWASP require cryptographic RNGs for security
5. **Attack Surface**: Predictable tokens enable session hijacking, CSRF bypass

### What This Rule Detects

- Instantiation of `System.Random` in security-related classes/methods
- `Random` usage for generating tokens, passwords, keys, or session IDs
- `Random` in authentication/authorization contexts
- `Random` for cryptographic operations

### Acceptable Uses (Not Flagged)

- Games and simulations
- Statistical sampling
- Shuffling non-sensitive data
- UI effects and animations

---

## Configuration

```ini
# .editorconfig

# Enable/disable rule
dotnet_diagnostic.CCS0044.severity = warning

# Security-sensitive method name patterns (regex)
dotnet_code_quality.CCS0044.sensitive_patterns = token|password|secret|key|session|auth|crypto|salt|nonce|iv
```

---

## Detection Patterns

### Flagged Type

| Type | Namespace | Issue |
|------|-----------|-------|
| `Random` | System | Not cryptographically secure |
| `Random` | System | Predictable with known seed |

### Secure Alternatives

| Alternative | Namespace | Use Case |
|-------------|-----------|----------|
| `RandomNumberGenerator` | System.Security.Cryptography | General cryptographic random |
| `RandomNumberGenerator.GetBytes()` | System.Security.Cryptography | Random bytes |
| `RandomNumberGenerator.GetInt32()` | System.Security.Cryptography | Random integers (.NET 6+) |

### Context Detection

The analyzer flags `System.Random` when used in:

| Context | Example Indicators |
|---------|-------------------|
| Token generation | Method/class names containing `token`, `key`, `secret` |
| Password generation | Method/class names containing `password`, `credential` |
| Session management | Method/class names containing `session`, `auth` |
| Cryptographic operations | Namespace `Security`, `Cryptography` |
| Salt/nonce generation | Variable names containing `salt`, `nonce`, `iv` |

---

## Compliant Examples

```csharp
using System.Security.Cryptography;

// Good - using RandomNumberGenerator for tokens
public string GenerateSecureToken(int length)
{
    var bytes = RandomNumberGenerator.GetBytes(length);
    return Convert.ToBase64String(bytes);
}

// Good - using RandomNumberGenerator for random integers
public int GenerateSecureRandomNumber(int min, int max)
{
    return RandomNumberGenerator.GetInt32(min, max);
}

// Good - using RandomNumberGenerator for password
public string GeneratePassword(int length)
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
    var result = new char[length];

    for (int i = 0; i < length; i++)
    {
        result[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
    }

    return new string(result);
}

// Good - using RandomNumberGenerator for session ID
public string CreateSessionId()
{
    var bytes = RandomNumberGenerator.GetBytes(32);
    return Convert.ToHexString(bytes);
}

// Good - System.Random for non-security purposes (games)
public class DiceRoller
{
    private readonly Random _random = new Random();

    public int RollDice()
    {
        return _random.Next(1, 7);
    }
}

// Good - System.Random for UI effects
public Color GetRandomColor()
{
    var random = new Random();
    return Color.FromArgb(random.Next(256), random.Next(256), random.Next(256));
}

// Good - System.Random for testing/mocking
public class TestDataGenerator
{
    private readonly Random _random = new Random(42); // Fixed seed for reproducibility

    public int GetTestValue() => _random.Next();
}
```

## Non-Compliant Examples

```csharp
// CCS0044 - System.Random for token generation
public class TokenGenerator
{
    private readonly Random _random = new Random();  // Insecure for tokens

    public string GenerateToken()
    {
        var bytes = new byte[32];
        _random.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

// CCS0044 - System.Random for password generation
public string GeneratePassword(int length)
{
    var random = new Random();  // Insecure for passwords
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
}

// CCS0044 - System.Random for session ID
public string CreateSessionId()
{
    var random = new Random();  // Insecure for session IDs
    return random.Next().ToString("X8");
}

// CCS0044 - System.Random for API key generation
public string GenerateApiKey()
{
    var random = new Random();  // Insecure for API keys
    var bytes = new byte[32];
    random.NextBytes(bytes);
    return Convert.ToHexString(bytes);
}

// CCS0044 - System.Random for salt generation
public byte[] GenerateSalt()
{
    var random = new Random();  // Insecure for cryptographic salt
    var salt = new byte[16];
    random.NextBytes(salt);
    return salt;
}

// CCS0044 - System.Random in authentication class
public class AuthenticationService
{
    private readonly Random _random = new Random();  // Insecure in auth context

    public string GenerateResetCode()
    {
        return _random.Next(100000, 999999).ToString();
    }
}

// CCS0044 - System.Random for nonce/IV
public byte[] GenerateNonce()
{
    var random = new Random();  // Insecure for cryptographic nonce
    var nonce = new byte[12];
    random.NextBytes(nonce);
    return nonce;
}
```

---

## Implementation Specification

### File Structure

```
CodeCop.Sharp/
├── Analyzers/Security/InsecureRandomAnalyzer.cs
└── CodeFixes/Security/InsecureRandomCodeFixProvider.cs
```

### Analyzer Implementation

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace CodeCop.Sharp.Analyzers.Security
{
    /// <summary>
    /// Analyzer that detects usage of System.Random in security-sensitive contexts.
    /// </summary>
    /// <remarks>
    /// Rule ID: CCS0044
    /// Category: Security
    /// Severity: Warning
    /// CWE: CWE-330
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InsecureRandomAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CCS0044";

        private static readonly LocalizableString Title = "Insecure random number generator";
        private static readonly LocalizableString MessageFormat =
            "System.Random is not cryptographically secure. Use RandomNumberGenerator for security purposes.";
        private static readonly LocalizableString Description =
            "System.Random should not be used for security-sensitive operations like generating tokens, passwords, or keys.";
        private const string Category = "Security";
        private const string HelpLinkUri = "https://cwe.mitre.org/data/definitions/330.html";

        // Patterns indicating security-sensitive context
        private static readonly Regex SecurityContextPattern = new Regex(
            @"(token|password|passwd|pwd|secret|key|session|auth|crypto|salt|nonce|iv|credential|secure)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Security-related namespaces
        private static readonly string[] SecurityNamespaces = new[]
        {
            "Security",
            "Cryptography",
            "Authentication",
            "Authorization",
            "Identity"
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
            customTags: new[] { "Security", "CWE-330" });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            var typeInfo = context.SemanticModel.GetTypeInfo(creation);
            if (typeInfo.Type?.ToDisplayString() != "System.Random")
                return;

            // Check if in security-sensitive context
            if (IsSecurityContext(creation, context.SemanticModel))
            {
                ReportDiagnostic(context, creation.GetLocation());
            }
        }

        private void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

            // Check if field type is System.Random
            var typeInfo = context.SemanticModel.GetTypeInfo(fieldDeclaration.Declaration.Type);
            if (typeInfo.Type?.ToDisplayString() != "System.Random")
                return;

            // Check if the containing class is security-related
            var containingClass = fieldDeclaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (containingClass != null && IsSecurityRelatedName(containingClass.Identifier.Text))
            {
                var location = fieldDeclaration.Declaration.Type.GetLocation();
                ReportDiagnostic(context, location);
            }
        }

        private bool IsSecurityContext(SyntaxNode node, SemanticModel semanticModel)
        {
            // Check containing method name
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method != null && IsSecurityRelatedName(method.Identifier.Text))
                return true;

            // Check containing class name
            var classDecl = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            if (classDecl != null && IsSecurityRelatedName(classDecl.Identifier.Text))
                return true;

            // Check containing namespace
            var namespaceDecl = node.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
            if (namespaceDecl != null)
            {
                var namespaceName = namespaceDecl.Name.ToString();
                foreach (var secNamespace in SecurityNamespaces)
                {
                    if (namespaceName.Contains(secNamespace))
                        return true;
                }
            }

            // Check if result is assigned to security-related variable
            var assignment = node.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
            if (assignment?.Left is IdentifierNameSyntax identifier)
            {
                if (IsSecurityRelatedName(identifier.Identifier.Text))
                    return true;
            }

            var variableDeclarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            if (variableDeclarator != null && IsSecurityRelatedName(variableDeclarator.Identifier.Text))
                return true;

            // Check local function name
            var localFunction = node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();
            if (localFunction != null && IsSecurityRelatedName(localFunction.Identifier.Text))
                return true;

            return false;
        }

        private static bool IsSecurityRelatedName(string name)
        {
            return SecurityContextPattern.IsMatch(name);
        }

        private void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location)
        {
            var diagnostic = Diagnostic.Create(Rule, location);
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
    /// Code fix provider for CCS0044 (InsecureRandom).
    /// Replaces System.Random with RandomNumberGenerator.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InsecureRandomCodeFixProvider)), Shared]
    public class InsecureRandomCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(InsecureRandomAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);

            // Fix for object creation: new Random() -> RandomNumberGenerator
            if (node.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>() is ObjectCreationExpressionSyntax creation)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Use RandomNumberGenerator",
                        createChangedDocument: c => ReplaceWithRandomNumberGeneratorAsync(context.Document, creation, c),
                        equivalenceKey: "UseRandomNumberGenerator"),
                    diagnostic);
            }

            // Fix for field type: Random -> need manual fix
            if (node.FirstAncestorOrSelf<FieldDeclarationSyntax>() is FieldDeclarationSyntax field)
            {
                // Field replacement is more complex - suggest suppression or manual fix
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Add suppression comment",
                        createChangedDocument: c => AddSuppressionCommentAsync(context.Document, field, c),
                        equivalenceKey: "AddSuppression"),
                    diagnostic);
            }
        }

        private async Task<Document> ReplaceWithRandomNumberGeneratorAsync(
            Document document,
            ObjectCreationExpressionSyntax creation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Determine what method to use based on usage context
            var parent = creation.Parent;

            // Default: RandomNumberGenerator for direct replacement
            // This is a simplified fix - actual implementation would need to analyze
            // how the Random is used and replace accordingly

            var comment = SyntaxFactory.Comment("// TODO: Replace Random usage with RandomNumberGenerator methods");
            var newCreation = creation.WithLeadingTrivia(
                creation.GetLeadingTrivia().Add(comment).Add(SyntaxFactory.LineFeed));

            var newRoot = root.ReplaceNode(creation, newCreation);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> AddSuppressionCommentAsync(
            Document document,
            FieldDeclarationSyntax field,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var pragmaDisable = SyntaxFactory.Trivia(
                SyntaxFactory.PragmaWarningDirectiveTrivia(
                    SyntaxFactory.Token(SyntaxKind.DisableKeyword),
                    true)
                .WithErrorCodes(
                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.IdentifierName("CCS0044"))));

            var newField = field.WithLeadingTrivia(
                field.GetLeadingTrivia()
                    .Add(pragmaDisable)
                    .Add(SyntaxFactory.LineFeed));

            var newRoot = root.ReplaceNode(field, newField);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
```

---

## Decision Tree

```
                    ┌─────────────────────────────┐
                    │ Is it System.Random usage?  │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      NO       │────────► SKIP
                           └───────┬───────┘
                                   │ YES
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is containing method name   │
                    │ security-related?           │
                    │ (token, password, key, etc.)│
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► REPORT CCS0044
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is containing class name    │
                    │ security-related?           │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► REPORT CCS0044
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is namespace security-      │
                    │ related? (Security, Auth)   │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► REPORT CCS0044
                           └───────┬───────┘
                                   │ NO
                                   ▼
                    ┌─────────────────────────────┐
                    │ Is assigned to security-    │
                    │ related variable name?      │
                    └──────────────┬──────────────┘
                                   │
                           ┌───────▼───────┐
                           │      YES      │────────► REPORT CCS0044
                           └───────┬───────┘
                                   │ NO
                                   ▼
                                 SKIP
                         (non-security context)
```

---

## Test Cases

### Analyzer Tests - Should Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| TokenGeneratorClass | `class TokenGenerator { Random r = new(); }` | CCS0044 |
| GeneratePasswordMethod | `void GeneratePassword() { new Random(); }` | CCS0044 |
| CreateSessionIdMethod | `void CreateSessionId() { new Random(); }` | CCS0044 |
| AuthServiceClass | `class AuthService { Random r = new(); }` | CCS0044 |
| GenerateApiKeyMethod | `void GenerateApiKey() { new Random(); }` | CCS0044 |
| SecurityNamespace | `namespace Security { ... new Random() }` | CCS0044 |
| GenerateSaltMethod | `void GenerateSalt() { new Random(); }` | CCS0044 |
| SecureTokenVariable | `var secureToken = new Random();` | CCS0044 |
| CryptoHelperClass | `class CryptoHelper { Random r; }` | CCS0044 |
| GenerateNonceMethod | `void GenerateNonce() { new Random(); }` | CCS0044 |

### Analyzer Tests - Should NOT Trigger Diagnostic

| Test Name | Input | Expected |
|-----------|-------|----------|
| GameDiceRoll | `class Game { void Roll() { new Random(); } }` | No diagnostic |
| UIAnimation | `void Animate() { new Random(); }` | No diagnostic |
| TestDataGenerator | `class TestData { Random r; }` | No diagnostic |
| ShuffleCards | `void Shuffle() { new Random(); }` | No diagnostic |
| RandomColor | `void GetRandomColor() { new Random(); }` | No diagnostic |
| StatisticalSample | `void Sample() { new Random(); }` | No diagnostic |
| RandomNumberGenerator | `RandomNumberGenerator.GetBytes(16)` | No diagnostic |

### Code Fix Tests

| Test Name | Input | Expected |
|-----------|-------|-----------------|
| AddTodoComment | `new Random()` | Comment added with TODO |
| AddSuppression | Field with `Random` | `#pragma warning disable` added |

---

## Edge Cases

| Case | Behavior | Rationale |
|------|----------|-----------|
| Random in test code | Context-dependent | Test code may need reproducibility |
| Shared Random instance | Analyze per-usage | Thread safety is separate concern |
| Random with specific seed | Still flagged | Seeded random is still predictable |
| Random in game namespace | Not flagged | Game context is non-security |
| Random.Shared (.NET 6+) | Analyze context | Same security concerns apply |
| Third-party random libs | Not detected | Only detects System.Random |
| Random in LINQ shuffle | Context-dependent | Depends on what's being shuffled |

---

## Related Rules

| Rule | Relationship |
|------|--------------|
| CCS0043 | WeakHashAlgorithm - Both relate to cryptographic security |
| CA5394 | Do not use insecure randomness (Roslyn built-in) |

---

## References

- [CWE-330: Use of Insufficiently Random Values](https://cwe.mitre.org/data/definitions/330.html)
- [OWASP: Insecure Randomness](https://owasp.org/www-community/vulnerabilities/Insecure_Randomness)
- [Microsoft Docs: RandomNumberGenerator Class](https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator)
- [NIST SP 800-90A: Recommendation for Random Number Generation](https://csrc.nist.gov/publications/detail/sp/800-90a/rev-1/final)

---

## Deliverable Checklist

- [ ] Create `Analyzers/Security/InsecureRandomAnalyzer.cs`
- [ ] Create `CodeFixes/Security/InsecureRandomCodeFixProvider.cs`
- [ ] Write analyzer tests (~15 tests)
- [ ] Write code fix tests (~3 tests)
- [ ] Add .editorconfig options support (sensitive_patterns)
- [ ] Verify all tests pass
- [ ] Test manually in Visual Studio
