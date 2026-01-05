# Library vs Application Mode Detection

## Overview

Automatic detection of whether the analyzed project is a library or an application, used to adjust analyzer behavior (e.g., CCS0032 ConfigureAwait suggestions).

## Detection Logic

```csharp
namespace CodeCop.Sharp.Infrastructure
{
    public enum ProjectType
    {
        Unknown,
        Library,
        Application
    }

    public static class ProjectTypeDetector
    {
        public static ProjectType Detect(Compilation compilation)
        {
            // Check output type
            var options = compilation.Options;
            if (options.OutputKind == OutputKind.DynamicallyLinkedLibrary)
            {
                // It's a DLL, but check for web/UI frameworks
                if (HasWebFramework(compilation) || HasUIFramework(compilation))
                {
                    return ProjectType.Application;
                }
                return ProjectType.Library;
            }

            // Executable outputs are applications
            if (options.OutputKind == OutputKind.ConsoleApplication ||
                options.OutputKind == OutputKind.WindowsApplication)
            {
                return ProjectType.Application;
            }

            return ProjectType.Unknown;
        }

        private static bool HasWebFramework(Compilation compilation)
        {
            return compilation.ReferencedAssemblyNames.Any(a =>
                a.Name.StartsWith("Microsoft.AspNetCore") ||
                a.Name.StartsWith("Microsoft.AspNet") ||
                a.Name == "System.Web");
        }

        private static bool HasUIFramework(Compilation compilation)
        {
            return compilation.ReferencedAssemblyNames.Any(a =>
                a.Name == "PresentationFramework" ||
                a.Name == "PresentationCore" ||
                a.Name == "System.Windows.Forms" ||
                a.Name.StartsWith("Microsoft.Maui") ||
                a.Name.StartsWith("Xamarin.Forms") ||
                a.Name.StartsWith("Avalonia"));
        }
    }
}
```

## Configuration Override

```ini
# .editorconfig
[*.cs]
# Force library mode
dotnet_diagnostic.CCS0032.mode = library

# Force application mode
dotnet_diagnostic.CCS0032.mode = application

# Auto-detect (default)
dotnet_diagnostic.CCS0032.mode = auto
```
