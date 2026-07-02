using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestSupport;
using NUnit.Framework;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public sealed class GameControllerCaptureAndBlockadeTests : GameControllerTestBase
{
    [Test]
    public void MovePawn_WhenLandingOnOpponentInNormalCell_CapturesOpponent()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(2)
            .Build();
        context.Controller.StartGame();
        IPawn redPawn = context.PlayerPawns[context.Players[0]][0];
        IPawn bluePawn = context.PlayerPawns[context.Players[1]][0];
        IReadOnlyList<Position> redPath = context.Controller.GetFullPath(Color.Red);
        IReadOnlyList<Position> bluePath = context.Controller.GetFullPath(Color.Blue);
        Position target = redPath[2];
        int blueIndex = bluePath.ToList().IndexOf(target);
        PlacePawn(context, redPawn, PawnStatus.OnBoard, 0);
        PlacePawn(context, bluePawn, PawnStatus.OnBoard, blueIndex);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(redPawn.StepIndex, Is.EqualTo(2));
            Assert.That(bluePawn.Status, Is.EqualTo(PawnStatus.InBase));
            Assert.That(bluePawn.StepIndex, Is.EqualTo(-1));
            Assert.That(
                context.Controller.GetCell(target).OccupyingPawns,
                Does.Contain(redPawn));
            Assert.That(
                context.Controller.GetCell(target).OccupyingPawns,
                Does.Not.Contain(bluePawn));
        }
    }

    [Test]
    public void MovePawn_WhenLandingOnProtectedCell_DoesNotCaptureOpponent()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        IPawn redPawn = context.PlayerPawns[context.Players[0]][0];
        IPawn bluePawn = context.PlayerPawns[context.Players[1]][0];
        IReadOnlyList<Position> redPath = context.Controller.GetFullPath(Color.Red);
        IReadOnlyList<Position> bluePath = context.Controller.GetFullPath(Color.Blue);

        int targetIndex = redPath
            .Select((position, index) => new { position, index })
            .First(item =>
                item.index > 0 &&
                context.Controller.GetCell(item.position).Type == CellType.Protected &&
                bluePath.Contains(item.position))
            .index;

        Position target = redPath[targetIndex];
        int blueIndex = bluePath.ToList().IndexOf(target);
        PlacePawn(context, redPawn, PawnStatus.OnBoard, targetIndex - 1);
        PlacePawn(context, bluePawn, PawnStatus.OnBoard, blueIndex);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(redPawn.StepIndex, Is.EqualTo(targetIndex));
            Assert.That(bluePawn.Status, Is.EqualTo(PawnStatus.OnBoard));
            Assert.That(
                context.Controller.GetCell(target).OccupyingPawns,
                Does.Contain(redPawn));
            Assert.That(
                context.Controller.GetCell(target).OccupyingPawns,
                Does.Contain(bluePawn));
        }
    }

    [Test]
    public void MovePawn_WhenOpponentBlockadeIsOnPath_DoesNotMovePawn()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(3)
            .Build();
        context.Controller.StartGame();
        IPawn redPawn = context.PlayerPawns[context.Players[0]][0];
        List<IPawn> bluePawns = context.PlayerPawns[context.Players[1]];
        IReadOnlyList<Position> redPath = context.Controller.GetFullPath(Color.Red);
        IReadOnlyList<Position> bluePath = context.Controller.GetFullPath(Color.Blue);
        Position blockadePosition = redPath[2];
        int blueIndex = bluePath.ToList().IndexOf(blockadePosition);
        PlacePawn(context, redPawn, PawnStatus.OnBoard, 0);
        PlacePawn(context, bluePawns[0], PawnStatus.OnBoard, blueIndex);
        PlacePawn(context, bluePawns[1], PawnStatus.OnBoard, blueIndex);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(redPawn.StepIndex, Is.Zero);
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[1]));
        }
    }

    [Test]
    public void MovePawn_WhenDifferentOpponentColorsShareTarget_CapturesBoth()
    {
        GameContext context = new GameContextBuilder()
            .WithPlayers(Color.Red, Color.Blue, Color.Green)
            .WithDiceValues(2)
            .Build();
        context.Controller.StartGame();
        IPawn redPawn = context.PlayerPawns[context.Players[0]][0];
        IPawn bluePawn = context.PlayerPawns[context.Players[1]][0];
        IPawn greenPawn = context.PlayerPawns[context.Players[2]][0];
        IReadOnlyList<Position> redPath = context.Controller.GetFullPath(Color.Red);
        Position target = redPath[2];
        int blueIndex = context.Controller.GetFullPath(Color.Blue).ToList().IndexOf(target);
        int greenIndex = context.Controller.GetFullPath(Color.Green).ToList().IndexOf(target);
        PlacePawn(context, redPawn, PawnStatus.OnBoard, 0);
        PlacePawn(context, bluePawn, PawnStatus.OnBoard, blueIndex);
        PlacePawn(context, greenPawn, PawnStatus.OnBoard, greenIndex);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bluePawn.Status, Is.EqualTo(PawnStatus.InBase));
            Assert.That(greenPawn.Status, Is.EqualTo(PawnStatus.InBase));
            Assert.That(
                context.Controller.GetCell(target).OccupyingPawns,
                Is.EquivalentTo(new[] { redPawn }));
        }
    }
}
