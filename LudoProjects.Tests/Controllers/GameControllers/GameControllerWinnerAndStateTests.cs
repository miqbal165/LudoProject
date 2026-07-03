using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public class GameControllerWinnerAndStateTests : GameControllerTestBase
{
    [Test]
    public void MoveLastPawnToCenter_SetsWinnerAndRaisesEvent()
    {
        GameContext context = Builder
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        List<IPawn> redPawns = context.PlayerPawns[context.Players[0]];
        int finishIndex = context.Controller.GetFullPath(Color.Red).Count - 1;

        for (int index = 0; index < 3; index++)
        {
            PlacePawn(context, redPawns[index], PawnStatus.Finished, finishIndex);
        }

        PlacePawn(
            context,
            redPawns[3],
            PawnStatus.InHomeColumn,
            finishIndex - 1);

        IPlayer? winnerFromEvent = null;
        context.Controller.OnPlayerWon += player => 
        {
            winnerFromEvent = player;
        };
        
        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(redPawns[3].Status, Is.EqualTo(PawnStatus.Finished));
            Assert.That(context.Players[0].IsFinished, Is.True);
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.GameOver));
            Assert.That(state.Winner, Is.SameAs(context.Players[0]));
            Assert.That(winnerFromEvent, Is.SameAs(context.Players[0]));
            Assert.That(context.Controller.CanRoll(), Is.False);
        }
    }

    [Test]
    public void MovePawnToCenter_WhenOtherPawnsAreNotFinished_DoesNotEndGame()
    {
        GameContext context = Builder
            .WithDiceValues(1)
            .Build();
        context.Controller.StartGame();
        IPawn redPawn = context.PlayerPawns[context.Players[0]][0];
        int finishIndex = context.Controller.GetFullPath(Color.Red).Count - 1;
        PlacePawn(context, redPawn, PawnStatus.InHomeColumn, finishIndex - 1);

        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(redPawn.Status, Is.EqualTo(PawnStatus.Finished));
            Assert.That(context.Players[0].IsFinished, Is.False);
            Assert.That(state.Phase, Is.EqualTo(TurnPhase.Rolling));
            Assert.That(state.Winner, Is.Null);
            Assert.That(state.CurrentPlayer, Is.SameAs(context.Players[1]));
        }
    }

    [Test]
    public void ValidGameActions_BroadcastUpdatedStates()
    {
        GameContext context = Builder
            .WithDiceValues(1)
            .Build();
        List<GameState> states = [];
        context.Controller.OnStateChanged += state => states.Add(state);
        context.Controller.StartGame();
        List<IPawn> redPawns = context.PlayerPawns[context.Players[0]];
        PlacePawn(context, redPawns[0], PawnStatus.OnBoard, 0);
        PlacePawn(context, redPawns[1], PawnStatus.OnBoard, 2);

        context.Controller.RollDice();
        context.Controller.SelectPawn(redPawns[0].Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(states, Has.Count.EqualTo(3));
            Assert.That(states[0].Phase, Is.EqualTo(TurnPhase.Rolling));
            Assert.That(states[1].Phase, Is.EqualTo(TurnPhase.SelectingPawn));
            Assert.That(states[2].CurrentPlayer, Is.SameAs(context.Players[1]));
        }
    }

    [Test]
    public void GetGameState_ReturnsCurrentGameInformation()
    {
        GameContext context = Builder
            .WithDiceValues(6)
            .Build();
        context.Controller.StartGame();
        context.Controller.RollDice();

        GameState state = context.Controller.GetGameState();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(state.Players, Has.Count.EqualTo(2));
            Assert.That(state.PlayerPawns, Has.Count.EqualTo(2));
            Assert.That(state.LastDiceValue, Is.EqualTo(6));
            Assert.That(state.ExtraRollPending, Is.True);
            Assert.That(state.Winner, Is.Null);
            Assert.That(
                state.PlayerPawns[context.Players[0]],
                Has.Count.EqualTo(4));
        }
    }
}
