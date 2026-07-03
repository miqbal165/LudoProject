using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;
using LudoProjects.Tests.TestDoubles;
using LudoProjects.Tests.TestSupport;

namespace LudoProjects.Tests.Builders;

internal class GameContextBuilder
{
    private Color[] _playerColors = [Color.Red, Color.Blue];
    private int[] _diceValues = [];
    private bool _initializeBoard = true;

    public GameContextBuilder WithPlayers(params Color[] colors)
    {
        _playerColors = colors;
        return this;
    }

    public GameContextBuilder WithDiceValues(params int[] values)
    {
        _diceValues = values;
        return this;
    }

    public GameContextBuilder WithReadyBoard()
    {
        _initializeBoard = true;
        return this;
    }

    public GameContextBuilder WithIncompleteBoard()
    {
        _initializeBoard = false;
        return this;
    }

    public GameContext Build()
    {
        List<IPlayer> players = _playerColors
            .Select(color => (IPlayer)new Player($"{color} Player", color))
            .ToList();

        Dictionary<IPlayer, List<IPawn>> playerPawns = players.ToDictionary(
            player => player,
            player => CreatePawns(player.Color));

        TestBoardBuilder boardBuilder = new TestBoardBuilder()
            .WithPlayerPawns(playerPawns);

        if (_initializeBoard)
        {
            boardBuilder.AsReadyBoard();
        }
        else
        {
            boardBuilder.AsIncompleteBoard();
        }

        Cell[,] cells = boardBuilder.Build();
        IBoard board = new Board(cells);
        IDice dice = new Dice();
        Random random = new SequenceRandom(_diceValues);

        GameController controller = new (
            players,
            playerPawns,
            new Dictionary<Color, IReadOnlyList<Position>>(),
            board,
            dice,
            random);

        return new GameContext(
            controller,
            players,
            playerPawns,
            cells);
    }

    private static List<IPawn> CreatePawns(Color color)
    {
        return Enumerable.Range(0, 4)
            .Select(id => (IPawn)new Pawn(id, color))
            .ToList();
    }
}
