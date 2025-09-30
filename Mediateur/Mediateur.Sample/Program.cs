using System;
using System.Threading;
using Mediateur;
using Mediateur.Sample;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("╔═══════════════════════════════════════╗");
Console.WriteLine("║     Mediateur Sample Application      ║");
Console.WriteLine("╚═══════════════════════════════════════╝");
Console.WriteLine();

// Create cancellation token source for graceful shutdown
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("\nShutdown requested...");
    cts.Cancel();
    e.Cancel = true;
};

try
{
    // Build the service provider with the generated mediator
    await using var serviceProvider = new ServiceCollection()
        .AddCompiledMediator()
        .BuildServiceProvider();

    // Get the mediator
    var mediator = serviceProvider.GetRequiredService<IMediator>();

    // Example 1: Query with response
    Console.WriteLine("┌─────────────────────────────────────┐");
    Console.WriteLine("│ Example 1: Query (GetUserQuery)    │");
    Console.WriteLine("└─────────────────────────────────────┘");

    var userId = Guid.NewGuid();
    var userQuery = new GetUserQuery(userId);
    var user = await mediator.Send(userQuery, cts.Token);

    Console.WriteLine($"✓ Result: {user}");
    Console.WriteLine();

    // Example 2: Command (void response)
    Console.WriteLine("┌──────────────────────────────────────────┐");
    Console.WriteLine("│ Example 2: Command (UpdateEmailCommand) │");
    Console.WriteLine("└──────────────────────────────────────────┘");

    var updateCommand = new UpdateEmailCommand(userId, "newemail@example.com");
    await mediator.Send(updateCommand, cts.Token);

    Console.WriteLine($"✓ Updated email for user {userId:N}");
    Console.WriteLine();

    // Example 3: Notification (pub/sub pattern)
    Console.WriteLine("┌───────────────────────────────────────────────┐");
    Console.WriteLine("│ Example 3: Notification (Pub/Sub Pattern)    │");
    Console.WriteLine("└───────────────────────────────────────────────┘");

    var notification = new UserEmailUpdatedNotification(
        userId,
        "john.doe@example.com",
        "newemail@example.com");

    await mediator.Publish(notification, cts.Token);
    Console.WriteLine();

    Console.WriteLine("╔═══════════════════════════════════════╗");
    Console.WriteLine("║  All examples completed successfully! ║");
    Console.WriteLine("╚═══════════════════════════════════════╝");
}
catch (OperationCanceledException)
{
    Console.WriteLine("⚠ Operation cancelled by user");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.WriteLine($"  Type: {ex.GetType().Name}");
    Console.WriteLine();
    Console.WriteLine("Stack Trace:");
    Console.WriteLine(ex.StackTrace);
    return 1; // Exit with error code
}

return 0; // Success
