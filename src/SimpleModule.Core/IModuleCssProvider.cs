using System.Collections.Generic;

namespace SimpleModule.Core;

public interface IModuleCssProvider
{
    IReadOnlyList<string> CssPaths { get; }
}
