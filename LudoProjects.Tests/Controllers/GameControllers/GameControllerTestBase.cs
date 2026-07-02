using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Controllers.GameControllers;

public abstract class GameControllerTestBase
{
    protected static void PlacePawn(
        GameContext context,
        IPawn pawn,
        PawnStatus status,
        int stepIndex)
    {
        RemovePawnFromBoard(context.Cells, pawn);

        pawn.Status = status;
        pawn.StepIndex = stepIndex;

        Position position = status switch
        {
            PawnStatus.InBase =>
                context.Controller.GetBasePositions(pawn.Color)[pawn.Id],
            PawnStatus.Finished => context.Controller.GetCenterPosition(),
            PawnStatus.OnBoard or PawnStatus.InHomeColumn =>
                context.Controller.GetFullPath(pawn.Color)[stepIndex],
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                null)
        };

        Cell cell = context.Cells[position.Row, position.Column];
        List<IPawn> occupyingPawns = cell.OccupyingPawns.ToList();

        if (!occupyingPawns.Contains(pawn))
        {
            occupyingPawns.Add(pawn);
        }

        context.Cells[position.Row, position.Column] = new Cell(
            cell.Position,
            cell.Type,
            cell.Color,
            occupyingPawns);
    }

    private static void RemovePawnFromBoard(Cell[,] cells, IPawn pawn)
    {
        for (int row = 0; row < cells.GetLength(0); row++)
        {
            for (int column = 0; column < cells.GetLength(1); column++)
            {
                Cell cell = cells[row, column];

                if (!cell.OccupyingPawns.Contains(pawn))
                {
                    continue;
                }

                cells[row, column] = new Cell(
                    cell.Position,
                    cell.Type,
                    cell.Color,
                    cell.OccupyingPawns
                        .Where(item => !ReferenceEquals(item, pawn))
                        .ToList());
            }
        }
    }
}
