using LudoProjects.Interfaces;

namespace LudoProjects.Tests.TestSupport;

internal record ValidConstructorInputs(
    List<IPlayer> Players,
    Dictionary<IPlayer, List<IPawn>> PlayerPawns,
    IBoard Board,
    IDice Dice,
    Random Random);