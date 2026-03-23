using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace bashGPT.Server;

/// <summary>
/// Resolves controllers from the DI container as singletons, enabling factory-delegate
/// registration with optional (nullable) service dependencies.
/// </summary>
internal sealed class SingletonControllerActivator : IControllerActivator
{
    public object Create(ControllerContext context)
        => context.HttpContext.RequestServices.GetRequiredService(
            context.ActionDescriptor.ControllerTypeInfo.AsType());

    public void Release(ControllerContext context, object controller) { }

    public ValueTask ReleaseAsync(ControllerContext context, object controller)
        => ValueTask.CompletedTask;
}
