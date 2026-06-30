using LudoProjects.Enums;
using LudoProjects.Models;

namespace LudoProjects.Interfaces;


public interface IBoard
{
    Cell[,] Cells { get; }
}