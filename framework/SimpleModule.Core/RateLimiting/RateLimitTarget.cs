using System.Text.Json.Serialization;

namespace SimpleModule.Core.RateLimiting;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RateLimitTarget
{
    Ip,
    User,
    IpAndUser,
    Global,
}
