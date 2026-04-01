namespace SimpleModule.BackgroundJobs.Contracts;

public interface IModuleJob
{
    Task ExecuteAsync(IJobExecutionContext context, CancellationToken cancellationToken);
}
