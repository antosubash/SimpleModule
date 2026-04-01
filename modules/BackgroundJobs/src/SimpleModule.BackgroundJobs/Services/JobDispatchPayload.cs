namespace SimpleModule.BackgroundJobs.Services;

public record JobDispatchPayload(string JobTypeName, string? SerializedData);
