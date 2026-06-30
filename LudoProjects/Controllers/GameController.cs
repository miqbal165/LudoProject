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
        _players = players;
        _board = board;
        _dice = dice;
        _rng = rng;
        _playerPawns = new Dictionary<IPlayer, List<IPawn>>();
        _currentPlayerIndex = 0;
        _extraRollPending = false;
        _consecutiveSixes = 0;
        _currentPhase = TurnPhase.WaitingToStart;

        foreach (IPlayer player in _players)
        {
            List<IPawn> pawns = [];
            IReadOnlyList<Position> basePositions = _board.GetBasePositions(player.Color);

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
        
        IReadOnlyList<IPawn> movablePawns = GetMovablePawns();
        
        if (movablePawns.Count == 0)
        {
            if (_extraRollPending)
                _currentPhase = TurnPhase.Rolling;
            else
                NextTurn();
        }
        else
        {
            List<IPawn> currentPawns = _playerPawns[GetCurrentPlayer()];
            bool allPawnsInBase = currentPawns.All(pawn => pawn.Status == PawnStatus.InBase);
            
            if (allPawnsInBase || movablePawns.Count == 1)
            {
                IPawn automaticPawn = movablePawns.OrderBy(pawn => pawn.Id).First();
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

        IPawn? selectedPawn = GetMovablePawns().FirstOrDefault(pawn => pawn.Id == pawnId);

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
        // Make a copy of each player's piece list.
        Dictionary<IPlayer,IReadOnlyList<IPawn>> pawnSnapshot = _playerPawns.ToDictionary(
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
        IPlayer currentPlayer = GetCurrentPlayer();

        if (!_playerPawns[currentPlayer].All(pawn => pawn.Status == PawnStatus.Finished))
        {
            return;
        }

        currentPlayer.IsFinished = true;
        _currentPhase = TurnPhase.GameOver;
        OnPlayerWon?.Invoke(currentPlayer);
    }

    private void HandleCapture(IPawn target)
    {
        Position oldPosition = GetCurrentPosition(target);
        if (_board.GetCell(oldPosition) is Cell oldCell)
        {
            oldCell.RemovePawn(target);
        }

        target.Status = PawnStatus.InBase;
        target.StepIndex = -1;
        Position basePosition = _board.GetBasePositions(target.Color)[target.Id];
        if (_board.GetCell(basePosition) is Cell baseCell)
        {
            baseCell.AddPawn(target);
        }
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
        if (cell.Type is CellType.Start or CellType.Protected 
            or CellType.HomeColumn or CellType.Base or CellType.Center)
        {
            return;
        }
        
        List<IPawn> capturedPawns = cell.OccupyingPawns
            .Where(pawn => pawn.Color != attackerColor)
            .ToList();

        foreach (IPawn capturedPawn in capturedPawns)
        {
            HandleCapture(capturedPawn);
        }
    }

    private bool IsValidMove(IPawn pawn, int steps)
    {
        if (steps is < 1 or > 6)
        {
            return false;
        }

        if (pawn.Status == PawnStatus.Finished)
        {
            return false;
        }

        IReadOnlyList<Position> path = _board.GetFullPath(pawn.Color);

        // Pawns that are still on the base can only come out with the number 6.
        if (pawn.Status == PawnStatus.InBase)
        {
            if (steps != 6)
            {
                return false;
            }

            Position startPosition = path[0];

            // You may not leave if your opponent is blocking the start.
            return !IsPathBlocked(startPosition, pawn.Color);
        }

        int targetIndex = pawn.StepIndex + steps;

        // Pawns may not move past the finish.
        if (targetIndex >= path.Count)
        {
            return false;
        }

        // Check each cell you pass so you don't jump over a blockade.
        for (int index = pawn.StepIndex + 1;
             index <= targetIndex;
             index++)
        {
            Position position = path[index];

            if (IsPathBlocked(position, pawn.Color))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPathBlocked(Position targetPosition, Color color)
    {
        ICell cell = _board.GetCell(targetPosition);

        if (cell.Type != CellType.Normal)
        {
            return false;
        }

        return cell.OccupyingPawns
            .Where(pawn => pawn.Color != color)
            .GroupBy(pawn => pawn.Color)
            .Any(group => group.Count() >= 2);
    }

    private void MovePawnAlongPath(IPawn pawn, int steps)
    {
        IReadOnlyList<Position> path = _board.GetFullPath(pawn.Color);

        // Specify the target index before removing the pawn from the old cell.
        int targetIndex = pawn.Status == PawnStatus.InBase
            ? 0
            : pawn.StepIndex + steps;

        // A safety measure to prevent the pawn from passing the finish.
        if (targetIndex < 0 || targetIndex >= path.Count)
        {
            return;
        }

        Position oldPosition = GetCurrentPosition(pawn);

        if (_board.GetCell(oldPosition) is Cell oldCell)
        {
            oldCell.RemovePawn(pawn);
        }
        pawn.StepIndex = targetIndex;

        // Calculate Home Column limits dynamically.
        int finishIndex = path.Count - 1;

        int homeColumnCount = _board.GetHomeColumnPositions(pawn.Color).Count;

        int homeColumnStartIndex = finishIndex - homeColumnCount;

        // Updates pawn status based on position on the path.
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

        Position targetPosition = path[pawn.StepIndex];
        ICell targetCell = _board.GetCell(targetPosition);

        CheckAndHandleCapture(targetCell, pawn.Color);

        if (targetCell is Cell mutableTargetCell)
        {
            mutableTargetCell.AddPawn(pawn);
        }
    }

    private int PerformRoll()
    {
        int value = _rng.Next(1, 7);
        _dice.CurrentValue = value;
        return value;
    }

    private void BroadcastState()
    {
        OnStateChange?.Invoke(GetGameState());
    }
}