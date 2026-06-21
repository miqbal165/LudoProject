using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class GameState
{
    public TurnPhase Phase { get; }
    public IPlayer CurrentPlayer { get; }
    public IReadOnlyList<IPlayer> Players { get; }
    public IReadOnlyDictionary<IPlayer, IReadOnlyList<IPawn>> PlayerPawns { get; }
    public int LastDiceValue { get; }
    public bool ExtraRollPending { get; }
    public IReadOnlyList<IPawn> MovablePawns { get; }
    public IPlayer? Winner { get; }

    public GameState(
        TurnPhase phase,
        IPlayer currentPlayer,
        IReadOnlyList<IPlayer> players,
        IReadOnlyDictionary<IPlayer, IReadOnlyList<IPawn>> playerPawns,
        int lastDiceValue,
        bool extraRollPending,
        IReadOnlyList<IPawn> movablePawns,
        IPlayer? winner)
    {
        Phase = phase;
        CurrentPlayer = currentPlayer;
        Players = players;
        PlayerPawns = playerPawns;
        LastDiceValue = lastDiceValue;
        ExtraRollPending = extraRollPending;
        MovablePawns = movablePawns;
        Winner = winner;
    }
}