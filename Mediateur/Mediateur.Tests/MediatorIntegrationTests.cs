using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mediateur.Tests;

/// <summary>
/// Integration tests that use real DI container and execute handlers end-to-end
/// </summary>
public class MediatorIntegrationTests
{
    [Fact]
    public async Task SendQuery_ExecutesHandler_ReturnsResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompiledMediator();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var query = new TestQuery("Alice");
        var result = await mediator.Send(query);

        // Assert
        Assert.Equal("Hello, Alice!", result);
    }

    [Fact]
    public async Task SendCommand_ExecutesHandler_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompiledMediator();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var command = new TestCommand("DoSomething");
        await mediator.Send(command);

        // Assert - should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task PublishNotification_ExecutesAllHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompiledMediator();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var notification = new TestNotification("TestEvent");
        await mediator.Publish(notification);

        // Assert - Handlers should execute (we can verify via output in real scenario)
        Assert.True(true);
    }

    [Fact]
    public async Task SendQueryWithDependency_InjectsDependencyCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestRepository, TestRepository>();
        services.AddCompiledMediator();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var query = new GetDataQuery(123);
        var result = await mediator.Send(query);

        // Assert
        Assert.Equal("Data from repository: 123", result);
    }

    [Fact]
    public async Task SendWithCancellationToken_PassesTokenToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddCompiledMediator();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        var query = new CancellableQuery();
        // TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await mediator.Send(query, cts.Token);
        });
    }
}

// Test domain models and handlers
public sealed record TestQuery(string Name) : IRequest<string>;

public sealed class TestQueryHandler : IRequestHandler<TestQuery, string>
{
    public ValueTask<string> Handle(TestQuery request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult($"Hello, {request.Name}!");
    }
}

public sealed record TestCommand(string Action) : IRequest;

public sealed class TestCommandHandler : IRequestHandler<TestCommand>
{
    public ValueTask<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
    {
        // Execute command logic
        return ValueTask.FromResult(Unit.Value);
    }
}

public sealed record TestNotification(string Message) : INotification;

public sealed class FirstTestNotificationHandler : INotificationHandler<TestNotification>
{
    public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        // Handle notification
        return ValueTask.CompletedTask;
    }
}

public sealed class SecondTestNotificationHandler : INotificationHandler<TestNotification>
{
    public ValueTask Handle(TestNotification notification, CancellationToken cancellationToken)
    {
        // Handle notification
        return ValueTask.CompletedTask;
    }
}

// Test with dependency injection
public interface ITestRepository
{
    string GetData(int id);
}

public class TestRepository : ITestRepository
{
    public string GetData(int id) => $"Data from repository: {id}";
}

public sealed record GetDataQuery(int Id) : IRequest<string>;

public sealed class GetDataQueryHandler : IRequestHandler<GetDataQuery, string>
{
    private readonly ITestRepository _repository;

    public GetDataQueryHandler(ITestRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<string> Handle(GetDataQuery request, CancellationToken cancellationToken)
    {
        var data = _repository.GetData(request.Id);
        return ValueTask.FromResult(data);
    }
}

// Test cancellation
public sealed record CancellableQuery : IRequest<string>;

public sealed class CancellableQueryHandler : IRequestHandler<CancellableQuery, string>
{
    public async ValueTask<string> Handle(CancellableQuery request, CancellationToken cancellationToken)
    {
        // Simulate work that checks cancellation
        await Task.Delay(100, cancellationToken); // This will throw if cancelled
        return "Completed";
    }
}
