using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static void ShowTitle()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("            LUDO GAME CONSOLE");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }
    
    public static void DrawBoard(GameController controller)
    {
        Console.WriteLine("BOARD");
        Console.WriteLine("-----");

        for (int row = 0; row < 15; row++)
        {
            for (int column = 0; column < 15; column++)
            {
                ICell cell = controller.GetCell(new Position(row, column));
                string token = GetCellToken(cell);
                ConsoleColor consoleColor = GetCellConsoleColor(cell);
                
                Console.Write("[");
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = consoleColor;
                Console.Write(token.PadRight(3)[..3]);
                Console.ForegroundColor = originalColor;
                Console.Write("]");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("Keterangan : R/B/G/Y = warna pawn, S = start, * = protected, H = home");
        Console.WriteLine("            R#2/B#2/G#2/Y#2 = BLOCK (2+ pawn dengan warna yang sama)");
    }

    private static string GetCellToken(ICell cell)
    {
        
        if (cell.OccupyingPawns.Count > 0)
        {
            List<IGrouping<Color,IPawn>> groups = cell.OccupyingPawns.GroupBy(pawn => pawn.Color).ToList();
            if (groups.Count == 1)
            {
                List<IPawn> group = groups[0].OrderBy(pawn => pawn.Id).ToList();
                string letter = GetColorLetter(group[0].Color);

                if (group.Count >= 2)
                    return $"{letter}#{group.Count}";
                
                return $"{letter}{group[0].Id + 1}";
            }

            string mixedLetters = string.Concat(groups.Select(group => GetColorLetter(group.Key)));
            return mixedLetters.Length <= 3 ? mixedLetters : "MIX";
        }
        
        return cell.Type switch
        {
            CellType.Start => $"S{GetColorLetter(cell.Color)}",
            CellType.Protected => " * ",
            CellType.HomeColumn => $"H{GetColorLetter(cell.Color)}",
            CellType.Center => " C ",
            CellType.Base => "<^>",
            _ => "  "
        };
    }

    private static string GetColorLetter(Color? color)
    {
        string colorLetter = color switch
        {
            Color.Red => "R",
            Color.Blue => "B",
            Color.Green => "G",
            Color.Yellow => "Y",
            _ => "-"
        };

        return colorLetter;
    }
    
    private static ConsoleColor GetCellConsoleColor(ICell cell)
    {
        Color? color = cell.OccupyingPawns.FirstOrDefault()?.Color ?? cell.Color;
        return color switch
        {
            Color.Red => ConsoleColor.Red,
            Color.Blue => ConsoleColor.Blue,
            Color.Green => ConsoleColor.Green,
            Color.Yellow => ConsoleColor.Yellow,
            _ => ConsoleColor.Gray
        };
    }
    
    public static void DrawGameState(GameState state)
    {
        Console.WriteLine();
        Console.WriteLine("GAME STATUS");
        Console.WriteLine("-----------");
        Console.WriteLine($"Giliran      : {state.CurrentPlayer.Name} ({GetColorName(state.CurrentPlayer.Color)})");
        Console.WriteLine($"Fase         : {state.Phase}");
        Console.WriteLine($"Nilai Dadu   : {(state.LastDiceValue == 0 ? "-" : state.LastDiceValue)}");
        Console.WriteLine($"Extra Roll   : {(state.ExtraRollPending ? "Iya" : "Tidak")}");
        Console.WriteLine();

        foreach (IPlayer player in state.Players)
        {
            IEnumerable<string> pawnDescriptions = state.PlayerPawns[player]
                .OrderBy(pawn => pawn.Id)
                .Select(DescribePawnStatus);
            Console.WriteLine($"{player.Name,-12} ({GetColorLetter(player.Color)}) : {string.Join(" | ", pawnDescriptions)}");
        }
    }

    private static string DescribePawnStatus(IPawn pawn)
    {
        string status = pawn.Status switch
        {
            PawnStatus.InBase => $"{GetPawnLabel(pawn)}=Base",
            PawnStatus.OnBoard => $"{GetPawnLabel(pawn)}=Track-{pawn.StepIndex}",
            PawnStatus.InHomeColumn => $"{GetPawnLabel(pawn)}=Home-{pawn.StepIndex - 51}",
            PawnStatus.Finished => $"{GetPawnLabel(pawn)}=Finish",
            _ => GetPawnLabel(pawn)
        };
        
        return status;
    }
    
    private static string GetPawnLabel(IPawn pawn) => $"{GetColorLetter(pawn.Color)}{pawn.Id + 1}";
    
    private static void TryClearConsole()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Consoles that don't support Clear can still run the game.
        }
    }
    
}