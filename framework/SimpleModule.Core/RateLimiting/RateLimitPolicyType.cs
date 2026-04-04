using System.Text.Json.Serialization;

namespace SimpleModule.Core.RateLimiting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RateLimitPolicyType
{
    FixedWindow,
    SlidingWindow,
    TokenBucket,
}
