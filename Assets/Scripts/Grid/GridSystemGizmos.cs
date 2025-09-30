using Grid.Configuration;
using Grid.Models.Grid;
using UnityEngine;

namespace Grid.Grid
{
    public partial class GridSystemGizmos: GridSystem 
    {
        private void OnDrawGizmos()
        {
            if (_gridConfig == null || !_gridConfig.showGridGizmos)
            {
                return;
            }

            DrawGridLines();
            if (_gridConfig.showGridNumbers)
            {
                DrawGridNumbers();
            }
        }

        private void DrawGridLines()
        {
            Gizmos.color = _gridConfig.gridColor;
    
            Vector3 gridStartPosition = GetGridStartPosition();

            for (int row = 0; row < _gridConfig.gridHeight; row++)
            {
                for (int column = 0; column < _gridConfig.gridWidth; column++)
                {
                    Vector3 cellPosition = gridStartPosition + new Vector3(
                        column * _gridConfig.cellWidth, 
                        -row * _gridConfig.cellHeight, 
                        0
                    );
            
                    Vector3 topLeft = transform.TransformPoint(cellPosition);
                    Vector3 topRight = transform.TransformPoint(cellPosition + new Vector3(_gridConfig.cellWidth, 0, 0));
                    Vector3 bottomLeft = transform.TransformPoint(cellPosition + new Vector3(0, -_gridConfig.cellHeight, 0));
                    Vector3 bottomRight = transform.TransformPoint(cellPosition + new Vector3(_gridConfig.cellWidth, -_gridConfig.cellHeight, 0));
            
                    Gizmos.DrawLine(topLeft, topRight);
                    Gizmos.DrawLine(topRight, bottomRight);
                    Gizmos.DrawLine(bottomRight, bottomLeft);
                    Gizmos.DrawLine(bottomLeft, topLeft);
                }
            }
        }

        private void DrawGridNumbers()
        {
            for (int row = 0; row < _gridConfig.gridHeight; row++)
            {
                for (int column = 0; column < _gridConfig.gridWidth; column++)
                {
                    GridCell cell = new GridCell(row, column);
                    Vector3 cellCenter = GetCellCenterWorldPosition(cell);
                    int linearIndex = GetLinearIndex(cell);
                    
                    DrawGizmoText(cellCenter, linearIndex.ToString());
                }
            }
        }

        private void DrawGizmoText(Vector3 position, string text)
        {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = GridSystemConfiguration.DEFAULT_CELL_NUMBER_COLOR;
            UnityEditor.Handles.Label(position, text);
            #endif
        }

        private Vector3 GetGridStartPosition()
        {
            return new Vector3(_gridConfig.offsetLeft, -_gridConfig.offsetTop, 0);
        }

        public void DrawCellHighlight(GridCell cell, Color highlightColor)
        {
            if (!_gridConfig.IsValidGridCell(cell))
            {
                return;
            }

            Gizmos.color = highlightColor;
            Vector3 cellCenter = GetCellCenterWorldPosition(cell);
            Vector3 cellSize = new Vector3(_gridConfig.cellWidth, _gridConfig.cellHeight, 0.1f);
            
            Gizmos.DrawWireCube(cellCenter, cellSize);
        }

        public void DrawCellFill(GridCell cell, Color fillColor)
        {
            if (!_gridConfig.IsValidGridCell(cell))
            {
                return;
            }

            Gizmos.color = fillColor;
            Vector3 cellCenter = GetCellCenterWorldPosition(cell);
            Vector3 cellSize = new Vector3(_gridConfig.cellWidth, _gridConfig.cellHeight, 0.01f);
            
            Gizmos.DrawCube(cellCenter, cellSize);
        }

        public Bounds GetGridBounds()
        {
            Vector3 center = transform.TransformPoint(
                _gridConfig.offsetLeft + _gridConfig.GetTotalWidth() * 0.5f,
                _gridConfig.offsetTop - _gridConfig.GetTotalHeight() * 0.5f,
                0
            );
            
            Vector3 size = new Vector3(
                _gridConfig.GetTotalWidth(),
                _gridConfig.GetTotalHeight(),
                0.1f
            );

            return new Bounds(center, size);
        }

    }
}