using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestDoubles;
using NUnit.Framework;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public sealed class GameControllerConstructionTests
{
    [TestCase(1)]
    [TestCase(5)]
    public void Constructor_WithInvalidPlayerCount_ThrowsArgumentException(
        int playerCount)
    {
        Color[] colors = Enumerable.Range(0, playerCount)
            .Select(index => (Color)(index % 4))
            .ToArray();
        var inputs = CreateValidConstructorInputs(colors);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithDuplicatePlayerColors_ThrowsArgumentException()
    {
        var inputs = CreateValidConstructorInputs(
            [Color.Red, Color.Red]);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WhenPawnCollectionIsMissing_ThrowsArgumentException()
    {
        var inputs = CreateValidConstructorInputs();
        inputs.PlayerPawns.Remove(inputs.Players[1]);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithWrongPawnCount_ThrowsArgumentException()
    {
        var inputs = CreateValidConstructorInputs();
        inputs.PlayerPawns[inputs.Players[0]].RemoveAt(0);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithPawnOfDifferentColor_ThrowsArgumentException()
    {
        var inputs = CreateValidConstructorInputs();
        inputs.PlayerPawns[inputs.Players[0]][0] =
            new Pawn(0, Color.Blue);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithDuplicatePawnIds_ThrowsArgumentException()
    {
        var inputs = CreateValidConstructorInputs();
        inputs.PlayerPawns[inputs.Players[0]][1] =
            new Pawn(0, Color.Red);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithInvalidBoardSize_ThrowsArgumentException()
    {
        var inputs = CreateValidConstructorInputs(
            rows: 14,
            columns: 15);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    private static GameController CreateController(
        (
            List<IPlayer> Players,
            Dictionary<IPlayer, List<IPawn>> PlayerPawns,
            IBoard Board,
            IDice Dice,
            Random Random
        ) inputs)
    {
        return new GameController(
            inputs.Players,
            inputs.PlayerPawns,
            new Dictionary<Color, IReadOnlyList<Position>>(),
            inputs.Board,
            inputs.Dice,
            inputs.Random);
    }

    private static (
        List<IPlayer> Players,
        Dictionary<IPlayer, List<IPawn>> PlayerPawns,
        IBoard Board,
        IDice Dice,
        Random Random
    ) CreateValidConstructorInputs(
        Color[]? playerColors = null,
        int rows = 15,
        int columns = 15)
    {
        Color[] colors = playerColors ?? [Color.Red, Color.Blue];

        List<IPlayer> players = colors
            .Select(color => (IPlayer)new Player($"{color} Player", color))
            .ToList();

        Dictionary<IPlayer, List<IPawn>> playerPawns = players.ToDictionary(
            player => player,
            player => Enumerable.Range(0, 4)
                .Select(id => (IPawn)new Pawn(id, player.Color))
                .ToList());

        Cell[,] cells = new TestBoardBuilder(rows, columns)
            .WithPlayerPawns(playerPawns)
            .AsIncompleteBoard()
            .Build();

        return (
            players,
            playerPawns,
            new Board(cells),
            new Dice(),
            new SequenceRandom());
    }
}
