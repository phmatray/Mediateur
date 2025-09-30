using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mediateur.Sample;

/// <summary>
/// Handler for UpdateEmailCommand.
/// </summary>
public sealed class UpdateEmailCommandHandler : IRequestHandler<UpdateEmailCommand>
{
    public ValueTask<Unit> Handle(UpdateEmailCommand request, CancellationToken cancellationToken)
    {
        // Simulate updating in a database
        Console.WriteLine($"Updated email for user {request.UserId} to {request.NewEmail}");

        return ValueTask.FromResult(Unit.Value);
    }
}
