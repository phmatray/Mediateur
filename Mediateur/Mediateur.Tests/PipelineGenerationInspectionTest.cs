using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace Mediateur.Tests;

/// <summary>
/// Test to inspect generated pipeline code for manual verification
/// </summary>
public class PipelineGenerationInspectionTest
{
    private readonly ITestOutputHelper _output;

    public PipelineGenerationInspectionTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void InspectGeneratedPipelineCode()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record TestQuery(string Name) : IRequest<string>;

    [Log]
    public sealed class TestQueryHandler : IRequestHandler<TestQuery, string>
    {
        public ValueTask<string> Handle(TestQuery request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult($""Hello, {request.Name}!"");
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        foreach (var file in generatedFiles)
        {
            _output.WriteLine($"\n=== {file.HintName} ===");
            _output.WriteLine(file.SourceText.ToString());
            _output.WriteLine("\n");
        }

        // Verify pipeline file exists
        var hasPipelineFile = generatedFiles.Any(f => f.HintName.StartsWith("Pipeline."));
        Assert.True(hasPipelineFile, "Expected to find a pipeline file");
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<GeneratedSourceResult> GeneratedFiles)
        RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new MediatorGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics);

        var runResult = driver.GetRunResult();

        return (diagnostics, runResult.GeneratedTrees.Length > 0
            ? runResult.Results[0].GeneratedSources
            : ImmutableArray<GeneratedSourceResult>.Empty);
    }
}
