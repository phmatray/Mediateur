using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mediateur.Tests;

/// <summary>
/// Tests for Roslyn analyzers and diagnostics
/// </summary>
public class MediatorAnalyzerTests
{
    [Fact]
    public void WarnWhenMultipleHandlersForSameRequest()
    {
        // Arrange - Define TWO handlers for the same request type
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record GetUserQuery(Guid Id) : IRequest<string>;

    public sealed class FirstHandler : IRequestHandler<GetUserQuery, string>
    {
        public ValueTask<string> Handle(GetUserQuery request, CancellationToken cancellationToken)
            => ValueTask.FromResult(""First"");
    }

    public sealed class SecondHandler : IRequestHandler<GetUserQuery, string>
    {
        public ValueTask<string> Handle(GetUserQuery request, CancellationToken cancellationToken)
            => ValueTask.FromResult(""Second"");
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        // Should produce a warning diagnostic about multiple handlers
        Assert.Contains(diagnostics, d => d.Id == "MEDGEN001" && d.Severity == DiagnosticSeverity.Warning);
        Assert.NotEmpty(generatedFiles); // Should still generate code
    }

    [Fact]
    public void HandlesRequestWithoutHandlerGracefully()
    {
        // Arrange - Request defined but no handler
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record OrphanQuery(string Data) : IRequest<string>;

    // No handler defined!
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        // Should produce a warning about request without handler
        Assert.Contains(diagnostics, d => d.Id == "MEDGEN002" && d.Severity == DiagnosticSeverity.Warning);

        if (generatedFiles.Length > 0)
        {
            var mediatorFile = generatedFiles.FirstOrDefault(f => f.HintName == "Mediator.g.cs");
            if (mediatorFile.SourceText != null)
            {
                var code = mediatorFile.SourceText.ToString();
                // OrphanQuery shouldn't appear in routing
                Assert.DoesNotContain("OrphanQuery", code);
            }
        }
    }

    [Fact]
    public void IgnoresClassesNotImplementingHandlerInterfaces()
    {
        // Arrange
        var source = @"
using System;
using Mediateur;

namespace TestNamespace
{
    // This is NOT a handler
    public class RegularClass
    {
        public void DoSomething() { }
    }

    // This is NOT a request
    public class RegularData
    {
        public string Name { get; set; }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        // Should not have any MEDGEN diagnostics
        var medgenDiagnostics = diagnostics.Where(d => d.Id.StartsWith("MEDGEN")).ToArray();
        Assert.Empty(medgenDiagnostics);
        Assert.Empty(generatedFiles); // No handlers, so no generated code
    }

    [Fact]
    public void ValidatesHandlerImplementation()
    {
        // Arrange - Handler with wrong signature
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record TestQuery(string Data) : IRequest<string>;

    // Correctly implements IRequestHandler
    public sealed class TestQueryHandler : IRequestHandler<TestQuery, string>
    {
        public ValueTask<string> Handle(TestQuery request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(""Result"");
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        // Should not have any MEDGEN diagnostics or errors
        var medgenDiagnostics = diagnostics.Where(d => d.Id.StartsWith("MEDGEN") || d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(medgenDiagnostics);
        Assert.NotEmpty(generatedFiles);

        var mediatorFile = generatedFiles.First(f => f.HintName == "Mediator.g.cs");
        var code = mediatorFile.SourceText.ToString();

        Assert.Contains("TestQuery", code);
        Assert.Contains("TestQueryHandler", code);
    }

    [Fact]
    public void HandlesGenericRequestsCorrectly()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record PagedQuery<T>(int Page, int Size) : IRequest<T>;

    // Generic handler
    public sealed class PagedQueryHandler<T> : IRequestHandler<PagedQuery<T>, T>
    {
        public ValueTask<T> Handle(PagedQuery<T> request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(default(T));
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        // Generic handlers are complex - not currently supported
        // This test documents current behavior: analyzer warns about missing handler
        // because it can't match open generic types
        var medgenDiagnostics = diagnostics.Where(d => d.Id.StartsWith("MEDGEN")).ToArray();
        Assert.NotEmpty(medgenDiagnostics); // Should warn about missing handler for generic request
        Assert.Contains(medgenDiagnostics, d => d.Id == "MEDGEN002"); // Missing handler warning
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<GeneratedSourceResult> GeneratedFiles)
        RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>()
            .Append(MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MediatorGenerator();

        // Run generator
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        var runResult = driver.GetRunResult();

        // Load and run analyzer
        var assembly = typeof(MediatorGenerator).Assembly;
        var analyzerType = assembly.GetType("Mediateur.MediatorAnalyzer");

        var allDiagnostics = generatorDiagnostics;

        if (analyzerType != null)
        {
            var analyzer = (DiagnosticAnalyzer)Activator.CreateInstance(analyzerType)!;
            var compilationWithAnalyzers = outputCompilation.WithAnalyzers(
                ImmutableArray.Create(analyzer));

            var analyzerDiagnostics = compilationWithAnalyzers.GetAllDiagnosticsAsync().Result;

            // Combine diagnostics from generator and analyzer
            allDiagnostics = generatorDiagnostics.AddRange(analyzerDiagnostics);
        }

        return (allDiagnostics, runResult.GeneratedTrees.Length > 0
            ? runResult.Results[0].GeneratedSources
            : ImmutableArray<GeneratedSourceResult>.Empty);
    }
}
