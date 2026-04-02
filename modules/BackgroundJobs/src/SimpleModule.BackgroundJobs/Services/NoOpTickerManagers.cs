using System.Collections.Concurrent;
using System.Reflection;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace SimpleModule.BackgroundJobs.Services;

internal static class NoOpTickerManagerFactory
{
    public static ITimeTickerManager<TimeTickerEntity> CreateTimeManager() =>
        DispatchProxy.Create<ITimeTickerManager<TimeTickerEntity>, NoOpProxy>();

    public static ICronTickerManager<CronTickerEntity> CreateCronManager() =>
        DispatchProxy.Create<ICronTickerManager<CronTickerEntity>, NoOpProxy>();
}

// DispatchProxy.Create requires public TProxy — CLR constraint
public class NoOpProxy : DispatchProxy
{
    private static readonly MethodInfo FromResultMethod = typeof(Task).GetMethod(
        nameof(Task.FromResult)
    )!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> FromResultCache = new();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var returnType = targetMethod?.ReturnType;
        if (returnType == typeof(Task))
            return Task.CompletedTask;
        if (
            returnType is { IsGenericType: true }
            && returnType.GetGenericTypeDefinition() == typeof(Task<>)
        )
        {
            var resultType = returnType.GetGenericArguments()[0];
            var constructed = FromResultCache.GetOrAdd(
                resultType,
                t => FromResultMethod.MakeGenericMethod(t)
            );
            var defaultValue = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return constructed.Invoke(null, [defaultValue]);
        }
        return null;
    }
}
