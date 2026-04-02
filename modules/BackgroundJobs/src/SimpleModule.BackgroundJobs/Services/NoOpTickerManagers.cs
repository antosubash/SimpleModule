using System.Reflection;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace SimpleModule.BackgroundJobs.Services;

/// <summary>
/// Creates no-op implementations of TickerQ manager interfaces via DispatchProxy.
/// Used in test environments where TickerQ is not initialized.
/// </summary>
internal static class NoOpTickerManagerFactory
{
    public static ITimeTickerManager<TimeTickerEntity> CreateTimeManager() =>
        DispatchProxy.Create<ITimeTickerManager<TimeTickerEntity>, NoOpProxy>();

    public static ICronTickerManager<CronTickerEntity> CreateCronManager() =>
        DispatchProxy.Create<ICronTickerManager<CronTickerEntity>, NoOpProxy>();
}

/// <summary>
/// DispatchProxy that returns completed tasks for all async methods.
/// </summary>
public class NoOpProxy : DispatchProxy
{
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var returnType = targetMethod?.ReturnType;
        if (returnType == typeof(Task))
            return Task.CompletedTask;
        if (returnType is not null && returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [defaultValue]);
        }
        return null;
    }
}
