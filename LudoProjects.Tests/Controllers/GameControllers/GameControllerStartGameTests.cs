using LudoProjects.Enums;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestSupport;
using NUnit.Framework;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public sealed class GameControllerStartGameTests : GameControllerTestBase
{
    [Test]
    public void StartGame_WithReadyBoard_StartsWithFirstPlayerAndBroadcastsState()
    {
        GameContext context = new GameContextBuilder().Build();
        GameState? receivedState = null;
        context.Controller.OnStateChanged += state => receivedState = state;

        context.Controller.StartGame();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Controller.CanRoll(), Is.True);
            Assert.That(
                context.Controller.GetCurrentPlayer(),
                Is.SameAs(context.Players[0]));
            Assert.That(receivedState, Is.Not.Null);
            Assert.That(receivedState!.Phase, Is.EqualTo(TurnPhase.Rolling));
            Assert.That(receivedState.LastDiceValue, Is.Zero);
            Assert.That(receivedState.ExtraRollPending, Is.False);
        }
    }

    [Test]
    public void StartGame_WithIncompleteBoard_RemainsWaitingToStart()
    {
        GameContext context = new GameContextBuilder()
            .WithIncompleteBoard()
            .Build();

        context.Controller.StartGame();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Controller.CanRoll(), Is.False);
            Assert.That(
                context.Controller.GetGameState().Phase,
                Is.EqualTo(TurnPhase.WaitingToStart));
        }
    }

    [Test]
    public void StartGame_WhenAlreadyStarted_DoesNotResetGameState()
    {
        GameContext context = new GameContextBuilder()
            .WithDiceValues(6)
            .Build();
        context.Controller.StartGame();
        context.Controller.RollDice();
        var stateBeforeSecondStart = context.Controller.GetGameState();

        context.Controller.StartGame();

        GameState stateAfterSecondStart = context.Controller.GetGameState();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                stateAfterSecondStart.CurrentPlayer,
                Is.SameAs(stateBeforeSecondStart.CurrentPlayer));
            Assert.That(
                stateAfterSecondStart.LastDiceValue,
                Is.EqualTo(stateBeforeSecondStart.LastDiceValue));
            Assert.That(
                stateAfterSecondStart.Phase,
                Is.EqualTo(stateBeforeSecondStart.Phase));
        }
    }

    [Test]
    public void StartGame_BuildsMovementPathsForEveryColor()
    {
        GameContext context = new GameContextBuilder().Build();

        context.Controller.StartGame();

        foreach (Color color in Enum.GetValues<Color>())
        {
            IReadOnlyList<Position> path = context.Controller.GetFullPath(color);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(path, Has.Count.EqualTo(57));
                Assert.That(path[0], Is.EqualTo(context.Controller.GetStartPosition(color)));
                Assert.That(path[^1], Is.EqualTo(context.Controller.GetCenterPosition()));
            }
        }
    }
}
