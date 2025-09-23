using Microsoft.Extensions.DependencyInjection;

namespace Helldivers2ModManager;

[AttributeUsage(AttributeTargets.Class)]
internal sealed class RegisterServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
    
    public Type? Contract { get; init; }
}