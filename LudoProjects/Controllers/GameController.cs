using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Validators;
using Serilog;

namespace LudoProjects.Controllers;

public sealed class GameController
{
    private readonly List<IPlayer> _players;
    private readonly Dictionary<IPlayer, List<IPawn>> _playerPawns;
    private readonly Dictionary<Color, IReadOnlyList<Position>> _pathCache;
    private readonly IBoard _board;
    private readonly IDice _dice;
    private readonly Random _randomDiceNumberGenerator;
    private readonly ILogger _logger;
    private int _currentPlayerIndex;
    private bool _extraRollPending;
    private int _consecutiveSixes;
    private TurnPhase _currentPhase;

    public Action<GameState>? OnStateChanged { get; set; }
    public Action<IPlayer>? OnPlayerWon { get; set; }

    public GameController(
        List<IPlayer> players,
        Dictionary<IPlayer, List<IPawn>> playerPawns,
        Dictionary<Color, IReadOnlyList<Position>> pathCache,
        IBoard board,
        IDice dice,
        Random randomDiceNumberGenerator,
        ILogger? logger = null)
    {
        GameControllerValidator.ValidateConstructorArguments(
            players,
            playerPawns,
            pathCache,
            board,
            dice,
            randomDiceNumberGenerator);

        _players = players;
        _playerPawns = playerPawns;
        _pathCache = pathCache;
        _board = board;
        _dice = dice;
        _randomDiceNumberGenerator = randomDiceNumberGenerator;
        _logger = (logger ?? Log.Logger).ForContext<GameController>();
        _currentPlayerIndex = 0;
        _extraRollPending = false;
        _consecutiveSixes = 0;
        _currentPhase = TurnPhase.WaitingToStart;

        _logger.Debug(
            "Game controller initialized with {PlayerCount} players",
            _players.Count);
    }

    public void StartGame()
    {
        if (_currentPhase != TurnPhase.WaitingToStart)
        {
            _logger.Warning(
                "Game start ignored because current phase is {CurrentPhase}",
                _currentPhase);
            return;
        }

        int totalColors = Enum.GetValues<Color>().Length;

        IReadOnlyList<ICell> startCells = GetCellsByType(CellType.Start);
        IReadOnlyList<ICell> protectedCells = GetCellsByType(CellType.Protected);
        IReadOnlyList<ICell> homeColumnCells = GetCellsByType(CellType.HomeColumn);
        IReadOnlyList<ICell> centerCells = GetCellsByType(CellType.Center);

        bool boardIsReady = startCells.Count == totalColors &&
                            protectedCells.Count == totalColors &&
                            homeColumnCells.Count == totalColors * 5 &&
                            centerCells.Count == 9;

        if (!boardIsReady)
        {
            _logger.Warning(
                "Game start rejected because the board is not ready. " +
                "StartCells={StartCellCount}, ProtectedCells={ProtectedCellCount}, " +
                "HomeColumnCells={HomeColumnCellCount}, CenterCells={CenterCellCount}",
                startCells.Count,
                protectedCells.Count,
                homeColumnCells.Count,
                centerCells.Count);
            return;
        }

        if (_pathCache.Count == 0)
        {
            BuildAllPaths();
            _logger.Debug(
                "Movement paths built for {PathColorCount} colors",
                _pathCache.Count);
        }

        _currentPlayerIndex = 0;
        _extraRollPending = false;
        _consecutiveSixes = 0;
        _dice.CurrentValue = 0;
        _currentPhase = TurnPhase.Rolling;

        IPlayer currentPlayer = GetCurrentPlayer();

        _logger.Information(
            "Game started with {PlayerCount} players. " +
            "First player is {CurrentPlayerName} ({CurrentPlayerColor})",
            _players.Count,
            currentPlayer.Name,
            currentPlayer.Color);

        BroadcastState();
    }

    public void RollDice()
    {
        if (!CanRoll())
        {
            _logger.Warning(
                "Dice roll ignored because current phase is {CurrentPhase}",
                _currentPhase);
            return;
        }

        IPlayer rollingPlayer = GetCurrentPlayer();
        int rolledValue = PerformRoll();

        _consecutiveSixes = rolledValue == 6
            ? _consecutiveSixes + 1
            : 0;

        _logger.Information(
            "Player {PlayerName} ({PlayerColor}) rolled {DiceValue}. " +
            "ConsecutiveSixes={ConsecutiveSixes}",
            rollingPlayer.Name,
            rollingPlayer.Color,
            rolledValue,
            _consecutiveSixes);

        if (_consecutiveSixes == 3)
        {
            _logger.Warning(
                "Turn forfeited for {PlayerName} ({PlayerColor}) after " +
                "{ConsecutiveSixes} consecutive sixes",
                rollingPlayer.Name,
                rollingPlayer.Color,
                _consecutiveSixes);

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
            _logger.Information(
                "Player {PlayerName} has no movable pawn for dice value {DiceValue}. " +
                "ExtraRollPending={ExtraRollPending}",
                rollingPlayer.Name,
                rolledValue,
                _extraRollPending);

            if (_extraRollPending)
            {
                _currentPhase = TurnPhase.Rolling;
            }
            else
            {
                NextTurn();
            }
        }
        else
        {
            List<IPawn> currentPawns = _playerPawns[GetCurrentPlayer()];

            bool allPawnsInBase = currentPawns.All(pawn => pawn.Status == PawnStatus.InBase);

            if (allPawnsInBase || movablePawns.Count == 1)
            {
                IPawn automaticPawn = movablePawns
                    .OrderBy(pawn => pawn.Id)
                    .First();

                _logger.Information(
                    "Pawn {PawnId} of {PawnColor} selected automatically. " +
                    "MovablePawnCount={MovablePawnCount}",
                    automaticPawn.Id,
                    automaticPawn.Color,
                    movablePawns.Count);

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
            else
            {
                _logger.Information(
                    "Waiting for player {PlayerName} to select one of " +
                    "{MovablePawnCount} movable pawns",
                    rollingPlayer.Name,
                    movablePawns.Count);
            }
        }

        BroadcastState();
    }

    public void SelectPawn(int pawnId)
    {
        if (_currentPhase != TurnPhase.SelectingPawn)
        {
            _logger.Warning(
                "Pawn selection ignored. PawnId={PawnId}, CurrentPhase={CurrentPhase}",
                pawnId,
                _currentPhase);
            return;
        }

        IPlayer currentPlayer = GetCurrentPlayer();

        IPawn? selectedPawn = GetMovablePawns()
            .FirstOrDefault(pawn => pawn.Id == pawnId);

        if (selectedPawn is null)
        {
            _logger.Warning(
                "Invalid pawn selection by {PlayerName}. " +
                "PawnId={PawnId}, DiceValue={DiceValue}",
                currentPlayer.Name,
                pawnId,
                _dice.CurrentValue);
            return;
        }

        _logger.Information(
            "Player {PlayerName} selected pawn {PawnId} of {PawnColor}",
            currentPlayer.Name,
            selectedPawn.Id,
            selectedPawn.Color);

        MovePawnAlongPath(
            selectedPawn,
            _dice.CurrentValue);

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
        if (_currentPhase != TurnPhase.SelectingPawn)
        {
            return Array.Empty<IPawn>();
        }

        return _playerPawns[GetCurrentPlayer()]
            .Where(pawn => IsValidMove(
                pawn,
                _dice.CurrentValue))
            .ToList()
            .AsReadOnly();
    }

    public GameState GetGameState()
    {
        Dictionary<IPlayer, IReadOnlyList<IPawn>> pawnSnapshot =
            _playerPawns.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<IPawn>)pair.Value
                    .ToList()
                    .AsReadOnly());

        return new GameState(
            _currentPhase,
            GetCurrentPlayer(),
            GetPlayers(),
            pawnSnapshot,
            _dice.CurrentValue,
            _extraRollPending,
            GetMovablePawns(),
            _players.FirstOrDefault(
                player => player.IsFinished));
    }

    public IReadOnlyList<IPlayer> GetPlayers()
    {
        return _players
            .ToList()
            .AsReadOnly();
    }

    public bool CanRoll()
    {
        return _currentPhase == TurnPhase.Rolling;
    }

    public ICell GetCell(Position position)
    {
        return BoardValidator.GetRequiredCell(_board, position);
    }

    public IReadOnlyList<ICell> GetCellsByType(CellType type)
    {
        List<ICell> result = [];

        foreach (Cell cell in _board.Cells)
        {
            if (cell.Type == type)
            {
                result.Add(cell);
            }
        }

        return result.AsReadOnly();
    }

    public Position GetStartPosition(Color color)
    {
        Position startPosition = new Position(6, 1);

        switch (color)
        {
            case Color.Red: 
                startPosition = new Position(6, 1);
                break;
            case Color.Blue: 
                startPosition = new Position(1, 8);
                break;
            case Color.Green: 
                startPosition = new Position(8, 13);
                break;
            case Color.Yellow:
                startPosition = new Position(13, 6);
                break;
        }

        return startPosition;
    }

    public IReadOnlyList<Position> GetBasePositions(Color color)
    {
        Position[] positions = [
            new Position(1, 1),
            new Position(1, 4),
            new Position(4, 1),
            new Position(4, 4)
        ];
        
        switch (color) {

            case Color.Blue:
            positions = [
                new Position(1, 10),
                new Position(1, 13),
                new Position(4, 10),
                new Position(4, 13)
            ];
            break;

            case Color.Green:
            positions = [
                new Position(10, 10),
                new Position(10, 13),
                new Position(13, 10),
                new Position(13, 13)
            ];
            break;

            case Color.Yellow:
            positions = [
                new Position(10, 1),
                new Position(10, 4),
                new Position(13, 1),
                new Position(13, 4)
            ];
            break;
        };

        return Array.AsReadOnly(positions);
    }

    public IReadOnlyList<Position> GetHomeColumnPositions(Color color)
    {

        Position[] positions = [
            new Position(7, 1),
            new Position(7, 2),
            new Position(7, 3),
            new Position(7, 4),
            new Position(7, 5)
        ];



        switch (color)
        {
            case Color.Blue:
                positions = [
                new Position(1, 7),
                new Position(2, 7),
                new Position(3, 7),
                new Position(4, 7),
                new Position(5, 7)
            ];
            break;

            case Color.Green:
            positions = [
                new Position(7, 13),
                new Position(7, 12),
                new Position(7, 11),
                new Position(7, 10),
                new Position(7, 9)
            ];
            break;

            case Color.Yellow:
            positions = [
                new Position(13, 7),
                new Position(12, 7),
                new Position(11, 7),
                new Position(10, 7),
                new Position(9, 7)
            ];
            break;
        };

        return Array.AsReadOnly(positions);
    }

    public Position GetCenterPosition()
    {
        return new Position(7, 7);
    }

    public IReadOnlyList<Position> GetFullPath(Color color)
    {
        return _pathCache.TryGetValue(color, out IReadOnlyList<Position>? path)
            ? path
            : Array.Empty<Position>();
    }
    
    private void BuildAllPaths()
    {
        foreach (Color color in Enum.GetValues<Color>())
        {
            IReadOnlyList<Position> path = BuildPathForColor(color);

            if (path.Count > 0)
            {
                _pathCache[color] = path;
            }
        }
    }

    private IReadOnlyList<Position> BuildPathForColor(Color color)
    {
        List<Position> path = GetClockwiseOuterTrack(GetStartPosition(color)).ToList();

        if (path.Count == 0)
        {
            return Array.Empty<Position>();
        }

        path.RemoveAt(path.Count - 1);

        path.AddRange(GetHomeColumnPositions(color));

        path.Add(GetCenterPosition());

        return path.AsReadOnly();
    }

    private IEnumerable<Position> GetClockwiseOuterTrack(Position startFrom)
    {
        List<Position> outerTrack =
        [
            new(6, 1), new(6, 2), new(6, 3), new(6, 4), new(6, 5),
            new(5, 6), new(4, 6), new(3, 6), new(2, 6), new(1, 6), new(0, 6),
            new(0, 7),
            new(0, 8), new(1, 8), new(2, 8), new(3, 8), new(4, 8), new(5, 8),
            new(6, 9), new(6, 10), new(6, 11), new(6, 12), new(6, 13), new(6, 14),
            new(7, 14),
            new(8, 14), new(8, 13), new(8, 12), new(8, 11), new(8, 10), new(8, 9),
            new(9, 8), new(10, 8), new(11, 8), new(12, 8), new(13, 8), new(14, 8),
            new(14, 7),
            new(14, 6), new(13, 6), new(12, 6), new(11, 6), new(10, 6), new(9, 6),
            new(8, 5), new(8, 4), new(8, 3), new(8, 2), new(8, 1), new(8, 0),
            new(7, 0),
            new(6, 0)
        ];

        int startIndex = outerTrack.IndexOf(startFrom);

        if (startIndex < 0)
        {
            yield break;
        }

        for (int offset = 0; offset < outerTrack.Count; offset++)
        {
            yield return outerTrack[(startIndex + offset) % outerTrack.Count];
        }
    }

    private void NextTurn()
    {
        IPlayer previousPlayer = GetCurrentPlayer();

        _extraRollPending = false;
        _consecutiveSixes = 0;
        _currentPhase = TurnPhase.Rolling;

        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } while (_players[_currentPlayerIndex].IsFinished);

        IPlayer currentPlayer = GetCurrentPlayer();

        _logger.Information(
            "Turn changed from {PreviousPlayerName} ({PreviousPlayerColor}) " +
            "to {CurrentPlayerName} ({CurrentPlayerColor})",
            previousPlayer.Name,
            previousPlayer.Color,
            currentPlayer.Name,
            currentPlayer.Color);
    }

    private void CheckWinCondition()
    {
        IPlayer currentPlayer = GetCurrentPlayer();

        if (_playerPawns[currentPlayer]
            .Any(pawn => pawn.Status != PawnStatus.Finished))
        {
            return;
        }

        currentPlayer.IsFinished = true;
        _currentPhase = TurnPhase.GameOver;

        _logger.Information(
            "Player {PlayerName} ({PlayerColor}) won the game",
            currentPlayer.Name,
            currentPlayer.Color);

        OnPlayerWon?.Invoke(currentPlayer);
    }

    private void HandleCapture(
        IPawn target,
        Color attackerColor,
        Position capturePosition)
    {
        RemovePawn(target);

        target.Status = PawnStatus.InBase;
        target.StepIndex = -1;

        AddPawn(target);

        _logger.Information(
            "Pawn {CapturedPawnId} of {CapturedPawnColor} was captured by " +
            "{AttackerColor} at {@CapturePosition}",
            target.Id,
            target.Color,
            attackerColor,
            capturePosition);
    }

    private Position GetCurrentPosition(IPawn pawn)
    {
        if (pawn.Status == PawnStatus.InBase)
        {
            IReadOnlyList<Position> basePositions = GetBasePositions(pawn.Color);

            return pawn.Id >= 0 &&
                   pawn.Id < basePositions.Count
                ? basePositions[pawn.Id]
                : default;
        }

        if (pawn.Status == PawnStatus.Finished)
        {
            return GetCenterPosition();
        }

        IReadOnlyList<Position> path = GetFullPath(pawn.Color);

        return pawn.StepIndex >= 0 &&
               pawn.StepIndex < path.Count
            ? path[pawn.StepIndex]
            : default;
    }

    private void CheckAndHandleCapture(ICell cell, Color attackerColor)
    {
        if (cell.Type != CellType.Normal)
        {
            return;
        }

        List<IPawn> capturedPawns =
            cell.OccupyingPawns
                .Where(pawn => pawn.Color != attackerColor)
                .ToList();

        foreach (IPawn capturedPawn in capturedPawns)
        {
            HandleCapture(
                capturedPawn,
                attackerColor,
                cell.Position);
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

        IReadOnlyList<Position> path = GetFullPath(pawn.Color);

        if (path.Count == 0)
        {
            return false;
        }

        if (pawn.Status == PawnStatus.InBase)
        {
            if (steps != 6)
            {
                return false;
            }

            Position startPosition = path[0];

            return !IsPathBlocked(
                startPosition,
                pawn.Color);
        }

        int targetIndex = pawn.StepIndex + steps;

        if (targetIndex < 0 || targetIndex >= path.Count)
        {
            return false;
        }

        for (int index = pawn.StepIndex + 1;
             index <= targetIndex;
             index++)
        {
            if (IsPathBlocked(
                    path[index],
                    pawn.Color))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPathBlocked(Position targetPosition, Color color)
    {
        ICell cell = GetCell(targetPosition);

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
        IReadOnlyList<Position> path = GetFullPath(pawn.Color);

        if (path.Count == 0)
        {
            _logger.Warning(
                "Pawn move rejected because path is unavailable. " +
                "PawnId={PawnId}, PawnColor={PawnColor}",
                pawn.Id,
                pawn.Color);
            return;
        }

        Position fromPosition = GetCurrentPosition(pawn);

        int targetIndex = pawn.Status == PawnStatus.InBase
                ? 0
                : pawn.StepIndex + steps;

        if (targetIndex < 0 || targetIndex >= path.Count)
        {
            _logger.Warning(
                "Pawn move rejected because target index is invalid. " +
                "PawnId={PawnId}, PawnColor={PawnColor}, " +
                "CurrentStepIndex={CurrentStepIndex}, Steps={Steps}, " +
                "TargetStepIndex={TargetStepIndex}",
                pawn.Id,
                pawn.Color,
                pawn.StepIndex,
                steps,
                targetIndex);
            return;
        }

        RemovePawn(pawn);

        pawn.StepIndex = targetIndex;

        int finishIndex = path.Count - 1;
        int homeColumnCount = GetHomeColumnPositions(pawn.Color).Count;
        int homeColumnStartIndex = finishIndex - homeColumnCount;

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
        ICell targetCell = GetCell(targetPosition);

        CheckAndHandleCapture(
            targetCell,
            pawn.Color);

        AddPawn(pawn);

        _logger.Information(
            "Pawn {PawnId} of {PawnColor} moved from {@FromPosition} " +
            "to {@ToPosition} by {Steps} steps. " +
            "StepIndex={StepIndex}, PawnStatus={PawnStatus}",
            pawn.Id,
            pawn.Color,
            fromPosition,
            targetPosition,
            steps,
            pawn.StepIndex,
            pawn.Status);
    }

    private int PerformRoll()
    {
        int value = _randomDiceNumberGenerator.Next(1, 7);
        _dice.CurrentValue = value;
        return value;
    }

    private void BroadcastState()
    {
        GameState state = GetGameState();

        _logger.Debug(
            "Broadcasting game state. Phase={CurrentPhase}, " +
            "CurrentPlayer={CurrentPlayerName}, DiceValue={DiceValue}, " +
            "MovablePawnCount={MovablePawnCount}",
            state.Phase,
            state.CurrentPlayer.Name,
            state.LastDiceValue,
            state.MovablePawns.Count);

        OnStateChanged?.Invoke(state);
    }

    private void AddPawn(IPawn pawn)
    {
        Position position = GetCurrentPosition(pawn);

        ICell currentCell = GetCell(position);

        List<IPawn> pawns = currentCell.OccupyingPawns.ToList();

        if (!pawns.Contains(pawn))
        {
            pawns.Add(pawn);
        }

        _board.Cells[
            position.Row,
            position.Column] = new Cell(
            currentCell.Position,
            currentCell.Type,
            currentCell.Color,
            pawns.AsReadOnly());
    }

    private void RemovePawn(IPawn pawn)
    {
        Position position = GetCurrentPosition(pawn);

        ICell currentCell = GetCell(position);

        List<IPawn> pawns = currentCell.OccupyingPawns.ToList();

        pawns.Remove(pawn);

        _board.Cells[
            position.Row,
            position.Column] = new Cell(
            currentCell.Position,
            currentCell.Type,
            currentCell.Color,
            pawns.AsReadOnly());
    }
}