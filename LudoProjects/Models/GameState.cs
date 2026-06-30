using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class GameState(
    TurnPhase phase,
    IPlayer currentPlayer,
    IReadOnlyList<IPlayer> players,
    IReadOnlyDictionary<IPlayer, IReadOnlyList<IPawn>> playerPawns,
    int lastDiceValue,
    bool extraRollPending,
    IReadOnlyList<IPawn> movablePawns,
    IPlayer? winner)
{
    public TurnPhase Phase { get; } = phase;
    public IPlayer CurrentPlayer { get; } = currentPlayer;
    public IReadOnlyList<IPlayer> Players { get; } = players;
    public IReadOnlyDictionary<IPlayer, IReadOnlyList<IPawn>> PlayerPawns { get; } = playerPawns;
    public int LastDiceValue { get; } = lastDiceValue;
    public bool ExtraRollPending { get; } = extraRollPending;
    public IReadOnlyList<IPawn> MovablePawns { get; } = movablePawns;
    public IPlayer? Winner { get; } = winner;
}