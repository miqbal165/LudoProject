using LudoProjects.Interfaces;

namespace LudoProjects.Tests.TestSupport;

internal sealed record ValidConstructorInputs(
    List<IPlayer> Players,
    Dictionary<IPlayer, List<IPawn>> PlayerPawns,
    IBoard Board,
    IDice Dice,
    Random Random);