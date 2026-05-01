using CoolWSL.Core.Models;

namespace CoolWSL.Core.Abstractions;

public interface IWslCommandService
{
    Task<CommandResult> ExecuteAsync(WslCommand command, CancellationToken cancellationToken = default);
}