using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public class Board : IBoard
{
    private readonly Cell[,] _cells;
    private readonly Dictionary<Color, IReadOnlyList<Position>> _pathCache;

    public Board()
    {
        // initialize row & column ludo board
        _cells = new Cell[15, 15];
        
        static Color? GetBaseColor(int row, int column)
        {
            return row switch
            {
                <= 5 when column <= 5 => Color.Red,
                <= 5 when column >= 9 => Color.Blue,
                >= 9 when column >= 9 => Color.Green,
                >= 9 when column <= 5 => Color.Yellow,
                _ => null
            };
        }
        
        // Creates cells on the board and determines each cell type on the board,
        // whether it is a basic cell or a normal cell.
        for (var row = 0; row < _cells.GetLength(0); row++)
        {
            for (var column = 0; column < _cells.GetLength(1); column++)
            {
                var baseColor = GetBaseColor(row, column);
                var type = baseColor.HasValue ? CellType.Base : CellType.Normal;
                _cells[row, column] = new Cell(new Position(row, column), type, baseColor);
            }
        }

        // Make a map of the central area which will later be used as the "Center" or finish center.
        for (var row = 6; row <= 8; row++)
        {
            for (var column = 6; column <= 8; column++)
            {
                _cells[row, column] = new Cell(new Position(row, column), CellType.Center, null);
            }
        }
        
        // Mark all outer lines (which are crosses) as Cell Type Normal first.
        foreach (var position in GetClockwiseOuterTrack(new Position(6, 1)))
        {
            _cells[position.Row, position.Column] = new Cell(position, CellType.Normal, null);
        }
        
        // Marks and determines the color for cells whose CellType is Start.
        foreach (var color in Enum.GetValues<Color>())
        {
            var start = GetStartPosition(color);
            _cells[start.Row, start.Column] = new Cell(start, CellType.Start, color);
        }
        
    }
    
    public ICell GetCell(Position position)
    {
        if (position.Row < 0 || position.Row >= 15 || position.Column < 0 || position.Column >= 15)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "The position is outside the board.");
        }

        return _cells[position.Row, position.Column];
    }

    public IReadOnlyList<ICell> GetCellsByType(CellType type)
    {
        List<ICell> result = new();
        foreach (var cell in _cells)
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
        var start = color switch
        {
            Color.Red => new Position(6, 1),
            Color.Blue => new Position(1, 8),
            Color.Green => new Position(8, 13),
            Color.Yellow => new Position(13, 6),
            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };

        return start;
    }

    public IReadOnlyList<Position> GetBasePositions(Color color)
    {
        Position[] positions = color switch
        {
            Color.Red =>
            [
                new Position(1, 1),
                new Position(1, 4),
                new Position(4, 1),
                new Position(4, 4)
            ],
            
            Color.Blue => 
            [
                new Position(1, 10),
                new Position(1, 13),
                new Position(4, 10),
                new Position(4, 13)
            ],
            
            Color.Green =>
            [
                new Position(10, 10),
                new Position(10, 13),
                new Position(13, 10),
                new Position(13, 13)
            ],
            
            Color.Yellow =>
            [
                new Position(10, 1),
                new Position(10, 4),
                new Position(13, 1),
                new Position(13, 4)
            ],
            
            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };

        return Array.AsReadOnly(positions);
    }

    public IReadOnlyList<Position> GetHomeColumnPositions(Color color)
    {
        Position[] positions = color switch
        {
            Color.Red =>
            [
                new Position(7, 1),
                new Position(7, 2),
                new Position(7, 3),
                new Position(7, 4),
                new Position(7, 5)
            ],
            
            Color.Blue =>
            [
                new Position(1, 7),
                new Position(2, 7),
                new Position(3, 7),
                new Position(4, 7),
                new Position(5, 7)
            ],
            
            Color.Green => 
            [
                new Position(7, 13),
                new Position(7, 12),
                new Position(7, 11),
                new Position(7, 10),
                new Position(7, 9)
            ],
            
            
            Color.Yellow =>
            [
                new Position(13, 7), 
                new Position(12, 7),
                new Position(11, 7),
                new Position(10, 7),
                new Position(9, 7)
            ],
                
            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };
        
        return Array.AsReadOnly(positions);
    }

    public Position GetCenterPosition()
    {
        return new Position(7, 7);
    }
    
    // This method is used to take all the paths that the pawn must take based on its color.
    public IReadOnlyList<Position> GetFullPath(Color color)
    {
        if (!_pathCache.TryGetValue(color, out var path))
        {
            throw new InvalidOperationException(
                    $"The path for {color} has not been created yet."
                );
        }

        return path;
    }

    private void BuildAllPaths()
    {
        foreach (var color in Enum.GetValues<Color>())
        {
            _pathCache[color] = BuildPathForColor(color);
        }
    }

    private IReadOnlyList<Position> BuildPathForColor(Color color)
    {
        var path = GetClockwiseOuterTrack(GetStartPosition(color)).ToList();
        
        path.AddRange(GetHomeColumnPositions(color));
        path.Add(GetCenterPosition());

        return path.AsReadOnly();
    }
    

    private IEnumerable<Position> GetClockwiseOuterTrack(Position startFrom)
    {
        var outerTrack = new List<Position>
        {
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
        };

        var startIndex = outerTrack.IndexOf(startFrom);

        if (startIndex < 0)
        {
            throw new ArgumentException("The starting position is not on the outside lane.", nameof(startFrom));
        }

        for (var offset = 0; offset < outerTrack.Count; offset++)
        {
            yield return outerTrack[(startIndex + offset) % outerTrack.Count];
        }
    }
}