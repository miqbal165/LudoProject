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

        var playerCount = ReadNumber("Total Pemain (2-4): ", 2, 4);
        var players = new List<IPlayer>();
        var availableColors = Enum.GetValues<Color>().ToList();

        for (int i = 0; i < playerCount; i++)
        {
            Console.WriteLine();
            Console.WriteLine($"Pemain {i + 1}");

            string name;
            while (true)
            {
                Console.Write("Nama: ");
                name = (Console.ReadLine() ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Nama tidak boleh kosong");
                    Console.ResetColor();
                    continue;
                }

                if (players.Any(p => p.Name == name))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Nama sudah digunakan.");
                    Console.ResetColor();
                    continue;
                }
                break;
            }
            
            Console.WriteLine("Pilihan warna: ");
            for (int colorIndex = 0; colorIndex < availableColors.Count; colorIndex++)
            {
                Console.WriteLine($"  {colorIndex + 1}. {GetColorName(availableColors[colorIndex])}");
            }
            
            var selectedColorIndex = ReadNumber("Pilih: ", 1, availableColors.Count) - 1;
            
            var selectedColor = availableColors[selectedColorIndex];
            
            availableColors.RemoveAt(selectedColorIndex);
            players.Add(new Player(name, selectedColor));
        }
        Console.WriteLine();
        
        Console.WriteLine("List Pemain:");
        foreach (var player in players)
            Console.WriteLine($"- {player.Name} ({GetColorName(player.Color)})");

        Console.WriteLine();
        Console.Write("Tekan ENTER untuk mulai bermain...");
        Console.ReadLine();
        return players;
    }

    private static int ReadNumber(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out int value) && value >= min && value <= max)
            {
                return value;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Kamu harus menginput dengan nilai minimal {min} dan maksimal {max}.");
            Console.ResetColor();
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