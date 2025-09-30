using Grid.Enums;
using Grid.Models.Grid;

namespace Grid.Grid
{
    public static class GridNavigationExtensions
    {
        public static GridResult<GridCell> GetAdjacentCell(this GridSystem gridSystem, GridCell cell, GridDirection direction)
        {
            GridConfiguration config = gridSystem.GetGridConfiguration();
            GridCell adjacentCell = GetAdjacentCellInternal(cell, direction);
            
            if (config.IsValidGridCell(adjacentCell))
            {
                return GridResult<GridCell>.Success(adjacentCell);
            }
            
            return GridResult<GridCell>.Failure();
        }

        public static GridResult<GridCell> GetTopCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.Top);
        }

        public static GridResult<GridCell> GetTopRightCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.TopRight);
        }

        public static GridResult<GridCell> GetRightCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.Right);
        }

        public static GridResult<GridCell> GetBottomRightCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.BottomRight);
        }

        public static GridResult<GridCell> GetBottomCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.Bottom);
        }

        public static GridResult<GridCell> GetBottomLeftCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.BottomLeft);
        }

        public static GridResult<GridCell> GetLeftCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.Left);
        }

        public static GridResult<GridCell> GetTopLeftCell(this GridSystem gridSystem, GridCell cell)
        {
            return gridSystem.GetAdjacentCell(cell, GridDirection.TopLeft);
        }

        public static GridResult<GridCell>[] GetAllAdjacentCells(this GridSystem gridSystem, GridCell cell)
        {
            GridResult<GridCell>[] results = new GridResult<GridCell>[8];
            
            results[0] = gridSystem.GetTopCell(cell);
            results[1] = gridSystem.GetTopRightCell(cell);
            results[2] = gridSystem.GetRightCell(cell);
            results[3] = gridSystem.GetBottomRightCell(cell);
            results[4] = gridSystem.GetBottomCell(cell);
            results[5] = gridSystem.GetBottomLeftCell(cell);
            results[6] = gridSystem.GetLeftCell(cell);
            results[7] = gridSystem.GetTopLeftCell(cell);
            
            return results;
        }

        private static GridCell GetAdjacentCellInternal(GridCell cell, GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.Top:
                    return new GridCell(cell.row - 1, cell.column);
                case GridDirection.TopRight:
                    return new GridCell(cell.row - 1, cell.column + 1);
                case GridDirection.Right:
                    return new GridCell(cell.row, cell.column + 1);
                case GridDirection.BottomRight:
                    return new GridCell(cell.row + 1, cell.column + 1);
                case GridDirection.Bottom:
                    return new GridCell(cell.row + 1, cell.column);
                case GridDirection.BottomLeft:
                    return new GridCell(cell.row + 1, cell.column - 1);
                case GridDirection.Left:
                    return new GridCell(cell.row, cell.column - 1);
                case GridDirection.TopLeft:
                    return new GridCell(cell.row - 1, cell.column - 1);
                default:
                    return cell;
            }
        }
    }
}