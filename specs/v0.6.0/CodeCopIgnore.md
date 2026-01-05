# .codecopignore File Support

## Overview

The `.codecopignore` file allows developers to exclude specific files, directories, or patterns from CodeCop analysis. This follows the familiar `.gitignore` format and provides fine-grained control over which files are analyzed.

## Goals

1. **Selective Exclusion**: Exclude files/folders that shouldn't be analyzed
2. **Familiar Syntax**: Use gitignore-style patterns developers already know
3. **Hierarchical**: Support multiple .codecopignore files in subdirectories
4. **Performance**: Skip excluded files early in the analysis pipeline
5. **Security Focus**: Allow exclusion of intentional secret patterns in test files

---

## File Format

### Location

```
MyProject/
├── .codecopignore          # Root ignore file
├── src/
│   └── .codecopignore      # Additional ignore file (inherits from root)
├── tests/
│   └── .codecopignore      # Test-specific exclusions
└── generated/
    └── .codecopignore      # Generated code exclusions
```

### Syntax

The `.codecopignore` file uses gitignore syntax:

```gitignore
# Comments start with #

# Ignore all files in a directory
bin/
obj/

# Ignore files by extension
*.Designer.cs
*.generated.cs

# Ignore specific files
appsettings.Development.json
secrets.json

# Ignore files matching pattern
**/Migrations/*.cs

# Negate a pattern (include previously excluded)
!important.Designer.cs

# Ignore by rule ID (CodeCop extension)
# @CCS0040: tests/**/*.cs
# @CCS0041: tests/**/*.cs
```

---

## Pattern Types

### Standard Patterns

| Pattern | Matches | Example |
|---------|---------|---------|
| `file.cs` | Exact file name | `file.cs` anywhere |
| `*.cs` | Wildcard extension | Any `.cs` file |
| `dir/` | Directory | All contents of `dir` |
| `**/dir/` | Directory anywhere | `dir` at any depth |
| `dir/**` | All subdirectories | Everything under `dir` |
| `*.{cs,vb}` | Multiple extensions | `.cs` or `.vb` files |
| `!pattern` | Negation | Un-exclude pattern |

### CodeCop Extensions

| Syntax | Description |
|--------|-------------|
| `# @CCS0040: pattern` | Exclude pattern from specific rule |
| `# @Security: pattern` | Exclude pattern from security rules |
| `# @All: pattern` | Equivalent to standard pattern |

---

## Configuration Examples

### Basic .codecopignore

```gitignore
# Ignore generated code
*.Designer.cs
*.g.cs
*.generated.cs

# Ignore build output
bin/
obj/

# Ignore packages
packages/
node_modules/

# Ignore IDE files
.vs/
.idea/

# Ignore migrations (often have hardcoded strings)
**/Migrations/*.cs

# Ignore test fixtures with intentional "secrets"
tests/Fixtures/**
tests/**/*TestData*

# Ignore third-party code
vendor/
external/
```

### Rule-Specific Exclusions

```gitignore
# Allow hardcoded passwords in test files
# @CCS0040: tests/**/*.cs
# @CCS0040: **/*Tests.cs

# Allow hardcoded API keys in test mocks
# @CCS0041: tests/Mocks/**

# Allow test connection strings
# @CCS0042: tests/**/appsettings.Test.json

# Allow weak hashes in legacy compatibility layer
# @CCS0043: src/Legacy/**

# Allow System.Random in game logic
# @CCS0044: src/Game/**
```

### Development Environment

```gitignore
# Development secrets (should not be committed anyway)
appsettings.Development.json
appsettings.Local.json
secrets.json
.env
.env.local

# Local test databases
*.db
*.sqlite
```

---

## Implementation

### File Structure

```
CodeCop.Sharp/
└── Infrastructure/
    ├── CodeCopIgnore.cs
    ├── CodeCopIgnoreParser.cs
    └── CodeCopIgnoreMatcher.cs
```

### CodeCopIgnore Class

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace CodeCop.Sharp.Infrastructure
{
    /// <summary>
    /// Represents a parsed .codecopignore file.
    /// </summary>
    public sealed class CodeCopIgnore
    {
        /// <summary>
        /// The directory containing this .codecopignore file.
        /// </summary>
        public string BasePath { get; }

        /// <summary>
        /// Global patterns that apply to all rules.
        /// </summary>
        public ImmutableArray<IgnorePattern> GlobalPatterns { get; }

        /// <summary>
        /// Patterns specific to certain rule IDs.
        /// </summary>
        public ImmutableDictionary<string, ImmutableArray<IgnorePattern>> RulePatterns { get; }

        /// <summary>
        /// Parent .codecopignore (from parent directory), if any.
        /// </summary>
        public CodeCopIgnore Parent { get; }

        public CodeCopIgnore(
            string basePath,
            IEnumerable<IgnorePattern> globalPatterns,
            IDictionary<string, IEnumerable<IgnorePattern>> rulePatterns,
            CodeCopIgnore parent = null)
        {
            BasePath = basePath;
            GlobalPatterns = globalPatterns.ToImmutableArray();
            RulePatterns = rulePatterns
                .ToImmutableDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToImmutableArray());
            Parent = parent;
        }

        /// <summary>
        /// Checks if a file path should be ignored for a specific rule.
        /// </summary>
        /// <param name="filePath">Full path to the file.</param>
        /// <param name="ruleId">The rule ID (e.g., "CCS0040").</param>
        /// <returns>True if the file should be ignored.</returns>
        public bool IsIgnored(string filePath, string ruleId = null)
        {
            // Convert to relative path
            var relativePath = GetRelativePath(filePath);
            if (relativePath == null)
                return Parent?.IsIgnored(filePath, ruleId) ?? false;

            // Check global patterns
            var globalMatch = MatchPatterns(GlobalPatterns, relativePath);

            // Check rule-specific patterns
            var ruleMatch = false;
            if (ruleId != null && RulePatterns.TryGetValue(ruleId, out var patterns))
            {
                ruleMatch = MatchPatterns(patterns, relativePath);
            }

            // Check parent
            var parentMatch = Parent?.IsIgnored(filePath, ruleId) ?? false;

            return globalMatch || ruleMatch || parentMatch;
        }

        private bool MatchPatterns(ImmutableArray<IgnorePattern> patterns, string relativePath)
        {
            bool isIgnored = false;

            foreach (var pattern in patterns)
            {
                if (pattern.Matches(relativePath))
                {
                    isIgnored = !pattern.IsNegation;
                }
            }

            return isIgnored;
        }

        private string GetRelativePath(string filePath)
        {
            if (!filePath.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase))
                return null;

            return filePath.Substring(BasePath.Length).TrimStart(Path.DirectorySeparatorChar);
        }
    }
}
```

### IgnorePattern Class

```csharp
using System;
using System.Text.RegularExpressions;

namespace CodeCop.Sharp.Infrastructure
{
    /// <summary>
    /// Represents a single pattern from a .codecopignore file.
    /// </summary>
    public sealed class IgnorePattern
    {
        /// <summary>
        /// The original pattern string.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Whether this is a negation pattern (starts with !).
        /// </summary>
        public bool IsNegation { get; }

        /// <summary>
        /// Whether this pattern matches directories only (ends with /).
        /// </summary>
        public bool IsDirectoryOnly { get; }

        /// <summary>
        /// Compiled regex for matching.
        /// </summary>
        private readonly Regex _regex;

        public IgnorePattern(string pattern)
        {
            var originalPattern = pattern;

            // Handle negation
            IsNegation = pattern.StartsWith("!");
            if (IsNegation)
                pattern = pattern.Substring(1);

            // Handle directory marker
            IsDirectoryOnly = pattern.EndsWith("/");
            if (IsDirectoryOnly)
                pattern = pattern.TrimEnd('/');

            Pattern = originalPattern;
            _regex = ConvertToRegex(pattern);
        }

        /// <summary>
        /// Checks if the given path matches this pattern.
        /// </summary>
        public bool Matches(string path)
        {
            // Normalize path separators
            path = path.Replace('\\', '/');

            return _regex.IsMatch(path);
        }

        private static Regex ConvertToRegex(string pattern)
        {
            // Escape regex special characters except * and ?
            var regexPattern = Regex.Escape(pattern)
                .Replace(@"\*\*", ".*")           // ** matches anything
                .Replace(@"\*", "[^/]*")          // * matches within path segment
                .Replace(@"\?", "[^/]");          // ? matches single char

            // If pattern doesn't start with /, match from any directory
            if (!pattern.StartsWith("/"))
            {
                regexPattern = "(^|/)" + regexPattern;
            }
            else
            {
                regexPattern = "^" + regexPattern.Substring(2); // Remove escaped /
            }

            // Match full path or as prefix for directories
            regexPattern = regexPattern + "($|/)";

            return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
```

### CodeCopIgnoreParser Class

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodeCop.Sharp.Infrastructure
{
    /// <summary>
    /// Parses .codecopignore files.
    /// </summary>
    public static class CodeCopIgnoreParser
    {
        private static readonly Regex RulePatternRegex = new Regex(
            @"^#\s*@(CCS\d{4}|Security|All):\s*(.+)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses a .codecopignore file.
        /// </summary>
        public static CodeCopIgnore Parse(string filePath, CodeCopIgnore parent = null)
        {
            if (!File.Exists(filePath))
                return null;

            var basePath = Path.GetDirectoryName(filePath);
            var globalPatterns = new List<IgnorePattern>();
            var rulePatterns = new Dictionary<string, List<IgnorePattern>>();

            foreach (var line in File.ReadAllLines(filePath))
            {
                var trimmedLine = line.Trim();

                // Skip empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine))
                    continue;

                // Check for rule-specific pattern
                var ruleMatch = RulePatternRegex.Match(trimmedLine);
                if (ruleMatch.Success)
                {
                    var ruleId = ruleMatch.Groups[1].Value;
                    var pattern = ruleMatch.Groups[2].Value.Trim();

                    if (ruleId == "All")
                    {
                        globalPatterns.Add(new IgnorePattern(pattern));
                    }
                    else if (ruleId == "Security")
                    {
                        // Add to all security rules
                        foreach (var secRule in SecurityRules)
                        {
                            AddRulePattern(rulePatterns, secRule, pattern);
                        }
                    }
                    else
                    {
                        AddRulePattern(rulePatterns, ruleId, pattern);
                    }
                    continue;
                }

                // Skip comments
                if (trimmedLine.StartsWith("#"))
                    continue;

                // Add global pattern
                globalPatterns.Add(new IgnorePattern(trimmedLine));
            }

            return new CodeCopIgnore(
                basePath,
                globalPatterns,
                rulePatterns.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (IEnumerable<IgnorePattern>)kvp.Value),
                parent);
        }

        /// <summary>
        /// Finds and parses all .codecopignore files in the path hierarchy.
        /// </summary>
        public static CodeCopIgnore ParseHierarchy(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            var ignoreFiles = new Stack<string>();

            // Collect all .codecopignore files from current to root
            while (!string.IsNullOrEmpty(directory))
            {
                var ignoreFile = Path.Combine(directory, ".codecopignore");
                if (File.Exists(ignoreFile))
                {
                    ignoreFiles.Push(ignoreFile);
                }

                var parent = Directory.GetParent(directory);
                directory = parent?.FullName;
            }

            // Parse from root to current (so child overrides parent)
            CodeCopIgnore result = null;
            while (ignoreFiles.Count > 0)
            {
                var ignoreFile = ignoreFiles.Pop();
                result = Parse(ignoreFile, result);
            }

            return result;
        }

        private static void AddRulePattern(
            Dictionary<string, List<IgnorePattern>> rulePatterns,
            string ruleId,
            string pattern)
        {
            if (!rulePatterns.TryGetValue(ruleId, out var patterns))
            {
                patterns = new List<IgnorePattern>();
                rulePatterns[ruleId] = patterns;
            }
            patterns.Add(new IgnorePattern(pattern));
        }

        private static readonly string[] SecurityRules = new[]
        {
            "CCS0040", "CCS0041", "CCS0042", "CCS0043", "CCS0044", "CCS0045"
        };
    }
}
```

---

## Integration with Analyzers

### In Security Analyzers

```csharp
public class HardcodedPasswordAnalyzer : DiagnosticAnalyzer
{
    private CodeCopIgnore _ignoreFile;

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Load .codecopignore for this compilation
            var syntaxTree = compilationContext.Compilation.SyntaxTrees.FirstOrDefault();
            if (syntaxTree != null)
            {
                _ignoreFile = CodeCopIgnoreParser.ParseHierarchy(syntaxTree.FilePath);
            }

            compilationContext.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.StringLiteralExpression);
        });
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        // Check if file is ignored
        var filePath = context.Node.SyntaxTree.FilePath;
        if (_ignoreFile?.IsIgnored(filePath, DiagnosticId) == true)
            return;

        // Continue with analysis...
    }
}
```

### In CodeCop.Core Runner

```csharp
public class AnalysisRunner
{
    public async Task<AnalysisResult> AnalyzeAsync(string projectPath)
    {
        var ignoreFile = CodeCopIgnoreParser.ParseHierarchy(projectPath);

        foreach (var sourceFile in GetSourceFiles(projectPath))
        {
            // Skip globally ignored files
            if (ignoreFile?.IsIgnored(sourceFile) == true)
            {
                continue;
            }

            // Analyze file...
            foreach (var diagnostic in AnalyzeFile(sourceFile))
            {
                // Check rule-specific exclusions
                if (ignoreFile?.IsIgnored(sourceFile, diagnostic.Id) == true)
                {
                    continue;
                }

                yield return diagnostic;
            }
        }
    }
}
```

---

## CLI Support

### Commands

```bash
# Initialize .codecopignore with default patterns
codecop init-ignore

# Add pattern to .codecopignore
codecop ignore "*.Designer.cs"

# Add rule-specific pattern
codecop ignore --rule CCS0040 "tests/**"

# List ignored files
codecop list-ignored

# Check if file is ignored
codecop check-ignored src/File.cs --rule CCS0040
```

---

## Caching

### Performance Optimization

```csharp
public sealed class CodeCopIgnoreCache
{
    private static readonly ConcurrentDictionary<string, CodeCopIgnore> _cache
        = new ConcurrentDictionary<string, CodeCopIgnore>(StringComparer.OrdinalIgnoreCase);

    public static CodeCopIgnore GetOrParse(string directory)
    {
        return _cache.GetOrAdd(directory, dir =>
        {
            var filePath = Path.Combine(dir, ".codecopignore");
            return CodeCopIgnoreParser.Parse(filePath, GetParentIgnore(dir));
        });
    }

    private static CodeCopIgnore GetParentIgnore(string directory)
    {
        var parent = Directory.GetParent(directory);
        return parent != null ? GetOrParse(parent.FullName) : null;
    }

    public static void InvalidateCache()
    {
        _cache.Clear();
    }
}
```

---

## Test Cases

### Parser Tests

| Test | Input | Expected |
|------|-------|----------|
| SimplePattern | `*.cs` | Matches all .cs files |
| DirectoryPattern | `bin/` | Matches bin directory |
| GlobPattern | `**/test/*.cs` | Matches test/*.cs at any depth |
| NegationPattern | `!important.cs` | Un-excludes important.cs |
| RulePattern | `# @CCS0040: tests/**` | Only for CCS0040 |
| CommentIgnored | `# comment` | No pattern created |
| EmptyLine | `` | Skipped |

### Matching Tests

| File | Pattern | Matches |
|------|---------|---------|
| `src/File.cs` | `*.cs` | Yes |
| `bin/Debug/out.dll` | `bin/` | Yes |
| `tests/Unit/Test.cs` | `**/tests/**` | Yes |
| `src/Core/File.cs` | `src/` | Yes |
| `important.cs` | `*.cs` + `!important.cs` | No |

---

## Deliverable Checklist

- [ ] Create `Infrastructure/IgnorePattern.cs`
- [ ] Create `Infrastructure/CodeCopIgnore.cs`
- [ ] Create `Infrastructure/CodeCopIgnoreParser.cs`
- [ ] Create `Infrastructure/CodeCopIgnoreCache.cs`
- [ ] Write pattern matching unit tests
- [ ] Write parser unit tests
- [ ] Write hierarchical parsing tests
- [ ] Integrate with analyzers
- [ ] Add CLI commands
- [ ] Document in user guide
