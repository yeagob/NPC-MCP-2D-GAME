using System.Collections.Generic;
using Grid.Enums;
using Grid.Models.Grid;
using UnityEngine;

namespace Grid.Grid
{
    public static class GridUtilities
    {
        public static List<GridCell> GetCellsInRadius(this GridSystem gridSystem, GridCell centerCell, int radius)
        {
            List<GridCell> cellsInRadius = new List<GridCell>();
            GridConfiguration config = gridSystem.GetGridConfiguration();
            
            for (int row = centerCell.row - radius; row <= centerCell.row + radius; row++)
            {
                for (int column = centerCell.column - radius; column <= centerCell.column + radius; column++)
                {
                    GridCell currentCell = new GridCell(row, column);
                    
                    if (config.IsValidGridCell(currentCell))
                    {
                        int distance = GetManhattanDistance(centerCell, currentCell);
                        if (distance <= radius)
                        {
                            cellsInRadius.Add(currentCell);
                        }
                    }
                }
            }
            
            return cellsInRadius;
        }

        public static List<GridCell> GetCellsInSquareArea(this GridSystem gridSystem, GridCell topLeft, GridCell bottomRight)
        {
            List<GridCell> cellsInArea = new List<GridCell>();
            GridConfiguration config = gridSystem.GetGridConfiguration();
            
            int startRow = Mathf.Min(topLeft.row, bottomRight.row);
            int endRow = Mathf.Max(topLeft.row, bottomRight.row);
            int startColumn = Mathf.Min(topLeft.column, bottomRight.column);
            int endColumn = Mathf.Max(topLeft.column, bottomRight.column);
            
            for (int row = startRow; row <= endRow; row++)
            {
                for (int column = startColumn; column <= endColumn; column++)
                {
                    GridCell currentCell = new GridCell(row, column);
                    
                    if (config.IsValidGridCell(currentCell))
                    {
                        cellsInArea.Add(currentCell);
                    }
                }
            }
            
            return cellsInArea;
        }

        public static List<GridCell> GetCellsInLine(this GridSystem gridSystem, GridCell startCell, GridCell endCell)
        {
            List<GridCell> cellsInLine = new List<GridCell>();
            GridConfiguration config = gridSystem.GetGridConfiguration();
            
            if (!config.IsValidGridCell(startCell) || !config.IsValidGridCell(endCell))
            {
                return cellsInLine;
            }
            
            int deltaRow = Mathf.Abs(endCell.row - startCell.row);
            int deltaColumn = Mathf.Abs(endCell.column - startCell.column);
            
            int rowDirection = startCell.row < endCell.row ? 1 : -1;
            int columnDirection = startCell.column < endCell.column ? 1 : -1;
            
            int error = deltaRow - deltaColumn;
            int currentRow = startCell.row;
            int currentColumn = startCell.column;
            
            while (true)
            {
                GridCell currentCell = new GridCell(currentRow, currentColumn);
                
                if (config.IsValidGridCell(currentCell))
                {
                    cellsInLine.Add(currentCell);
                }
                
                if (currentRow == endCell.row && currentColumn == endCell.column)
                {
                    break;
                }
                
                int doubleError = 2 * error;
                
                if (doubleError > -deltaColumn)
                {
                    error -= deltaColumn;
                    currentRow += rowDirection;
                }
                
                if (doubleError < deltaRow)
                {
                    error += deltaRow;
                    currentColumn += columnDirection;
                }
            }
            
            return cellsInLine;
        }

        public static int GetManhattanDistance(GridCell cellA, GridCell cellB)
        {
            return Mathf.Abs(cellA.row - cellB.row) + Mathf.Abs(cellA.column - cellB.column);
        }

        public static float GetEuclideanDistance(GridCell cellA, GridCell cellB)
        {
            int deltaRow = cellA.row - cellB.row;
            int deltaColumn = cellA.column - cellB.column;
            
            return Mathf.Sqrt(deltaRow * deltaRow + deltaColumn * deltaColumn);
        }

        public static int GetChebyshevDistance(GridCell cellA, GridCell cellB)
        {
            return Mathf.Max(Mathf.Abs(cellA.row - cellB.row), Mathf.Abs(cellA.column - cellB.column));
        }

        public static bool AreAdjacentCells(GridCell cellA, GridCell cellB)
        {
            int rowDifference = Mathf.Abs(cellA.row - cellB.row);
            int columnDifference = Mathf.Abs(cellA.column - cellB.column);
            
            return rowDifference <= 1 && columnDifference <= 1 && !(rowDifference == 0 && columnDifference == 0);
        }

        public static bool AreDiagonallyAdjacent(GridCell cellA, GridCell cellB)
        {
            int rowDifference = Mathf.Abs(cellA.row - cellB.row);
            int columnDifference = Mathf.Abs(cellA.column - cellB.column);
            
            return rowDifference == 1 && columnDifference == 1;
        }

        public static bool AreOrthogonallyAdjacent(GridCell cellA, GridCell cellB)
        {
            int rowDifference = Mathf.Abs(cellA.row - cellB.row);
            int columnDifference = Mathf.Abs(cellA.column - cellB.column);
            
            return (rowDifference == 1 && columnDifference == 0) || (rowDifference == 0 && columnDifference == 1);
        }

        public static GridDirection GetDirectionToCell(GridCell fromCell, GridCell toCell)
        {
            int rowDelta = toCell.row - fromCell.row;
            int columnDelta = toCell.column - fromCell.column;
            
            if (rowDelta < 0 && columnDelta == 0) return GridDirection.Top;
            if (rowDelta < 0 && columnDelta > 0) return GridDirection.TopRight;
            if (rowDelta == 0 && columnDelta > 0) return GridDirection.Right;
            if (rowDelta > 0 && columnDelta > 0) return GridDirection.BottomRight;
            if (rowDelta > 0 && columnDelta == 0) return GridDirection.Bottom;
            if (rowDelta > 0 && columnDelta < 0) return GridDirection.BottomLeft;
            if (rowDelta == 0 && columnDelta < 0) return GridDirection.Left;
            if (rowDelta < 0 && columnDelta < 0) return GridDirection.TopLeft;
            
            return GridDirection.Top;
        }

        public static List<GridCell> GetBorderCells(this GridSystem gridSystem)
        {
            List<GridCell> borderCells = new List<GridCell>();
            GridConfiguration config = gridSystem.GetGridConfiguration();
            
            for (int row = 0; row < config.gridHeight; row++)
            {
                for (int column = 0; column < config.gridWidth; column++)
                {
                    if (row == 0 || row == config.gridHeight - 1 || column == 0 || column == config.gridWidth - 1)
                    {
                        borderCells.Add(new GridCell(row, column));
                    }
                }
            }
            
            return borderCells;
        }
        
    }
}