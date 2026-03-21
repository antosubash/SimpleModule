using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SimpleModule.Generator;

internal readonly struct SortResult
{
    public bool IsSuccess { get; }
    public ImmutableArray<string> Sorted { get; }
    public ImmutableArray<string> Cycle { get; }
    public Dictionary<string, int> Phases { get; }
    public Dictionary<string, ImmutableArray<string>> DependenciesOf { get; }

    public SortResult(
        bool isSuccess,
        ImmutableArray<string> sorted,
        ImmutableArray<string> cycle,
        Dictionary<string, int> phases,
        Dictionary<string, ImmutableArray<string>> dependenciesOf
    )
    {
        IsSuccess = isSuccess;
        Sorted = sorted;
        Cycle = cycle;
        Phases = phases;
        DependenciesOf = dependenciesOf;
    }
}

internal static class TopologicalSort
{
    /// <summary>
    /// Topologically sorts nodes given dependency edges.
    /// Edges are (From, To) meaning "From depends on To" — To must come first.
    /// </summary>
    internal static SortResult Sort(
        ImmutableArray<string> nodes,
        ImmutableArray<(string From, string To)> edges
    )
    {
        var nodeSet = new HashSet<string>(nodes);

        // Build adjacency list: To -> list of From (To enables From)
        // and in-degree map (how many dependencies each node has)
        var adjacency = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();
        var dependenciesOf = new Dictionary<string, List<string>>();

        foreach (var node in nodes)
        {
            adjacency[node] = new List<string>();
            inDegree[node] = 0;
            dependenciesOf[node] = new List<string>();
        }

        foreach (var (from, to) in edges)
        {
            if (!nodeSet.Contains(from) || !nodeSet.Contains(to))
                continue;

            adjacency[to].Add(from);
            inDegree[from]++;
            dependenciesOf[from].Add(to);
        }

        // Kahn's algorithm
        var queue = new Queue<string>();
        var phases = new Dictionary<string, int>();

        foreach (var node in nodes)
        {
            if (inDegree[node] == 0)
            {
                queue.Enqueue(node);
                phases[node] = 0;
            }
        }

        var sorted = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sorted.Add(current);

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                {
                    // Phase = max(phase of all dependencies) + 1
                    var maxDepPhase = 0;
                    foreach (var dep in dependenciesOf[neighbor])
                    {
                        if (phases.TryGetValue(dep, out var depPhase) && depPhase > maxDepPhase)
                            maxDepPhase = depPhase;
                    }

                    phases[neighbor] = maxDepPhase + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        var depsOfResult = new Dictionary<string, ImmutableArray<string>>();
        foreach (var kvp in dependenciesOf)
            depsOfResult[kvp.Key] = kvp.Value.ToImmutableArray();

        if (sorted.Count != nodes.Length)
        {
            // Cycle detected — use DFS to find the actual cycle
            var cycle = FindCycle(nodes, edges, nodeSet);
            return new SortResult(false, ImmutableArray<string>.Empty, cycle, phases, depsOfResult);
        }

        return new SortResult(
            true,
            sorted.ToImmutableArray(),
            ImmutableArray<string>.Empty,
            phases,
            depsOfResult
        );
    }

    private static ImmutableArray<string> FindCycle(
        ImmutableArray<string> nodes,
        ImmutableArray<(string From, string To)> edges,
        HashSet<string> nodeSet
    )
    {
        // Build dependency adjacency: From -> list of To (From depends on To)
        var depAdj = new Dictionary<string, List<string>>();
        foreach (var node in nodes)
            depAdj[node] = new List<string>();

        foreach (var (from, to) in edges)
        {
            if (!nodeSet.Contains(from) || !nodeSet.Contains(to))
                continue;
            depAdj[from].Add(to);
        }

        // 3-color DFS: 0=unvisited, 1=in-stack, 2=done
        var color = new Dictionary<string, int>();
        foreach (var node in nodes)
            color[node] = 0;

        var stack = new List<string>();

        foreach (var node in nodes)
        {
            if (color[node] == 0)
            {
                var cycle = DfsFindCycle(node, depAdj, color, stack);
                if (cycle.Length > 0)
                    return cycle;
            }
        }

        return ImmutableArray<string>.Empty;
    }

    private static ImmutableArray<string> DfsFindCycle(
        string node,
        Dictionary<string, List<string>> adj,
        Dictionary<string, int> color,
        List<string> stack
    )
    {
        color[node] = 1;
        stack.Add(node);

        foreach (var neighbor in adj[node])
        {
            if (!color.ContainsKey(neighbor))
                continue;

            if (color[neighbor] == 1)
            {
                // Found cycle — extract from stack
                var cycleStart = stack.IndexOf(neighbor);
                var cycle = new List<string>();
                for (var i = cycleStart; i < stack.Count; i++)
                    cycle.Add(stack[i]);
                return cycle.ToImmutableArray();
            }

            if (color[neighbor] == 0)
            {
                var cycle = DfsFindCycle(neighbor, adj, color, stack);
                if (cycle.Length > 0)
                    return cycle;
            }
        }

        stack.RemoveAt(stack.Count - 1);
        color[node] = 2;
        return ImmutableArray<string>.Empty;
    }

    /// <summary>
    /// Convenience: sorts DiscoveryData.Modules using DiscoveryData.Dependencies.
    /// Returns modules in dependency order. Falls back to original order on cycle.
    /// </summary>
    internal static ImmutableArray<ModuleInfoRecord> SortModules(DiscoveryData data)
    {
        var moduleNames = data.Modules.Select(m => m.ModuleName).ToImmutableArray();
        var depEdges = data
            .Dependencies.Select(d => (d.ModuleName, d.DependsOnModuleName))
            .ToImmutableArray();

        var sortResult = Sort(moduleNames, depEdges);

        if (!sortResult.IsSuccess)
            return data.Modules;

        var moduleByName = new Dictionary<string, ModuleInfoRecord>();
        foreach (var m in data.Modules)
            moduleByName[m.ModuleName] = m;

        var sorted = new List<ModuleInfoRecord>();
        foreach (var name in sortResult.Sorted)
        {
            if (moduleByName.TryGetValue(name, out var m))
                sorted.Add(m);
        }

        return sorted.ToImmutableArray();
    }
}
