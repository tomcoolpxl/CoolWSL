using CoolWSL.Core.Models;

namespace CoolWSL.Core.Abstractions;

public interface IWslGlobalConfigService
{
    Task<WslGlobalConfigDocument> ReadAsync(CancellationToken cancellationToken = default);

    WslConfigValidationResult Validate(string content);

    Task<WslGlobalConfigSaveResult> SaveAsync(string content, CancellationToken cancellationToken = default);
}
