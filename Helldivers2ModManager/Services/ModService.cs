using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Helldivers2ModManager.Services;

[RegisterService(ServiceLifetime.Transient)]
internal sealed class ModService
{
    public async Task TryAddModFromArchiveAsync(FileInfo archiveFile)
    {
    }

    public async Task RemoveAsync(object modData)
    {
    }

    public async Task DeployAsync(Guid[] guis)
    {
    }

    public async Task PurgeAsync()
    {
    }

    public async Task SaveEnabledAsync(SettingsService settings)
    {
    }
}