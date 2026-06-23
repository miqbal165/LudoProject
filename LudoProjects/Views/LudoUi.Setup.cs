using LudoProjects.Enums;
using LudoProjects.Interfaces;
using LudoProjects.Models;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static List<IPlayer> CreatePlayers()
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
                Console.WriteLine($"  {colorIndex + 1}. {GetColorName(availableColors[colorIndex])}");
            }
            
            var selectedColorIndex = ReadNumber("Choose: ", 1, availableColors.Count) - 1;
            var selectedColor = availableColors[selectedColorIndex];
            availableColors.RemoveAt(selectedColorIndex);
            players.Add(new Player(name, selectedColor));
        }
        Console.WriteLine();
        Console.WriteLine("List Players:");
        foreach (var player in players)
            Console.WriteLine($"- {player.Name} ({GetColorName(player.Color)})");

        Console.WriteLine();
        Console.Write("Press ENTER to start playing...");
        Console.ReadLine();
        return players;
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
    
    private static string GetColorName(Color color) => color switch
    {
        Color.Red => "Red",
        Color.Blue => "Blue",
        Color.Green => "Green",
        Color.Yellow => "Yellow",
        _ => color.ToString()
    };
}