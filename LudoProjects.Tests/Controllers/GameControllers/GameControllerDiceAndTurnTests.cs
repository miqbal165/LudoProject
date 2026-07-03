using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public class GameControllerDiceAndTurnTests : GameControllerTestBase
{
    [Test]
    public void RollDice_BeforeGameStarts_IsIgnored()
    {
        GameContext context = Builder
            .WithDiceValues(6)
            .Build();

        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.WaitingToStart));
            Assert.That(state.LastDiceValue, Is.Zero);
        }
    }

    [Test]
    public void RollDice_WithNonSixAndNoMovablePawn_ChangesPlayer()
    {
        GameContext context = Builder
            .WithDiceValues(3)
            .Build();
        context.Controller.StartGame();

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[1]));
            Assert.That(context.Controller.CanRoll(), Is.True);
        }
    }

    [Test]
    public void RollDice_WithSixAndAllPawnsInBase_MovesFirstPawnAndGrantsExtraRoll()
    {
        GameContext context = Builder
            .WithDiceValues(6)
            .Build();
        IPawn firstPawn = context.PlayerPawns[context.Players[0]][0];
        context.Controller.StartGame();

        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstPawn.Status, Is.EqualTo(PawnStatus.OnBoard));
            Assert.That(firstPawn.StepIndex, Is.Zero);
            Assert.That(state.CurrentPlayer, Is.SameAs(context.Players[0]));
            Assert.That(state.ExtraRollPending, Is.True);
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.Rolling));
        }
    }

    [Test]
    public void RollDice_WithSixButNoMovablePawn_KeepsSamePlayerRolling()
    {
        GameContext context = Builder
            .WithDiceValues(6)
            .Build();
        context.Controller.StartGame();

        foreach (IPawn pawn in context.PlayerPawns[context.Players[0]])
        {
            PlacePawn(
                context,
                pawn,
                PawnStatus.Finished,
                context.Controller.GetFullPath(pawn.Color).Count - 1);
        }

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[0]));
            Assert.That(context.Controller.CanRoll(), Is.True);
            Assert.That(
                context.Controller.GetGameState().ExtraRollPending,
                Is.True);
        }
    }

    [Test]
    public void RollDice_WithOneMovablePawn_MovesItAutomaticallyAndChangesPlayer()
    {
        GameContext context = Builder
            .WithDiceValues(2)
            .Build();
        context.Controller.StartGame();
        IPawn pawn = context.PlayerPawns[context.Players[0]][0];
        PlacePawn(context, pawn, PawnStatus.OnBoard, 0);

        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawn.StepIndex, Is.EqualTo(2));
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[1]));
        }
    }

    [Test]
    public void RollDice_ThreeConsecutiveSixes_ForfeitsTurn()
    {
        GameContext context = Builder
            .WithDiceValues(6, 6, 6)
            .Build();
        context.Controller.StartGame();
        IPawn firstPawn = context.PlayerPawns[context.Players[0]][0];

        context.Controller.RollDice();
        context.Controller.RollDice();
        context.Controller.SelectPawn(firstPawn.Id);
        context.Controller.RollDice();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[1]));
            Assert.That(context.Controller.CanRoll(), Is.True);
        }
    }

    [Test]
    public void RollDice_WhileWaitingForPawnSelection_IsIgnored()
    {
        GameContext context = Builder
            .WithDiceValues(1, 5)
            .Build();
        context.Controller.StartGame();
        List<IPawn> redPawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, redPawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, redPawns[1], PawnStatus.OnBoard, 2);

        context.Controller.RollDice();
        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.SelectingPawn));
            Assert.That(state.LastDiceValue, Is.EqualTo(1));
            Assert.That(state.CurrentPlayer, Is.SameAs(context.Players[0]));
        }
    }

    [Test]
    public void NextTurn_SkipsPlayerWhoHasFinished()
    {
        GameContext context = Builder
            .WithPlayers(Color.Red, Color.Blue, Color.Green)
            .WithDiceValues(2)
            .Build();
        context.Players[1].IsFinished = true;
        context.Controller.StartGame();

        context.Controller.RollDice();

        Assert.That(
            context.Controller.GetCurrentPlayer(),
            Is.SameAs(context.Players[2]));
    }
}
