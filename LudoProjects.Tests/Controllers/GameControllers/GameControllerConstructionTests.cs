using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.Builders;
using LudoProjects.Tests.TestDoubles;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Controllers.GameControllers;

[TestFixture]
public sealed class GameControllerConstructionTests
{
    private ValidConstructorInputs _validInputs = null!;

    [SetUp]
    public void SetUp()
    {
        _validInputs = CreateValidConstructorInputs();
    }

    [TestCase(1)]
    [TestCase(5)]
    public void Constructor_WithInvalidPlayerCount_ThrowsArgumentException(
        int playerCount)
    {
        Color[] colors = Enumerable.Range(0, playerCount)
            .Select(index => (Color)(index % 4))
            .ToArray();
        ValidConstructorInputs inputs = CreateValidConstructorInputs(colors);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithDuplicatePlayerColors_ThrowsArgumentException()
    { 
        ValidConstructorInputs inputs = CreateValidConstructorInputs(
            [Color.Red, Color.Red]);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WhenPawnCollectionIsMissing_ThrowsArgumentException()
    {
        ValidConstructorInputs inputs = _validInputs;
        inputs.PlayerPawns.Remove(inputs.Players[1]);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithWrongPawnCount_ThrowsArgumentException()
    {
        ValidConstructorInputs inputs = _validInputs;
        inputs.PlayerPawns[inputs.Players[0]].RemoveAt(0);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithPawnOfDifferentColor_ThrowsArgumentException()
    {
        ValidConstructorInputs inputs = _validInputs;
        inputs.PlayerPawns[inputs.Players[0]][0] =
            new Pawn(0, Color.Blue);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithDuplicatePawnIds_ThrowsArgumentException()
    {
        ValidConstructorInputs inputs = _validInputs;
        inputs.PlayerPawns[inputs.Players[0]][1] =
            new Pawn(0, Color.Red);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    [Test]
    public void Constructor_WithInvalidBoardSize_ThrowsArgumentException()
    {
        ValidConstructorInputs inputs = CreateValidConstructorInputs(
            rows: 14,
            columns: 15);

        Action createController = () => _ = CreateController(inputs);

        Assert.Throws<ArgumentException>(createController);
    }

    private static GameController CreateController(ValidConstructorInputs inputs)
    {
        return new GameController(
            inputs.Players,
            inputs.PlayerPawns,
            new Dictionary<Color, IReadOnlyList<Position>>(),
            inputs.Board,
            inputs.Dice,
            inputs.Random);
    }

    private static ValidConstructorInputs CreateValidConstructorInputs(
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

        return new ValidConstructorInputs(
            players,
            playerPawns,
            new Board(cells),
            new Dice(),
            new SequenceRandom());
    }
}
