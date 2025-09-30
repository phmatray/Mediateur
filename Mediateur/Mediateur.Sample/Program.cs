using System;
using System.Threading.Tasks;
using Mediateur;
using Mediateur.Sample;
using Microsoft.Extensions.DependencyInjection;

// Build the service provider with the generated mediator
var services = new ServiceCollection();
services.AddCompiledMediator();

var serviceProvider = services.BuildServiceProvider();

// Get the mediator
var mediator = serviceProvider.GetRequiredService<IMediator>();

Console.WriteLine("=== Mediateur Sample ===\n");

// Example 1: Query with logging
Console.WriteLine("1. Executing GetUserQuery (with logging):");
var userId = Guid.NewGuid();
var userQuery = new GetUserQuery(userId);
var user = await mediator.Send(userQuery);
Console.WriteLine($"   Result: {user}\n");

// Example 2: Command with validation and logging
Console.WriteLine("2. Executing UpdateEmailCommand (with validation + logging):");
var updateCommand = new UpdateEmailCommand(userId, "newemail@example.com");
await mediator.Send(updateCommand);
Console.WriteLine();

// Example 3: Notification (pub/sub)
Console.WriteLine("3. Publishing UserEmailUpdatedNotification:");
var notification = new UserEmailUpdatedNotification(userId, "john.doe@example.com", "newemail@example.com");
await mediator.Publish(notification);
Console.WriteLine();

Console.WriteLine("=== All examples completed successfully! ===");
