using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public class GameControllerMovementTests : GameControllerTestBase
{
    [Test]
    public void MovePawn_WhenEnteringHomeColumn_UpdatesPawnStatus()
    {
        GameContext context = Builder
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        IPawn pawn = context.PlayerPawns[context.Players[0]][0];
        PlacePawn(context, pawn, PawnStatus.OnBoard, 50);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawn.StepIndex, Is.EqualTo(51));
            Assert.That(pawn.Status, Is.EqualTo(PawnStatus.InHomeColumn));
        }
    }

    [Test]
    public void MovePawn_WhenReachingCenter_MarksPawnAsFinished()
    {
        GameContext context = Builder
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        IPawn pawn = context.PlayerPawns[context.Players[0]][0];
        int finishIndex = context.Controller.GetFullPath(Color.Red).Count - 1;
        PlacePawn(context, pawn, PawnStatus.InHomeColumn, finishIndex - 1);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawn.StepIndex, Is.EqualTo(finishIndex));
            Assert.That(pawn.Status, Is.EqualTo(PawnStatus.Finished));
            Assert.That(context.Players[0].IsFinished, Is.False);
        }
    }

    [Test]
    public void MovePawn_WhenDiceWouldOvershootCenter_DoesNotMovePawn()
    {
        GameContext context = Builder
            .WithDiceValues(2)
            .Build();
        context.Controller.StartGame();
        IPawn pawn = context.PlayerPawns[context.Players[0]][0];
        int finishIndex = context.Controller.GetFullPath(Color.Red).Count - 1;
        PlacePawn(context, pawn, PawnStatus.InHomeColumn, finishIndex - 1);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawn.StepIndex, Is.EqualTo(finishIndex - 1));
            Assert.That(pawn.Status, Is.EqualTo(PawnStatus.InHomeColumn));
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[1]));
        }
    }

    [Test]
    public void MovePawn_WhenSingleOpponentIsOnIntermediateCell_CanPassIt()
    {
        GameContext context = Builder
            .WithDiceValues(3)
            .Build();
        context.Controller.StartGame();
        IPawn redPawn = context.PlayerPawns[context.Players[0]][0];
        IPawn bluePawn = context.PlayerPawns[context.Players[1]][0];
        IReadOnlyList<Position> redPath = context.Controller.GetFullPath(Color.Red);
        IReadOnlyList<Position> bluePath = context.Controller.GetFullPath(Color.Blue);
        Position intermediatePosition = redPath[2];
        int blueIndex = bluePath.ToList().IndexOf(intermediatePosition);
        PlacePawn(context, redPawn, PawnStatus.OnBoard, 0);
        PlacePawn(context, bluePawn, PawnStatus.OnBoard, blueIndex);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(redPawn.StepIndex, Is.EqualTo(3));
            Assert.That(bluePawn.Status, Is.EqualTo(PawnStatus.OnBoard));
            Assert.That(bluePawn.StepIndex, Is.EqualTo(blueIndex));
        }
    }

    [Test]
    public void MovePawn_WhenFriendlyPawnsShareIntermediateCell_IsNotBlocked()
    {
        GameContext context = Builder
            .WithDiceValues(3)
            .Build();
        context.Controller.StartGame();
        List<IPawn> redPawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, redPawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, redPawns[1], PawnStatus.OnBoard, 2);
        PlacePawn(context, redPawns[2], PawnStatus.OnBoard, 2);
        context.Controller.RollDice();

        context.Controller.SelectPawn(redPawns[0].Id);

        Assert.That(redPawns[0].StepIndex, Is.EqualTo(3));
    }

    [Test]
    public void GetMovablePawns_DoesNotIncludeFinishedOrOvershootingPawns()
    {
        GameContext context = Builder
            .WithDiceValues(2)
            .Build();
        context.Controller.StartGame();
        List<IPawn> redPawns = context.PlayerPawns[context.Players[0]];
        int finishIndex = context.Controller.GetFullPath(Color.Red).Count - 1;
        PlacePawn(context, redPawns[0], PawnStatus.Finished, finishIndex);
        PlacePawn(context, redPawns[1], PawnStatus.InHomeColumn, finishIndex - 1);
        PlacePawn(context, redPawns[2], PawnStatus.OnBoard, 0);
        PlacePawn(context, redPawns[3], PawnStatus.OnBoard, 4);

        context.Controller.RollDice();

        IReadOnlyList<IPawn> movablePawns = context.Controller.GetMovablePawns();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(movablePawns, Does.Not.Contain(redPawns[0]));
            Assert.That(movablePawns, Does.Not.Contain(redPawns[1]));
            Assert.That(movablePawns, Does.Contain(redPawns[2]));
            Assert.That(movablePawns, Does.Contain(redPawns[3]));
        }
    }
}
