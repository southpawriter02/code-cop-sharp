# v0.6.0 "Secure" - Specification Overview

## Overview

| Property | Value |
|----------|-------|
| Version | v0.6.0 |
| Theme | "Secure" |
| Target Framework | netstandard2.0 (analyzers) |
| Total Analyzers | 30 (6 new + 24 from previous versions) |
| Key Features | Security pattern detection, CWE mapping, .codecopignore support |

## Goals

1. **Secret Detection**: Identify hardcoded secrets (passwords, API keys, connection strings)
2. **Cryptography Checks**: Detect weak hash algorithms and insecure random number generators
3. **SQL Safety**: Warn on potential SQL injection patterns
4. **CWE Mapping**: Map diagnostics to Common Weakness Enumeration

## New Analyzers

| ID | Name | Category | Severity | Fix | Description | Spec |
|----|------|----------|----------|-----|-------------|------|
| CCS0040 | HardcodedPassword | Security | Error | No | Detect hardcoded passwords | [CCS0040_HardcodedPassword.md](CCS0040_HardcodedPassword.md) |
| CCS0041 | HardcodedApiKey | Security | Error | No | Detect hardcoded API keys | [CCS0041_HardcodedApiKey.md](CCS0041_HardcodedApiKey.md) |
| CCS0042 | HardcodedConnectionString | Security | Warning | No | Detect hardcoded connection strings | [CCS0042_HardcodedConnectionString.md](CCS0042_HardcodedConnectionString.md) |
| CCS0043 | WeakHashAlgorithm | Security | Warning | Yes | Detect MD5/SHA1 for security | [CCS0043_WeakHashAlgorithm.md](CCS0043_WeakHashAlgorithm.md) |
| CCS0044 | InsecureRandom | Security | Warning | Yes | Detect System.Random for security | [CCS0044_InsecureRandom.md](CCS0044_InsecureRandom.md) |
| CCS0045 | SqlStringConcatenation | Security | Warning | No | Warn on SQL string concatenation | [CCS0045_SqlStringConcatenation.md](CCS0045_SqlStringConcatenation.md) |

## Infrastructure Components

### 1. Secret Pattern Matching Engine

Pattern-based detection for common secret formats:
- Password variable names
- API key patterns (prefixes, lengths)
- Connection string formats
- Provider-specific patterns (AWS, Stripe, GitHub, etc.)

**Specification**: [SecretPatternEngine.md](SecretPatternEngine.md)

### 2. CWE Mapping

Map each security rule to its corresponding CWE:

| Rule | CWE ID | CWE Name |
|------|--------|----------|
| CCS0040 | CWE-259, CWE-798 | Use of Hard-coded Password/Credentials |
| CCS0041 | CWE-798 | Use of Hard-coded Credentials |
| CCS0042 | CWE-259, CWE-798 | Use of Hard-coded Password/Credentials |
| CCS0043 | CWE-327, CWE-328 | Broken Crypto Algorithm / Reversible Hash |
| CCS0044 | CWE-330 | Use of Insufficiently Random Values |
| CCS0045 | CWE-89 | SQL Injection |

### 3. .codecopignore File Support

Allow suppressing diagnostics for specific files/patterns using gitignore-style syntax:

```gitignore
# .codecopignore
**/Tests/**
**/TestData/**
appsettings.Development.json

# Rule-specific exclusions
# @CCS0040: tests/**/*.cs
# @CCS0041: tests/Mocks/**
```

**Specification**: [CodeCopIgnore.md](CodeCopIgnore.md)

## Specification Documents

| Document | Description |
|----------|-------------|
| [CCS0040_HardcodedPassword.md](CCS0040_HardcodedPassword.md) | Password detection specification |
| [CCS0041_HardcodedApiKey.md](CCS0041_HardcodedApiKey.md) | API key detection specification |
| [CCS0042_HardcodedConnectionString.md](CCS0042_HardcodedConnectionString.md) | Connection string detection specification |
| [CCS0043_WeakHashAlgorithm.md](CCS0043_WeakHashAlgorithm.md) | Weak hash algorithm detection |
| [CCS0044_InsecureRandom.md](CCS0044_InsecureRandom.md) | Insecure random detection |
| [CCS0045_SqlStringConcatenation.md](CCS0045_SqlStringConcatenation.md) | SQL injection pattern detection |
| [SecretPatternEngine.md](SecretPatternEngine.md) | Pattern matching engine specification |
| [CodeCopIgnore.md](CodeCopIgnore.md) | Ignore file specification |

## Implementation Order

1. **Phase 1: Infrastructure**
   - Secret pattern matching engine
   - .codecopignore file support
   - CWE mapping infrastructure

2. **Phase 2: Secret Detection** (CCS0040-CCS0042)
   - HardcodedPassword
   - HardcodedApiKey
   - HardcodedConnectionString

3. **Phase 3: Cryptography** (CCS0043-CCS0044)
   - WeakHashAlgorithm + CodeFix
   - InsecureRandom + CodeFix

4. **Phase 4: SQL Safety** (CCS0045)
   - SqlStringConcatenation

## File Structure

```
CodeCop.Sharp/
├── Analyzers/
│   └── Security/
│       ├── HardcodedPasswordAnalyzer.cs
│       ├── HardcodedApiKeyAnalyzer.cs
│       ├── HardcodedConnectionStringAnalyzer.cs
│       ├── WeakHashAlgorithmAnalyzer.cs
│       ├── InsecureRandomAnalyzer.cs
│       └── SqlStringConcatenationAnalyzer.cs
├── CodeFixes/
│   └── Security/
│       ├── WeakHashAlgorithmCodeFixProvider.cs
│       └── InsecureRandomCodeFixProvider.cs
├── Infrastructure/
│   ├── CodeCopIgnore.cs
│   ├── CodeCopIgnoreParser.cs
│   └── CodeCopIgnoreMatcher.cs
└── Utilities/
    ├── SecretCategory.cs
    ├── SecretPattern.cs
    └── SecretPatternEngine.cs
```

## Deliverable Checklist

### Infrastructure
- [ ] Create `Utilities/SecretCategory.cs`
- [ ] Create `Utilities/SecretPattern.cs`
- [ ] Create `Utilities/SecretPatternEngine.cs`
- [ ] Create `Infrastructure/CodeCopIgnore.cs`
- [ ] Create `Infrastructure/CodeCopIgnoreParser.cs`

### Analyzers
- [ ] Create `Analyzers/Security/` directory
- [ ] Implement CCS0040 (HardcodedPassword)
- [ ] Implement CCS0041 (HardcodedApiKey)
- [ ] Implement CCS0042 (HardcodedConnectionString)
- [ ] Implement CCS0043 (WeakHashAlgorithm)
- [ ] Implement CCS0044 (InsecureRandom)
- [ ] Implement CCS0045 (SqlStringConcatenation)

### Code Fixes
- [ ] Implement WeakHashAlgorithmCodeFixProvider
- [ ] Implement InsecureRandomCodeFixProvider

### Testing
- [ ] Write tests for SecretPatternEngine
- [ ] Write tests for CodeCopIgnore parser
- [ ] Write tests for CCS0040 (~20 tests)
- [ ] Write tests for CCS0041 (~25 tests)
- [ ] Write tests for CCS0042 (~20 tests)
- [ ] Write tests for CCS0043 (~15 tests)
- [ ] Write tests for CCS0044 (~15 tests)
- [ ] Write tests for CCS0045 (~20 tests)

### Documentation
- [ ] Add CWE information to all security diagnostics
- [ ] Update CHANGELOG.md
- [ ] Document .codecopignore in user guide
