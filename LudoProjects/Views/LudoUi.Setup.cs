using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    private static List<IPlayer> CreatePlayers()
    {
        Console.WriteLine("PLAYERS SETUP");
        Console.WriteLine("-------------");

        var playerCount = ReadNumber("Total Player (2-4): ", 2, 4);
        var players = new List<IPlayer>();
        var availableColors = Enum.GetValues<Color>().ToList();

        for (int i = 0; i < playerCount; i++)
        {
            Console.WriteLine();
            Console.WriteLine($"Player {i + 1}");

            string name;
            while (true)
            {
                Console.Write("Name: ");
                name = (Console.ReadLine() ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.WriteLine("Name is empty.");
                    continue;
                }

                if (players.Any(p => p.Name == name))
                {
                    Console.WriteLine("The name is already in use");
                    continue;
                }
                break;
            }
            
            Console.WriteLine("Choose a color: ");
            for (int colorIndex = 0; colorIndex < availableColors.Count; colorIndex++)
            {
                
            }
        }

        return null;
    }

    private static int ReadNumber(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out var value) && value >= min && value <= max)
            {
                return value;
            }
            
            Console.WriteLine($"You must input the number of players, minimum {min} and maximum {max}.");
        }
    }
}