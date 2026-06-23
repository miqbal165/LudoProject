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

    public GameController(List<IPlayer> players, IBoard board, IDice dice, Random rng)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(dice);
        ArgumentNullException.ThrowIfNull(rng);

        if (players.Count < 2 || players.Count > 4)
        {
            throw new ArgumentException("Minimum players must be 2 and maximum is 4.");
        }

        if (players.Select(p => p.Color).Distinct().Count() != players.Count)
        {
            throw new ArgumentException("Each player must choose a different color.");
        }

        _players = players;
        _board = board;
        _dice = dice;
        _rng = rng;
        _playerPawns = new Dictionary<IPlayer, List<IPawn>>();
        _currentPlayerIndex = 0;
        _extraRollPending = false;
        _consecutiveSixes = 0;
        _currentPhase = TurnPhase.WaitingToStart;

        foreach (var player in _players)
        {
            var pawns = new List<IPawn>();
            var basePositions = _board.GetBasePositions(player.Color);

            for (int pawnId = 0; pawnId < 4; pawnId++)
            {
                IPawn pawn = new Pawn(pawnId, player.Color);
                pawns.Add(pawn);

                if (_board.GetCell(basePositions[pawnId]) is Cell baseCell)
                {
                    baseCell.AddPawn(pawn);
                }
            }

            _playerPawns[player] = pawns;
        }
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
        return _players[_currentPlayerIndex];
    }

    public IReadOnlyList<IPawn> GetMovablePawns()
    {
        return null;
    }

    public GameState GetGameState()
    {
        // make a copy of each player's pawn list
        var pawnSnapshot = _playerPawns.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<IPawn>) pair.Value.ToList().AsReadOnly()
            );

        return new GameState(
                _currentPhase,
                GetCurrentPlayer(),
                GetPlayers(),
                pawnSnapshot,
                _dice.CurrentValue,
                _extraRollPending,
                GetMovablePawns(),
                _players.FirstOrDefault(player => player.IsFinished)
            );
    }

    public IReadOnlyList<IPlayer> GetPlayers()
    {
        return _players.ToList().AsReadOnly();
    }

    public bool CanRoll()
    {
        return _currentPhase == TurnPhase.Rolling;
    }

    private void NextTurn()
    {
        _extraRollPending = false;
        _consecutiveSixes = 0;
        _currentPhase = TurnPhase.Rolling;

        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } while (_players[_currentPlayerIndex].IsFinished);
        
        
        
    }

    private void CheckWinCondition()
    {
        
    }

    private void HandleCapture(IPawn target)
    {
        
    }

    private Position GetCurrentPosition(IPawn pawn)
    {
        throw new ArgumentException();
    }

    private void CheckAndHandleCapture(ICell cell, Color attackerColor)
    {
        
    }

    private bool IsValidMove(IPawn pawn, int steps)
    {
        throw new ArgumentException();
    }

    private bool IsPathBlocked(Position targetPosition, Color color)
    {
        return false;
    }

    private void MovePawnAlongPath(IPawn pawn, int steps)
    {
        
    }

    private int PerformRoll()
    {
        var value = _rng.Next(1, 7);
        _dice.CurrentValue = value;
        return value;
    }

    public void BroadcastState()
    {
        OnStateChange?.Invoke(GetGameState());
    }
    
}