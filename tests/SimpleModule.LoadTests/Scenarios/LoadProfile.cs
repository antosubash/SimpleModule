using NBomber.Contracts;
using NBomber.CSharp;

namespace SimpleModule.LoadTests.Scenarios;

public sealed record LoadProfile(int Copies, TimeSpan RampDuration, TimeSpan SustainDuration)
{
    public static readonly LoadProfile Individual = new(
        Copies: 5,
        RampDuration: TimeSpan.FromSeconds(3),
        SustainDuration: TimeSpan.FromSeconds(10)
    );

    public static readonly LoadProfile Combined = new(
        Copies: 5,
        RampDuration: TimeSpan.FromSeconds(3),
        SustainDuration: TimeSpan.FromSeconds(12)
    );

    public LoadSimulation[] ToSimulations() =>
        [
            Simulation.RampingConstant(copies: Copies, during: RampDuration),
            Simulation.KeepConstant(copies: Copies, during: SustainDuration),
        ];
}
