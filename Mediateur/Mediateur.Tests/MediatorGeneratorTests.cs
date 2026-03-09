using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mediateur.Tests;

public class MediatorGeneratorTests
{
    [Fact]
    public void GeneratesMediator_WithBasicRequestHandler()
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
        Assert.Contains(generatedFiles, f => f.HintName == "Mediator.g.cs");
        Assert.Contains(generatedFiles, f => f.HintName == "ServiceCollectionExtensions.g.cs");

        var mediatorFile = generatedFiles.First(f => f.HintName == "Mediator.g.cs");
        var mediatorCode = mediatorFile.SourceText.ToString();

        Assert.Contains("class Mediator : IMediator", mediatorCode);
        Assert.Contains("SendTypedTestQuery", mediatorCode);
    }

    [Fact]
    public void GeneratesMediator_WithVoidRequestHandler()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record TestCommand(string Name) : IRequest;

    public sealed class TestCommandHandler : IRequestHandler<TestCommand>
    {
        public ValueTask<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            Console.WriteLine($""Executed: {request.Name}"");
            return ValueTask.FromResult(Unit.Value);
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        var mediatorFile = generatedFiles.First(f => f.HintName == "Mediator.g.cs");
        var mediatorCode = mediatorFile.SourceText.ToString();

        Assert.Contains("SendTypedTestCommand", mediatorCode);
        Assert.Contains("async ValueTask Send(IRequest request", mediatorCode);
    }

    [Fact]
    public void GeneratesMediator_WithNotificationHandler()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record TestNotification(string Message) : INotification;

    public sealed class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine(notification.Message);
            return ValueTask.CompletedTask;
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        var mediatorFile = generatedFiles.First(f => f.HintName == "Mediator.g.cs");
        var mediatorCode = mediatorFile.SourceText.ToString();

        Assert.Contains("ValueTask Publish<TNotification>", mediatorCode);
        Assert.Contains("TestNotification", mediatorCode);
    }

    [Fact]
    public void GeneratesMediator_WithMultipleNotificationHandlers()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record UserCreatedNotification(string UserId) : INotification;

    public sealed class EmailNotificationHandler : INotificationHandler<UserCreatedNotification>
    {
        public ValueTask Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($""Sending email for user: {notification.UserId}"");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class AuditNotificationHandler : INotificationHandler<UserCreatedNotification>
    {
        public ValueTask Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
        {
            Console.WriteLine($""Auditing user creation: {notification.UserId}"");
            return ValueTask.CompletedTask;
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        var mediatorFile = generatedFiles.First(f => f.HintName == "Mediator.g.cs");
        var mediatorCode = mediatorFile.SourceText.ToString();

        // Both handlers should be called
        Assert.Contains("EmailNotificationHandler", mediatorCode);
        Assert.Contains("AuditNotificationHandler", mediatorCode);
    }

    [Fact]
    public void GeneratesDependencyInjection_WithAllHandlers()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record TestQuery(int Id) : IRequest<string>;
    public sealed class TestQueryHandler : IRequestHandler<TestQuery, string>
    {
        public ValueTask<string> Handle(TestQuery request, CancellationToken cancellationToken)
            => ValueTask.FromResult(""Result"");
    }

    public sealed record TestCommand() : IRequest;
    public sealed class TestCommandHandler : IRequestHandler<TestCommand>
    {
        public ValueTask<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
            => ValueTask.FromResult(Unit.Value);
    }

    public sealed record TestNotification() : INotification;
    public sealed class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        var diFile = generatedFiles.First(f => f.HintName == "ServiceCollectionExtensions.g.cs");
        var diCode = diFile.SourceText.ToString();

        Assert.Contains("AddCompiledMediator", diCode);
        Assert.Contains("TestQueryHandler", diCode);
        Assert.Contains("TestCommandHandler", diCode);
        Assert.Contains("TestNotificationHandler", diCode);
        Assert.Contains("AddSingleton<IMediator, Mediator>", diCode);
    }

    [Fact]
    public void GeneratesNothing_WhenNoHandlersPresent()
    {
        // Arrange
        var source = @"
using System;

namespace TestNamespace
{
    public class MyClass
    {
        public void DoSomething() { }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);
        Assert.Empty(generatedFiles);
    }

    [Fact]
    public void GeneratesPipelineWrapper_WithLogAttribute()
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

        // Should generate a pipeline wrapper file
        Assert.Contains(generatedFiles, f => f.HintName == "Pipeline.TestQuery.g.cs");

        var pipelineFile = generatedFiles.First(f => f.HintName == "Pipeline.TestQuery.g.cs");
        var pipelineCode = pipelineFile.SourceText.ToString();

        // Should contain wrapper class
        Assert.Contains("__Log_TestQuery", pipelineCode);
        Assert.Contains("Stopwatch", pipelineCode);
        Assert.Contains("Console.WriteLine", pipelineCode);
    }

    [Fact]
    public void GeneratesPipelineWrapper_WithValidateAttribute()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record CreateUserCommand(string Name, string Email) : IRequest;

    [Validate]
    public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand>
    {
        public ValueTask<Unit> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Unit.Value);
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        // Should generate a pipeline wrapper file
        Assert.Contains(generatedFiles, f => f.HintName == "Pipeline.CreateUserCommand.g.cs");

        var pipelineFile = generatedFiles.First(f => f.HintName == "Pipeline.CreateUserCommand.g.cs");
        var pipelineCode = pipelineFile.SourceText.ToString();

        // Should contain wrapper class
        Assert.Contains("__Validate_CreateUserCommand", pipelineCode);
    }

    [Fact]
    public void GeneratesNestedPipelineWrappers_WithMultipleAttributes()
    {
        // Arrange
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
using Mediateur;

namespace TestNamespace
{
    public sealed record UpdateUserCommand(int Id, string Name) : IRequest;

    [Validate(Order = 1)]
    [Log(Order = 2)]
    public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
    {
        public ValueTask<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Unit.Value);
        }
    }
}";

        // Act
        var (diagnostics, generatedFiles) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics);

        var pipelineFile = generatedFiles.First(f => f.HintName == "Pipeline.UpdateUserCommand.g.cs");
        var pipelineCode = pipelineFile.SourceText.ToString();

        // Should contain both wrapper classes
        Assert.Contains("__Validate_UpdateUserCommand", pipelineCode);
        Assert.Contains("__Log_UpdateUserCommand", pipelineCode);

        // Check order in mediator
        var mediatorFile = generatedFiles.First(f => f.HintName == "Mediator.g.cs");
        var mediatorCode = mediatorFile.SourceText.ToString();

        // Validate should wrap Log which wraps the actual handler (order matters)
        var validateIndex = mediatorCode.IndexOf("__Validate_UpdateUserCommand");
        var logIndex = mediatorCode.IndexOf("__Log_UpdateUserCommand");

        Assert.True(validateIndex > 0);
        Assert.True(logIndex > 0);
        // Validate should appear first (outer wrapper)
        Assert.True(validateIndex < logIndex);
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
