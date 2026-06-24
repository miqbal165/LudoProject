using LudoProjects.Controllers;
using LudoProjects.Enums;
using LudoProjects.Interfaces;

namespace LudoProjects.Views;

public static partial class LudoUi
{
    public static void RunGame(GameController controller, IBoard board)
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
                        ? $" and kicks {string.Join(", ", automaticallyCaptured)} to base"
                        : string.Empty;
                    
                    var extraText = ReferenceEquals(playerBeforeRoll, afterRoll.CurrentPlayer)
                                    && afterRoll.Phase == TurnPhase.Rolling
                        ? " The player gets an additional roll."
                        : string.Empty;
                    
                    message = $"Dice results {afterRoll.LastDiceValue}. {GetPawnLabel(automaticallyMovedPawn)} " +
                              $"automatically played{captureText}.{extraText}";
                }
                else if (!ReferenceEquals(playerBeforeRoll, afterRoll.CurrentPlayer) && afterRoll.LastDiceValue == 6)
                {
                    message = $"{playerBeforeRoll.Name} got the number 6 three times in a row. Forfeit turn.";
                }
                else if (afterRoll.Phase == TurnPhase.SelectingPawn)
                {
                    message = $"Dice results {playerBeforeRoll.Name}: {afterRoll.LastDiceValue}. Select the pawn to play.";
                }
                else if (ReferenceEquals(playerBeforeRoll, afterRoll.CurrentPlayer) && afterRoll.LastDiceValue == 6)
                {
                    message = "The dice roll is 6, but there is no valid move. The player gets an extra roll.";
                }
                else
                {
                    message = $"Dice results {afterRoll.LastDiceValue}. No pawns may move; turn passes.";
                }

                continue;
            }

            if (state.Phase == TurnPhase.SelectingPawn)
            {
                Console.WriteLine();
                Console.WriteLine($"Dice results: {state.LastDiceValue}");
                Console.WriteLine("Playable pawns:");

                foreach (var pawn in state.MovablePawns.OrderBy(pawn => pawn.Id))
                    Console.WriteLine($"  {pawn.Id + 1}. {DescribeMove(board, pawn, state.LastDiceValue)}");

                int selectedPawnId;
                while (true)
                {
                    Console.Write("Select pawn number: ");
                    if (int.TryParse(Console.ReadLine(), out var selectedNumber))
                    {
                        selectedPawnId = selectedNumber - 1;
                        if (state.MovablePawns.Any(pawn => pawn.Id == selectedPawnId))
                            break;
                    }

                    Console.WriteLine("The pawn cannot be selected.");
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
                    ? $"{moveDescription}; {selectedLabel} kick {string.Join(", ", captured)} return to base."
                    : $"{moveDescription}.";
            }
        }
    }
    
    private static string DescribeMove(IBoard board, IPawn pawn, int diceValue)
    {
        if (pawn.Status == PawnStatus.InBase)
            return $"{GetPawnLabel(pawn)} exit from BASE to START";

        var path = board.GetFullPath(pawn.Color);
        var targetIndex = pawn.StepIndex + diceValue;
        var target = path[targetIndex];

        if (targetIndex == path.Count - 1)
            return $"{GetPawnLabel(pawn)} forward {diceValue} steps to CENTER/FINISH";

        if (targetIndex >= 52)
            return $"{GetPawnLabel(pawn)} forward {diceValue} steps to the Main Column ({target.Row},{target.Column})";

        return $"{GetPawnLabel(pawn)} forward {diceValue} step into the cell ({target.Row},{target.Column})";
    }
}