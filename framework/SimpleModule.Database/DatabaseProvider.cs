using NetEscapades.EnumGenerators;

namespace SimpleModule.Database;

[EnumExtensions]
public enum DatabaseProvider
{
    Sqlite,
    PostgreSql,
    SqlServer,
}
