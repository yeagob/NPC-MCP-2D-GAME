using System;
using UnityEngine;

namespace Grid.Models.Grid
{
    [Serializable]
    public class GridConfiguration
    {
        [Header("Grid Dimensions")]
        public int gridWidth = 10;
        public int gridHeight = 10;
        
        [Header("Cell Size")]
        public float cellWidth = 1.0f;
        public float cellHeight = 1.0f;
        
        [Header("Grid Offset")]
        public float offsetLeft = 0.0f;
        public float offsetTop = 0.0f;
        
        [Header("Gizmo Settings")]
        public Color gridColor = Color.white;
        public bool showGridNumbers = false;
        public bool showGridGizmos = true;

        public Vector2 GetCellSize()
        {
            return new Vector2(cellWidth, cellHeight);
        }

        public Vector2 GetGridOffset()
        {
            return new Vector2(offsetLeft, offsetTop);
        }

        public float GetTotalWidth()
        {
            return gridWidth * cellWidth;
        }

        public float GetTotalHeight()
        {
            return gridHeight * cellHeight;
        }

        public bool IsValidGridCell(GridCell cell)
        {
            return cell.IsValid(gridWidth, gridHeight);
        }

        public int GetTotalCells()
        {
            return gridWidth * gridHeight;
        }
    }
}