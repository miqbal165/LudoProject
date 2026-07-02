using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestSupport;
using NUnit.Framework;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public sealed class GameControllerPawnSelectionTests : GameControllerTestBase
{
    [Test]
    public void RollDice_WithMultipleMovablePawns_WaitsForPlayerSelection()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        List<IPawn> pawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, pawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, pawns[1], PawnStatus.OnBoard, 2);

        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.SelectingPawn));
            Assert.That(state.MovablePawns, Has.Count.EqualTo(2));
            Assert.That(state.MovablePawns, Does.Contain(pawns[0]));
            Assert.That(state.MovablePawns, Does.Contain(pawns[1]));
        }
    }

    [Test]
    public void SelectPawn_WithValidId_MovesSelectedPawnAndChangesPlayer()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        List<IPawn> pawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, pawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, pawns[1], PawnStatus.OnBoard, 2);
        context.Controller.RollDice();

        context.Controller.SelectPawn(pawns[1].Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawns[0].StepIndex, Is.Zero);
            Assert.That(pawns[1].StepIndex, Is.EqualTo(3));
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[1]));
        }
    }

    [Test]
    public void SelectPawn_WithInvalidId_DoesNotMoveAnyPawn()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        List<IPawn> pawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, pawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, pawns[1], PawnStatus.OnBoard, 2);
        context.Controller.RollDice();

        context.Controller.SelectPawn(99);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawns[0].StepIndex, Is.Zero);
            Assert.That(pawns[1].StepIndex, Is.EqualTo(2));
            Assert.That(
                context.Controller.GetGameState().Phase,
                Is.EqualTo(TurnPhase.SelectingPawn));
        }
    }

    [Test]
    public void SelectPawn_OutsideSelectionPhase_IsIgnored()
    {
        GameContext context = new GameContextBuilder().Build();
        IPawn pawn = context.PlayerPawns[context.Players[0]][0];

        context.Controller.SelectPawn(pawn.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawn.Status, Is.EqualTo(PawnStatus.InBase));
            Assert.That(pawn.StepIndex, Is.EqualTo(-1));
            Assert.That(
                context.Controller.GetGameState().Phase,
                Is.EqualTo(TurnPhase.WaitingToStart));
        }
    }

    [Test]
    public void SelectPawn_AfterRollingSix_GrantsExtraRollToSamePlayer()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(6)
            .Build();
        context.Controller.StartGame();
        List<IPawn> pawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, pawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, pawns[1], PawnStatus.OnBoard, 2);
        context.Controller.RollDice();

        context.Controller.SelectPawn(pawns[1].Id);

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(pawns[1].StepIndex, Is.EqualTo(8));
            Assert.That(state.CurrentPlayer, Is.SameAs(context.Players[0]));
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.Rolling));
            Assert.That(state.ExtraRollPending, Is.True);
        }
    }
}
