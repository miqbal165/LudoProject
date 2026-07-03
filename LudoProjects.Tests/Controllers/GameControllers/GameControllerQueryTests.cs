using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public class GameControllerQueryTests : GameControllerTestBase
{
    [TestCase(Color.Red, 6, 1)]
    [TestCase(Color.Blue, 1, 8)]
    [TestCase(Color.Green, 8, 13)]
    [TestCase(Color.Yellow, 13, 6)]
    public void ColorQueries_ReturnExpectedBoardData(
        Color color,
        int expectedStartRow,
        int expectedStartColumn)
    {
        GameContext context = Builder.Build();
        context.Controller.StartGame();

        IReadOnlyList<Position> path = context.Controller.GetFullPath(color);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                context.Controller.GetStartPosition(color),
                Is.EqualTo(new Position(expectedStartRow, expectedStartColumn)));
            Assert.That(context.Controller.GetBasePositions(color), Has.Count.EqualTo(4));
            Assert.That(
                context.Controller.GetHomeColumnPositions(color),
                Has.Count.EqualTo(5));
            Assert.That(path, Has.Count.EqualTo(57));
            Assert.That(path[^1], Is.EqualTo(context.Controller.GetCenterPosition()));
        }
    }

    [Test]
    public void GetCellsByType_ReturnsExpectedBoardCellCounts()
    {
        GameContext context = Builder.Build();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Controller.GetCellsByType(CellType.Start), Has.Count.EqualTo(4));
            Assert.That(
                context.Controller.GetCellsByType(CellType.Protected),
                Has.Count.EqualTo(4));
            Assert.That(
                context.Controller.GetCellsByType(CellType.HomeColumn),
                Has.Count.EqualTo(20));
            Assert.That(context.Controller.GetCellsByType(CellType.Center), Has.Count.EqualTo(9));
        }
    }

    [Test]
    public void GetCell_WithValidPosition_ReturnsRequestedCell()
    {
        GameContext context = Builder.Build();
        Position position = new(6, 1);

        ICell cell = context.Controller.GetCell(position);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cell.Position, Is.EqualTo(position));
            Assert.That(cell.Type, Is.EqualTo(CellType.Start));
            Assert.That(cell.Color, Is.EqualTo(Color.Red));
        }
    }

    [Test]
    public void GetCell_WithPositionOutsideBoard_ThrowsArgumentOutOfRangeException()
    {
        GameContext context = Builder.Build();
        Action getCell = () => context.Controller.GetCell(new Position(-1, 0));

        Assert.Throws<ArgumentOutOfRangeException>(getCell);
    }

    [Test]
    public void GetPlayers_ReturnsAllPlayersInTurnOrder()
    {
        GameContext context = Builder
            .WithPlayers(Color.Red, Color.Blue, Color.Green, Color.Yellow)
            .Build();

        IReadOnlyList<IPlayer> players = context.Controller.GetPlayers();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(players, Has.Count.EqualTo(4));
            Assert.That(players[0].Color, Is.EqualTo(Color.Red));
            Assert.That(players[1].Color, Is.EqualTo(Color.Blue));
            Assert.That(players[2].Color, Is.EqualTo(Color.Green));
            Assert.That(players[3].Color, Is.EqualTo(Color.Yellow));
        }
    }
}
