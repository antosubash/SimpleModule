using Microsoft.AspNetCore.Components;

namespace SimpleModule.Blazor.Inertia;

public class InertiaOptions
{
    public Type ShellComponent { get; set; } = typeof(Components.InertiaShell);
}
