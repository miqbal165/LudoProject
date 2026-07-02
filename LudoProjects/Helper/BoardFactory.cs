using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Validators;

namespace LudoProjects.Helper;

public static class BoardFactory
{
    public static void InitializeCells(GameController controller, Cell[,] cells, Dictionary<IPlayer, List<IPawn>> playerPawns)
    {
        BoardFactoryValidator.Validate(controller, cells, playerPawns);

        InitializeDefaultCells(cells);
        InitializeCenterCells(cells);
        InitializeStartCells(controller, cells);
        InitializeProtectedCells(cells);
        InitializeHomeColumnCells(controller, cells);
        InitializeCenterFinishCell(controller, cells);
        PlacePawnsInBase(controller, cells, playerPawns);
    }

    private static void InitializeDefaultCells(Cell[,] cells)
    {
        for (int row = 0; row < cells.GetLength(0); row++)
        {
            for (int column = 0; column < cells.GetLength(1); column++)
            {
                Color? baseColor = null;

                if (row <= 5 && column <= 5)
                {
                    baseColor = Color.Red;
                }
                else if (row <= 5 && column >= 9)
                {
                    baseColor = Color.Blue;
                }
                else if (row >= 9 && column >= 9)
                {
                    baseColor = Color.Green;
                }
                else if (row >= 9 && column <= 5)
                {
                    baseColor = Color.Yellow;
                }

                Position position = new(row, column);

                CellType cellType = baseColor.HasValue
                    ? CellType.Base
                    : CellType.Normal;

                cells[row, column] = new Cell(
                    position,
                    cellType,
                    baseColor,
                    Array.Empty<IPawn>());
            }
        }
    }

    private static void InitializeCenterCells(Cell[,] cells)
    {
        for (int row = 6; row <= 8; row++)
        {
            for (int column = 6; column <= 8; column++)
            {
                Position position = new(row, column);

                cells[row, column] = new Cell(
                    position,
                    CellType.Center,
                    null,
                    Array.Empty<IPawn>());
            }
        }
    }

    private static void InitializeStartCells(GameController controller, Cell[,] cells)
    {
        foreach (Color color in Enum.GetValues<Color>())
        {
            Position startPosition = controller.GetStartPosition(color);

            cells[startPosition.Row, startPosition.Column] = new Cell(
                    startPosition,
                    CellType.Start,
                    color,
                    Array.Empty<IPawn>());
        }
    }

    private static void InitializeProtectedCells(Cell[,] cells)
    {
        Position[] protectedPositions = [
            new Position(2, 6),
            new Position(6, 12),
            new Position(12, 8),
            new Position(8, 2)
        ];

        foreach (Position position in protectedPositions)
        {
            cells[position.Row, position.Column] = new Cell(
                position,
                CellType.Protected,
                null,
                Array.Empty<IPawn>());
        }
    }

    private static void InitializeHomeColumnCells(GameController controller, Cell[,] cells)
    {
        foreach (Color color in Enum.GetValues<Color>())
        {
            IReadOnlyList<Position> homeColumnPositions = controller.GetHomeColumnPositions(color);

            foreach (Position position in homeColumnPositions)
            {
                cells[position.Row, position.Column] = new Cell(
                    position,
                    CellType.HomeColumn,
                    color,
                    Array.Empty<IPawn>());
            }
        }
    }

    private static void InitializeCenterFinishCell(GameController controller, Cell[,] cells)
    {
        Position centerPosition = controller.GetCenterPosition();

        cells[centerPosition.Row, centerPosition.Column] = new Cell(
                centerPosition,
                CellType.Center,
                null,
                Array.Empty<IPawn>());
    }

    private static void PlacePawnsInBase(GameController controller, Cell[,] cells, Dictionary<IPlayer, List<IPawn>> playerPawns)
    {
        foreach (KeyValuePair<IPlayer, List<IPawn>> pair in playerPawns)
        {
            IPlayer player = pair.Key;
            List<IPawn> pawns = pair.Value;

            IReadOnlyList<Position> basePositions = controller.GetBasePositions(player.Color);

            if (pawns.Count != basePositions.Count)
            {
                throw new ArgumentException(
                    $"Player {player.Name} must have exactly " +
                    $"{basePositions.Count} pawns.",
                    nameof(playerPawns));
            }

            for (int index = 0; index < pawns.Count; index++)
            {
                IPawn pawn = pawns[index];
                Position basePosition = basePositions[index];

                Cell baseCell = cells[basePosition.Row, basePosition.Column];

                List<IPawn> occupyingPawns = baseCell.OccupyingPawns.ToList();

                occupyingPawns.Add(pawn);

                cells[basePosition.Row, basePosition.Column] = new Cell(
                        baseCell.Position,
                        baseCell.Type,
                        baseCell.Color,
                        occupyingPawns.AsReadOnly());
            }
        }
    }
}