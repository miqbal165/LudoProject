using LudoProjects.Interfaces;

namespace LudoProjects.Models;

public sealed class Board : IBoard
{
    public Cell[,] Cells { get; }
    
    public Board(Cell[,] cells)
    {
        Cells = cells;
    }
}