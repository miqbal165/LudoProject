using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Controllers;

public class GameController
{
    private readonly List<IPlayer> _players;
    private readonly Dictionary<IPlayer, List<IPawn>> _playerPawns;
    private int _currentPlayerIndex;
    private readonly IBoard _board;
    private readonly IDice _dice;
    private bool _extraRollPending;
    private int _consecutiveSixes;
    private TurnPhase _currentPhase;
    private readonly Random _rng;
    
    public Action<GameState> OnStateChange { get; set; }
    public Action<IPlayer> OnPlayerWon { get; set; }

    public GameController()
    {
        
    }

    public void StartGame()
    {
        
    }

    public void RollDice()
    {
        
    }

    public void SelectPawn(int pawnId)
    {
        
    }

    public IPlayer GetCurrentPlayer()
    {
        throw new ArgumentException();
    }

    public IReadOnlyList<IPawn> GetMovablePawns()
    {
        throw new ArgumentException();
    }
    
    
}