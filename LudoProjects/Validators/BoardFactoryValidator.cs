using LudoProjects.Controllers;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Validators;

public static class BoardFactoryValidator
{
    public static void Validate(
        GameController controller,
        Cell[,] cells,
        Dictionary<IPlayer, List<IPawn>> playerPawns)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(cells);
        ArgumentNullException.ThrowIfNull(playerPawns);
    }
}