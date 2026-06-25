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
        // TODO: Logic untuk menentukan pemenangnya
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

        foreach (var capturedPawn in capturedPawns)
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

        var path = _board.GetFullPath(pawn.Color);

        // Pion yang masih di base hanya dapat keluar dengan angka 6.
        if (pawn.Status == PawnStatus.InBase)
        {
            if (steps != 6)
            {
                return false;
            }

            var startPosition = path[0];

            // Tidak boleh keluar jika start diblokade lawan.
            return !IsPathBlocked(startPosition, pawn.Color);
        }

        var targetIndex = pawn.StepIndex + steps;

        // Pion tidak boleh bergerak melewati finish.
        if (targetIndex >= path.Count)
        {
            return false;
        }

        // Periksa setiap cell yang dilewati agar tidak melompati blockade.
        for (var index = pawn.StepIndex + 1;
             index <= targetIndex;
             index++)
        {
            var position = path[index];

            if (IsPathBlocked(position, pawn.Color))
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
        var path = _board.GetFullPath(pawn.Color);

        // Tentukan index tujuan sebelum menghapus pion dari cell lama.
        var targetIndex = pawn.Status == PawnStatus.InBase
            ? 0
            : pawn.StepIndex + steps;

        // A safety measure to prevent the pawn from passing the finish.
        if (targetIndex < 0 || targetIndex >= path.Count)
        {
            return;
        }

        var oldPosition = GetCurrentPosition(pawn);

        if (_board.GetCell(oldPosition) is Cell oldCell)
        {
            oldCell.RemovePawn(pawn);
        }

        // Perbarui index pion.
        pawn.StepIndex = targetIndex;

        // Hitung batas Home Column secara dinamis.
        var finishIndex = path.Count - 1;

        var homeColumnCount =
            _board.GetHomeColumnPositions(pawn.Color).Count;

        var homeColumnStartIndex =
            finishIndex - homeColumnCount;

        // Perbarui status pion berdasarkan posisinya di path.
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

        var targetPosition = path[pawn.StepIndex];
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