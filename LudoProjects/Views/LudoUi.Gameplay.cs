using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    private static void RunGame(GameController controller, IBoard board)
    {
        string message = "Let's start this game.";
        controller.StartGame();

        while (true)
        {
            var state = controller.GetGameState();
            TryClearConsole();
            ShowTitle();
            DrawBoard(board);
            DrawGameState(state);

            if (!string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine();
                Console.WriteLine($"> {message}");
                message = string.Empty;
            }

            if (state.Phase == TurnPhase.GameOver)
            {
                Console.WriteLine();
                Console.WriteLine("========================================");
                Console.WriteLine($"The Winner: {state.Winner?.Name.ToUpper()} " +
                                  $"({GetColorName(state.Winner!.Color)})");
                Console.WriteLine("All player's pawns have reached the CENTER.");
                Console.WriteLine("========================================");
                Console.WriteLine("Press ENTER to exit...");
                Console.ReadLine();
                return;
            }

            if (state.Phase == TurnPhase.Rolling)
            {
                var playerBeforeRoll = state.CurrentPlayer;
                var pawnStatesBeforeRoll = state.PlayerPawns
                    .SelectMany(pair => pair.Value)
                    .ToDictionary(pawn => pawn, pawn => (pawn.Status, pawn.StepIndex));

                Console.WriteLine();
                Console.Write($"{playerBeforeRoll.Name.ToUpper()} Press ENTER to roll the dice...");
                Console.ReadLine();

                controller.RollDice();
                var afterRoll = controller.GetGameState();
                
                var automaticallyMovedPawn = afterRoll.PlayerPawns[playerBeforeRoll]
                    .FirstOrDefault(pawn => pawnStatesBeforeRoll.TryGetValue(pawn, out var oldState)
                                            && (oldState.Status != pawn.Status || oldState.StepIndex != pawn.StepIndex));
                
                var automaticallyCaptured = afterRoll.PlayerPawns
                    .SelectMany(pair => pair.Value)
                    .Where(pawn => pawnStatesBeforeRoll.TryGetValue(pawn, out var oldState)
                                   && oldState.Status != PawnStatus.InBase
                                   && pawn.Status == PawnStatus.InBase)
                    .Select(GetPawnLabel)
                    .ToList();

                if (automaticallyMovedPawn is not null)
                {
                    var captureText = automaticallyCaptured.Count > 0
                        ? $" dan menendang {string.Join(", ", automaticallyCaptured)} ke base"
                        : string.Empty;
                    var extraText = ReferenceEquals(playerBeforeRoll, afterRoll.CurrentPlayer)
                                    && afterRoll.Phase == TurnPhase.Rolling
                        ? " Pemain mendapat roll tambahan."
                        : string.Empty;
                    message = $"Hasil dadu {afterRoll.LastDiceValue}. {GetPawnLabel(automaticallyMovedPawn)} " +
                              $"otomatis dimainkan{captureText}.{extraText}";
                }
                else if (!ReferenceEquals(playerBeforeRoll, afterRoll.CurrentPlayer) && afterRoll.LastDiceValue == 6)
                {
                    message = $"{playerBeforeRoll.Name} mendapat angka 6 tiga kali berturut-turut. Giliran hangus.";
                }
                else if (afterRoll.Phase == TurnPhase.SelectingPawn)
                {
                    message = $"Hasil dadu {playerBeforeRoll.Name}: {afterRoll.LastDiceValue}. Pilih pion yang akan dimainkan.";
                }
                else if (ReferenceEquals(playerBeforeRoll, afterRoll.CurrentPlayer) && afterRoll.LastDiceValue == 6)
                {
                    message = "Hasil dadu 6, tetapi tidak ada langkah valid. Pemain memperoleh roll tambahan.";
                }
                else
                {
                    message = $"Hasil dadu {afterRoll.LastDiceValue}. Tidak ada pion yang dapat bergerak; giliran berpindah.";
                }

                continue;
            }

            if (state.Phase == TurnPhase.SelectingPawn)
            {
                Console.WriteLine();
                Console.WriteLine($"Hasil dadu: {state.LastDiceValue}");
                Console.WriteLine("Pion yang dapat dimainkan:");

                foreach (var pawn in state.MovablePawns.OrderBy(pawn => pawn.Id))
                    Console.WriteLine($"  {pawn.Id + 1}. {DescribeMove(board, pawn, state.LastDiceValue)}");

                int selectedPawnId;
                while (true)
                {
                    Console.Write("Pilih nomor pion: ");
                    if (int.TryParse(Console.ReadLine(), out var selectedNumber))
                    {
                        selectedPawnId = selectedNumber - 1;
                        if (state.MovablePawns.Any(pawn => pawn.Id == selectedPawnId))
                            break;
                    }

                    Console.WriteLine("Pion tersebut tidak dapat dipilih.");
                }

                var beforeMove = state.PlayerPawns
                    .SelectMany(pair => pair.Value)
                    .ToDictionary(pawn => pawn, pawn => (pawn.Status, pawn.StepIndex));
                var selectedPawn = state.MovablePawns.Single(pawn => pawn.Id == selectedPawnId);
                var selectedLabel = GetPawnLabel(selectedPawn);
                var moveDescription = DescribeMove(board, selectedPawn, state.LastDiceValue);

                controller.SelectPawn(selectedPawnId);
                var afterMove = controller.GetGameState();

                var captured = afterMove.PlayerPawns
                    .SelectMany(pair => pair.Value)
                    .Where(pawn => beforeMove.TryGetValue(pawn, out var oldState)
                                   && oldState.Status != PawnStatus.InBase
                                   && pawn.Status == PawnStatus.InBase)
                    .Select(GetPawnLabel)
                    .ToList();

                message = captured.Count > 0
                    ? $"{moveDescription}; {selectedLabel} menendang {string.Join(", ", captured)} kembali ke base."
                    : $"{moveDescription}.";
            }
        }
    }
    
    private static string DescribeMove(IBoard board, IPawn pawn, int diceValue)
    {
        if (pawn.Status == PawnStatus.InBase)
            return $"{GetPawnLabel(pawn)} keluar dari BASE ke START";

        var path = board.GetFullPath(pawn.Color);
        var targetIndex = pawn.StepIndex + diceValue;
        var target = path[targetIndex];

        if (targetIndex == path.Count - 1)
            return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke CENTER/FINISH";

        if (targetIndex >= 52)
            return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke HOME COLUMN ({target.Row},{target.Column})";

        return $"{GetPawnLabel(pawn)} maju {diceValue} langkah ke cell ({target.Row},{target.Column})";
    }
}