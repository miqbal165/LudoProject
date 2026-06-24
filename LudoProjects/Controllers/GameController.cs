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
        if (_currentPhase != TurnPhase.WaitingToStart)
        {
            return;
        }

        _currentPlayerIndex = 0;
        _extraRollPending = false;
        _consecutiveSixes = 0;
        _dice.CurrentValue = 0;
        _currentPhase = TurnPhase.Rolling;
        BroadcastState();
    }

    public void RollDice()
    {
        if (!CanRoll())
        {
            return;
        }

        int rolledValue = PerformRoll();
        _consecutiveSixes = rolledValue == 6 ? _consecutiveSixes + 1 : 0;

        if (_consecutiveSixes == 3)
        {
            _extraRollPending = false;
            NextTurn();
            BroadcastState();
            return;
        }

        _extraRollPending = rolledValue == 6;
        _currentPhase = TurnPhase.SelectingPawn;
        
        var movablePawns = GetMovablePawns();
        
        if (movablePawns.Count == 0)
        {
            if (_extraRollPending)
                _currentPhase = TurnPhase.Rolling;
            else
                NextTurn();
        }
        else
        {
            var currentPawns = _playerPawns[GetCurrentPlayer()];
            var allPawnsInBase = currentPawns.All(pawn => pawn.Status == PawnStatus.InBase);

            // If all the pawns are still on the base and a 6 is scored,
            // the first pawn is automatically played. If only one pawn is valid,
            // it is automatically played.
            if (allPawnsInBase || movablePawns.Count == 1)
            {
                var automaticPawn = movablePawns.OrderBy(pawn => pawn.Id).First();
                MovePawnAlongPath(automaticPawn, rolledValue);
                CheckWinCondition();

                if (_currentPhase != TurnPhase.GameOver)
                {
                    if (_extraRollPending)
                    {
                        _currentPhase = TurnPhase.Rolling;
                    }
                    else
                    {
                        NextTurn();
                    }
                }
            }
        }

        BroadcastState();
    }

    public void SelectPawn(int pawnId)
    {
        if (_currentPhase != TurnPhase.SelectingPawn)
        {
            return;
        }

        var selectedPawn = GetMovablePawns().FirstOrDefault(pawn => pawn.Id == pawnId);

        if (selectedPawn is null)
        {
            return;
        }
        
        MovePawnAlongPath(selectedPawn, _dice.CurrentValue);
        CheckWinCondition();

        if (_currentPhase != TurnPhase.GameOver)
        {
            if (_extraRollPending)
            {
                _currentPhase = TurnPhase.Rolling;
            }
            else
            {
                NextTurn();
            }
        }
        
        BroadcastState();
    }

    public IPlayer GetCurrentPlayer()
    {
        return _players[_currentPlayerIndex];
    }

    public IReadOnlyList<IPawn> GetMovablePawns()
    {
        if (_currentPhase != TurnPhase.SelectingPawn || _dice.CurrentValue is < 1 or > 6)
        {
            return Array.Empty<IPawn>();
        }
        
        return _playerPawns[GetCurrentPlayer()]
            .Where(pawn => IsValidMove(pawn, _dice.CurrentValue))
            .ToList()
            .AsReadOnly();
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
        return pawn.Status switch
        {
            PawnStatus.InBase => _board.GetBasePositions(pawn.Color)[pawn.Id],
            PawnStatus.Finished => _board.GetCenterPosition(),
            _ => _board.GetFullPath(pawn.Color)[pawn.StepIndex]
        };
    }

    private void CheckAndHandleCapture(ICell cell, Color attackerColor)
    {
        
    }

    private bool IsValidMove(IPawn pawn, int steps)
    {
        if (steps is < 1 or > 6 || pawn.Status == PawnStatus.Finished)
        {
            return false;
        }

        if (pawn.Status == PawnStatus.InBase)
        {
            if (steps != 6)
            {
                return false;
            }
            
            return !IsPathBlocked(_board.GetStartPosition(pawn.Color), pawn.Color);
        }

        var path = _board.GetFullPath(pawn.Color);
        var finishIndex = path.Count - 1;
        var targetIndex = pawn.StepIndex + steps;

        // Aturan HomeColumn: pion boleh bergerak jika hasil dadu masih mencapai
        // cell sebelum finish atau tepat mencapai finish. Jika melewati finish,
        // langkah tidak valid dan pion tetap diam.
        if (targetIndex > finishIndex)
        {
            return false;
        }

        // Setiap cell yang dilewati diperiksa agar pion tidak dapat melompati
        // block tiga atau lebih pion sewarna milik lawan.
        for (var index = pawn.StepIndex + 1; index <= targetIndex; index++)
        {
            if (IsPathBlocked(path[index], pawn.Color))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPathBlocked(Position targetPosition, Color color)
    {
        return false;
    }

    private void MovePawnAlongPath(IPawn pawn, int steps)
    {
        var oldPosition = GetCurrentPosition(pawn);
        if (_board.GetCell(oldPosition) is Cell oldCell)
        {
            oldCell.RemovePawn(pawn);
        }

        if (pawn.Status == PawnStatus.InBase)
        {
            // Angka 6 mengeluarkan pion ke start, bukan maju enam cell.
            pawn.StepIndex = 0;
            pawn.Status = PawnStatus.OnBoard;
        }
        else
        {
            pawn.StepIndex += steps;
            var path = _board.GetFullPath(pawn.Color);
            var finishIndex = path.Count - 1;
            var homeColumnStartIndex = 52;

            if (pawn.StepIndex == finishIndex)
            {
                pawn.Status = PawnStatus.Finished;
            }
            else if (pawn.StepIndex >= homeColumnStartIndex)
            {
                pawn.Status = PawnStatus.InHomeColumn;
            }
            else
            {
                pawn.Status = PawnStatus.OnBoard;
            }
        }

        var targetPosition = GetCurrentPosition(pawn);
        var targetCell = _board.GetCell(targetPosition);
        CheckAndHandleCapture(targetCell, pawn.Color);

        if (targetCell is Cell mutableTargetCell)
        {
            mutableTargetCell.AddPawn(pawn);
        }
    }

    private int PerformRoll()
    {
        var value = _rng.Next(1, 7);
        _dice.CurrentValue = value;
        return value;
    }

    private void BroadcastState()
    {
        OnStateChange?.Invoke(GetGameState());
    }
    
}