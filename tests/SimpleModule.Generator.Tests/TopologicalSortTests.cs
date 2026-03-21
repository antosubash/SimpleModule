using System.Collections.Immutable;
using FluentAssertions;

namespace SimpleModule.Generator.Tests;

public class TopologicalSortTests
{
    private static ImmutableArray<string> Arr(params string[] items) => items.ToImmutableArray();

    private static ImmutableArray<(string, string)> Edges(params (string, string)[] items) =>
        items.ToImmutableArray();

    [Fact]
    public void NoDependencies_ReturnsOriginalOrder()
    {
        var result = TopologicalSort.Sort(Arr("A", "B", "C"), Edges());
        result.IsSuccess.Should().BeTrue();
        result.Sorted.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void LinearDependency_ReturnsDependencyOrder()
    {
        // C depends on B, B depends on A → order: A, B, C
        var result = TopologicalSort.Sort(Arr("C", "B", "A"), Edges(("C", "B"), ("B", "A")));
        result.IsSuccess.Should().BeTrue();
        result.Sorted.IndexOf("A").Should().BeLessThan(result.Sorted.IndexOf("B"));
        result.Sorted.IndexOf("B").Should().BeLessThan(result.Sorted.IndexOf("C"));
    }

    [Fact]
    public void DiamondDependency_ResolvesCorrectly()
    {
        // D depends on B and C, B depends on A, C depends on A
        var result = TopologicalSort.Sort(
            Arr("D", "B", "C", "A"),
            Edges(("D", "B"), ("D", "C"), ("B", "A"), ("C", "A"))
        );

        result.IsSuccess.Should().BeTrue();
        result.Sorted.IndexOf("A").Should().BeLessThan(result.Sorted.IndexOf("B"));
        result.Sorted.IndexOf("A").Should().BeLessThan(result.Sorted.IndexOf("C"));
        result.Sorted.IndexOf("B").Should().BeLessThan(result.Sorted.IndexOf("D"));
        result.Sorted.IndexOf("C").Should().BeLessThan(result.Sorted.IndexOf("D"));
    }

    [Fact]
    public void SimpleCycle_DetectsCycle()
    {
        var result = TopologicalSort.Sort(Arr("A", "B"), Edges(("A", "B"), ("B", "A")));
        result.IsSuccess.Should().BeFalse();
        result.Cycle.Should().Contain("A");
        result.Cycle.Should().Contain("B");
    }

    [Fact]
    public void ThreeNodeCycle_DetectsCycle()
    {
        var result = TopologicalSort.Sort(
            Arr("A", "B", "C"),
            Edges(("A", "B"), ("B", "C"), ("C", "A"))
        );
        result.IsSuccess.Should().BeFalse();
        result.Cycle.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void PhaseAssignment_CorrectPhases()
    {
        // A (no deps) = phase 0, B (depends on A) = phase 1, C (depends on B) = phase 2
        var result = TopologicalSort.Sort(Arr("C", "B", "A"), Edges(("C", "B"), ("B", "A")));
        result.IsSuccess.Should().BeTrue();
        result.Phases["A"].Should().Be(0);
        result.Phases["B"].Should().Be(1);
        result.Phases["C"].Should().Be(2);
    }

    [Fact]
    public void DependenciesOf_TracksCorrectly()
    {
        var result = TopologicalSort.Sort(Arr("C", "B", "A"), Edges(("C", "B"), ("B", "A")));
        result.IsSuccess.Should().BeTrue();
        result.DependenciesOf["A"].Should().BeEmpty();
        result.DependenciesOf["B"].Should().ContainSingle().Which.Should().Be("A");
        result.DependenciesOf["C"].Should().ContainSingle().Which.Should().Be("B");
    }

    [Fact]
    public void UnknownNodeInEdge_IgnoredGracefully()
    {
        // Edge references a node not in the node list
        var result = TopologicalSort.Sort(Arr("A", "B"), Edges(("B", "A"), ("B", "Unknown")));
        result.IsSuccess.Should().BeTrue();
        result.Sorted.Should().HaveCount(2);
    }

    [Fact]
    public void EmptyNodes_ReturnsEmpty()
    {
        var result = TopologicalSort.Sort(
            ImmutableArray<string>.Empty,
            ImmutableArray<(string, string)>.Empty
        );
        result.IsSuccess.Should().BeTrue();
        result.Sorted.Should().BeEmpty();
    }

    [Fact]
    public void SingleNode_ReturnsSingleNode()
    {
        var result = TopologicalSort.Sort(
            ImmutableArray.Create("A"),
            ImmutableArray<(string, string)>.Empty
        );
        result.IsSuccess.Should().BeTrue();
        result.Sorted.Should().ContainSingle().Which.Should().Be("A");
        result.Phases["A"].Should().Be(0);
    }

    [Fact]
    public void SelfLoop_DetectsCycle()
    {
        var result = TopologicalSort.Sort(
            ImmutableArray.Create("A"),
            ImmutableArray.Create(("A", "A"))
        );
        result.IsSuccess.Should().BeFalse();
        result.Cycle.Should().Contain("A");
    }

    [Fact]
    public void DiamondDependency_PhaseAssignment()
    {
        // D depends on B and C, B depends on A, C depends on A
        var result = TopologicalSort.Sort(
            ImmutableArray.Create("D", "B", "C", "A"),
            ImmutableArray.Create(("D", "B"), ("D", "C"), ("B", "A"), ("C", "A"))
        );

        result.IsSuccess.Should().BeTrue();
        result.Phases["A"].Should().Be(0);
        result.Phases["B"].Should().Be(1);
        result.Phases["C"].Should().Be(1);
        result.Phases["D"].Should().Be(2);
    }

    [Fact]
    public void WideGraph_ManyIndependentModules()
    {
        // 5 independent modules, no dependencies
        var modules = ImmutableArray.Create("A", "B", "C", "D", "E");
        var result = TopologicalSort.Sort(modules, ImmutableArray<(string, string)>.Empty);

        result.IsSuccess.Should().BeTrue();
        result.Sorted.Should().HaveCount(5);
        // All should be phase 0
        foreach (var name in modules)
            result.Phases[name].Should().Be(0);
    }

    [Fact]
    public void DuplicateEdges_HandledGracefully()
    {
        var result = TopologicalSort.Sort(
            ImmutableArray.Create("A", "B"),
            ImmutableArray.Create(("B", "A"), ("B", "A"), ("B", "A"))
        );

        result.IsSuccess.Should().BeTrue();
        result.Sorted.IndexOf("A").Should().BeLessThan(result.Sorted.IndexOf("B"));
    }

    [Fact]
    public void LongChain_CorrectPhases()
    {
        // A -> B -> C -> D -> E (each depends on previous)
        var result = TopologicalSort.Sort(
            ImmutableArray.Create("E", "D", "C", "B", "A"),
            ImmutableArray.Create(("E", "D"), ("D", "C"), ("C", "B"), ("B", "A"))
        );

        result.IsSuccess.Should().BeTrue();
        result.Phases["A"].Should().Be(0);
        result.Phases["B"].Should().Be(1);
        result.Phases["C"].Should().Be(2);
        result.Phases["D"].Should().Be(3);
        result.Phases["E"].Should().Be(4);
    }

    [Fact]
    public void SortModules_WithDependencies_ReordersByDependency()
    {
        // ModuleB depends on ModuleA
        var data = new DiscoveryData(
            ImmutableArray.Create(
                new ModuleInfoRecord(
                    "global::B.BModule",
                    "ModuleB",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                ),
                new ModuleInfoRecord(
                    "global::A.AModule",
                    "ModuleA",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                )
            ),
            ImmutableArray<DtoTypeInfoRecord>.Empty,
            ImmutableArray<DbContextInfoRecord>.Empty,
            ImmutableArray<EntityConfigInfoRecord>.Empty,
            ImmutableArray.Create(new ModuleDependencyRecord("ModuleB", "ModuleA", "A.Contracts")),
            ImmutableArray<IllegalModuleReferenceRecord>.Empty,
            ImmutableArray<ContractInterfaceInfoRecord>.Empty,
            ImmutableArray<ContractImplementationRecord>.Empty,
            ImmutableArray<PermissionClassRecord>.Empty,
            ImmutableArray<InterceptorInfoRecord>.Empty
        );

        var result = TopologicalSort.SortModules(data);

        result.Should().HaveCount(2);
        // ModuleA should come before ModuleB
        var names = result.Select(m => m.ModuleName).ToList();
        names.IndexOf("ModuleA").Should().BeLessThan(names.IndexOf("ModuleB"));
    }

    [Fact]
    public void SortModules_WithCycle_ReturnsOriginalOrder()
    {
        var data = new DiscoveryData(
            ImmutableArray.Create(
                new ModuleInfoRecord(
                    "global::A.AModule",
                    "ModuleA",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                ),
                new ModuleInfoRecord(
                    "global::B.BModule",
                    "ModuleB",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                )
            ),
            ImmutableArray<DtoTypeInfoRecord>.Empty,
            ImmutableArray<DbContextInfoRecord>.Empty,
            ImmutableArray<EntityConfigInfoRecord>.Empty,
            ImmutableArray.Create(
                new ModuleDependencyRecord("ModuleA", "ModuleB", "B.Contracts"),
                new ModuleDependencyRecord("ModuleB", "ModuleA", "A.Contracts")
            ),
            ImmutableArray<IllegalModuleReferenceRecord>.Empty,
            ImmutableArray<ContractInterfaceInfoRecord>.Empty,
            ImmutableArray<ContractImplementationRecord>.Empty,
            ImmutableArray<PermissionClassRecord>.Empty,
            ImmutableArray<InterceptorInfoRecord>.Empty
        );

        var result = TopologicalSort.SortModules(data);

        // Falls back to original order on cycle
        result.Should().HaveCount(2);
        result[0].ModuleName.Should().Be("ModuleA");
        result[1].ModuleName.Should().Be("ModuleB");
    }

    [Fact]
    public void SortModules_NoDependencies_PreservesOriginalOrder()
    {
        var data = new DiscoveryData(
            ImmutableArray.Create(
                new ModuleInfoRecord(
                    "global::C.CModule",
                    "ModuleC",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                ),
                new ModuleInfoRecord(
                    "global::A.AModule",
                    "ModuleA",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                ),
                new ModuleInfoRecord(
                    "global::B.BModule",
                    "ModuleB",
                    false,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "",
                    "",
                    ImmutableArray<EndpointInfoRecord>.Empty,
                    ImmutableArray<ViewInfoRecord>.Empty
                )
            ),
            ImmutableArray<DtoTypeInfoRecord>.Empty,
            ImmutableArray<DbContextInfoRecord>.Empty,
            ImmutableArray<EntityConfigInfoRecord>.Empty,
            ImmutableArray<ModuleDependencyRecord>.Empty,
            ImmutableArray<IllegalModuleReferenceRecord>.Empty,
            ImmutableArray<ContractInterfaceInfoRecord>.Empty,
            ImmutableArray<ContractImplementationRecord>.Empty,
            ImmutableArray<PermissionClassRecord>.Empty,
            ImmutableArray<InterceptorInfoRecord>.Empty
        );

        var result = TopologicalSort.SortModules(data);

        result.Should().HaveCount(3);
        result[0].ModuleName.Should().Be("ModuleC");
        result[1].ModuleName.Should().Be("ModuleA");
        result[2].ModuleName.Should().Be("ModuleB");
    }
}
