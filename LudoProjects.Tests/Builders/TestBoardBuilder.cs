using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Tests.Builders;

internal class TestBoardBuilder
{
    private readonly int _rows;
    private readonly int _columns;
    private Dictionary<IPlayer, List<IPawn>> _playerPawns = new();
    private bool _isReadyBoard = true;

    public TestBoardBuilder(int rows = 15, int columns = 15)
    {
        _rows = rows;
        _columns = columns;
    }

    public TestBoardBuilder WithPlayerPawns(
        Dictionary<IPlayer, List<IPawn>> playerPawns)
    {
        _playerPawns = playerPawns;
        return this;
    }

    public TestBoardBuilder AsReadyBoard()
    {
        _isReadyBoard = true;
        return this;
    }

    public TestBoardBuilder AsIncompleteBoard()
    {
        _isReadyBoard = false;
        return this;
    }

    public Cell[,] Build()
    {
        Cell[,] cells = CreateDefaultCells();

        if (!_isReadyBoard || _rows != 15 || _columns != 15)
        {
            return cells;
        }

        InitializeCenterCells(cells);
        InitializeStartCells(cells);
        InitializeProtectedCells(cells);
        InitializeHomeColumnCells(cells);
        PlacePawnsInBase(cells);

        return cells;
    }

    private Cell[,] CreateDefaultCells()
    {
        Cell[,] cells = new Cell[_rows, _columns];

        for (int row = 0; row < _rows; row++)
        {
            for (int column = 0; column < _columns; column++)
            {
                Color? baseColor = GetBaseColor(row, column);
                CellType type = baseColor.HasValue
                    ? CellType.Base
                    : CellType.Normal;

                cells[row, column] = CreateCell(
                    new Position(row, column),
                    type,
                    baseColor);
            }
        }

        return cells;
    }

    private static Color? GetBaseColor(int row, int column)
    {
        if (row <= 5 && column <= 5)
        {
            return Color.Red;
        }

        if (row <= 5 && column >= 9)
        {
            return Color.Blue;
        }

        if (row >= 9 && column >= 9)
        {
            return Color.Green;
        }

        if (row >= 9 && column <= 5)
        {
            return Color.Yellow;
        }

        return null;
    }

    private static void InitializeCenterCells(Cell[,] cells)
    {
        for (int row = 6; row <= 8; row++)
        {
            for (int column = 6; column <= 8; column++)
            {
                Position position = new(row, column);
                cells[row, column] = CreateCell(
                    position,
                    CellType.Center,
                    null);
            }
        }
    }

    private static void InitializeStartCells(Cell[,] cells)
    {
        foreach (KeyValuePair<Color, Position> pair in StartPositions)
        {
            Position position = pair.Value;
            cells[position.Row, position.Column] = CreateCell(
                position,
                CellType.Start,
                pair.Key);
        }
    }

    private static void InitializeProtectedCells(Cell[,] cells)
    {
        foreach (Position position in ProtectedPositions)
        {
            cells[position.Row, position.Column] = CreateCell(
                position,
                CellType.Protected,
                null);
        }
    }

    private static void InitializeHomeColumnCells(Cell[,] cells)
    {
        foreach (KeyValuePair<Color, Position[]> pair in HomeColumnPositions)
        {
            foreach (Position position in pair.Value)
            {
                cells[position.Row, position.Column] = CreateCell(
                    position,
                    CellType.HomeColumn,
                    pair.Key);
            }
        }
    }

    private void PlacePawnsInBase(Cell[,] cells)
    {
        foreach (KeyValuePair<IPlayer, List<IPawn>> pair in _playerPawns)
        {
            IPlayer player = pair.Key;
            List<IPawn> pawns = pair.Value;
            Position[] basePositions = BasePositions[player.Color];

            for (int index = 0; index < pawns.Count && index < basePositions.Length; index++)
            {
                IPawn pawn = pawns[index];
                Position position = basePositions[index];
                Cell currentCell = cells[position.Row, position.Column];
                List<IPawn> occupants = currentCell.OccupyingPawns.ToList();
                occupants.Add(pawn);

                cells[position.Row, position.Column] = new Cell(
                    currentCell.Position,
                    currentCell.Type,
                    currentCell.Color,
                    occupants);
            }
        }
    }

    private static Cell CreateCell(
        Position position,
        CellType type,
        Color? color)
    {
        return new Cell(
            position,
            type,
            color,
            Array.Empty<IPawn>());
    }

    private static readonly Dictionary<Color, Position> StartPositions = new()
    {
        [Color.Red] = new Position(6, 1),
        [Color.Blue] = new Position(1, 8),
        [Color.Green] = new Position(8, 13),
        [Color.Yellow] = new Position(13, 6)
    };

    private static readonly Position[] ProtectedPositions =
    [
        new Position(2, 6),
        new Position(6, 12),
        new Position(12, 8),
        new Position(8, 2)
    ];

    private static readonly Dictionary<Color, Position[]> HomeColumnPositions = new()
    {
        [Color.Red] =
        [
            new Position(7, 1), new Position(7, 2), new Position(7, 3),
            new Position(7, 4), new Position(7, 5)
        ],
        [Color.Blue] =
        [
            new Position(1, 7), new Position(2, 7), new Position(3, 7),
            new Position(4, 7), new Position(5, 7)
        ],
        [Color.Green] =
        [
            new Position(7, 13), new Position(7, 12), new Position(7, 11),
            new Position(7, 10), new Position(7, 9)
        ],
        [Color.Yellow] =
        [
            new Position(13, 7), new Position(12, 7), new Position(11, 7),
            new Position(10, 7), new Position(9, 7)
        ]
    };

    private static readonly Dictionary<Color, Position[]> BasePositions = new()
    {
        [Color.Red] =
        [
            new Position(1, 1), new Position(1, 4),
            new Position(4, 1), new Position(4, 4)
        ],
        [Color.Blue] =
        [
            new Position(1, 10), new Position(1, 13),
            new Position(4, 10), new Position(4, 13)
        ],
        [Color.Green] =
        [
            new Position(10, 10), new Position(10, 13),
            new Position(13, 10), new Position(13, 13)
        ],
        [Color.Yellow] =
        [
            new Position(10, 1), new Position(10, 4),
            new Position(13, 1), new Position(13, 4)
        ]
    };
}
