using System;
using UnityEngine;
using Grid.Models.Grid;
using Grid.Configuration;
using MapSystem.Models.Map;

namespace Grid
{
    public class GridSystem : MonoBehaviour
    {
        [Header("Grid Configuration")]
        [SerializeField] protected GridConfiguration _gridConfig;

        [Header("Input")]
        [SerializeField] private Camera _targetCamera;

        public void Initialize()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            if (_gridConfig == null)
            {
                CreateDefaultConfiguration();
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        private void CreateDefaultConfiguration()
        {
            _gridConfig = new GridConfiguration
            {
                gridWidth = GridSystemConfiguration.DEFAULT_GRID_WIDTH,
                gridHeight = GridSystemConfiguration.DEFAULT_GRID_HEIGHT,
                cellWidth = GridSystemConfiguration.DEFAULT_CELL_WIDTH,
                cellHeight = GridSystemConfiguration.DEFAULT_CELL_HEIGHT,
                offsetLeft = GridSystemConfiguration.DEFAULT_OFFSET_LEFT,
                offsetTop = GridSystemConfiguration.DEFAULT_OFFSET_TOP,
                gridColor = GridSystemConfiguration.DEFAULT_GRID_COLOR,
                showGridNumbers = GridSystemConfiguration.DEFAULT_SHOW_GRID_NUMBERS,
                showGridGizmos = GridSystemConfiguration.DEFAULT_SHOW_GRID_GIZMOS
            };
        }

        public GridResult<GridCell> ScreenPointToGridCell(Vector3 screenPoint)
        {
            if (_targetCamera == null)
            {
                return GridResult<GridCell>.Failure();
            }

            Vector3 worldPoint = _targetCamera.ScreenToWorldPoint(screenPoint);
            return WorldPointToGridCell(worldPoint);
        }

        public GridResult<GridCell> WorldPointToGridCell(Vector3 worldPoint)
        {
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            
            float adjustedX = localPoint.x - _gridConfig.offsetLeft;
            float adjustedY = localPoint.y + _gridConfig.offsetTop;
            
            int column = Mathf.FloorToInt(adjustedX / _gridConfig.cellWidth);
            int row = Mathf.FloorToInt(-adjustedY / _gridConfig.cellHeight);
            
            float worldX = _gridConfig.offsetLeft + (column * _gridConfig.cellWidth) + (_gridConfig.cellWidth * 0.5f);
            float worldY = -(_gridConfig.offsetTop + (row * _gridConfig.cellHeight) + (_gridConfig.cellHeight * 0.5f));
            Vector3 localPosition = new Vector3(worldX, worldY, 0f);
            Vector3 center = transform.TransformPoint(localPosition);
            
            GridCell cell = new GridCell(row, column, center);
            
            if (_gridConfig.IsValidGridCell(cell))
            {
                return GridResult<GridCell>.Success(cell);
            }
            
            return GridResult<GridCell>.Failure();
        }

        public GridCell GetGridCell(int row, int column)
        {
            Vector3 worldPoint = GetGetCellCenterWorldPosition(row, column);
            return new GridCell(row, column, worldPoint); 
        }
        
        public Vector3 GetGetCellCenterWorldPosition(int row, int column)
        {
            return GetCellCenterWorldPosition(new GridCell(row, column));
        }
        
        public Vector3 GetCellCenterWorldPosition(GridCell cell)
        {
            if (!_gridConfig.IsValidGridCell(cell))
            {
                return Vector3.zero;
            }

            float worldX = _gridConfig.offsetLeft + (cell.column * _gridConfig.cellWidth) + (_gridConfig.cellWidth * 0.5f);
            float worldY = -(_gridConfig.offsetTop + (cell.row * _gridConfig.cellHeight) + (_gridConfig.cellHeight * 0.5f));
            
            Vector3 localPosition = new Vector3(worldX, worldY, 0f);
            return transform.TransformPoint(localPosition);
        }

        //TODO: Mover esta a CellGrid, deberían conocer su tamaño...
        public Vector3[] GetCellMultiplePositions(GridCell cell, int objectCount)
        {
            if (!_gridConfig.IsValidGridCell(cell) || objectCount < 1 || objectCount > GridSystemConfiguration.MAX_OBJECTS_PER_CELL)
            {
                return Array.Empty<Vector3>();
            }

            Vector3 centerPosition = GetCellCenterWorldPosition(cell);
            Vector3[] positions = new Vector3[objectCount];

            if (objectCount == 1)
            {
                positions[0] = centerPosition;
                return positions;
            }

            float cellWorldWidth = _gridConfig.cellWidth;
            float spacing = cellWorldWidth / (objectCount + 1);
            float startX = centerPosition.x - (cellWorldWidth * 0.5f) + spacing;

            for (int i = 0; i < objectCount; i++)
            {
                positions[i] = new Vector3(
                    startX + (i * spacing),
                    centerPosition.y,
                    centerPosition.z
                );
            }

            return positions;
        }

        public int GetLinearIndex(GridCell cell)
        {
            return cell.GetLinearIndex(_gridConfig.gridWidth);
        }
        

        public GridConfiguration GetGridConfiguration()
        {
            return _gridConfig;
        }

        public GridCell GetGridCellFromLinearIndex(int i)
        {
            if (i < 0 || i >= _gridConfig.GetTotalCells())
            {
                return new GridCell(-1, -1, Vector3.zero);
            }
    
            int row = i / _gridConfig.gridWidth;
            int column = i % _gridConfig.gridWidth;
            
            float worldX = _gridConfig.offsetLeft + (column * _gridConfig.cellWidth) + (_gridConfig.cellWidth * 0.5f);
            float worldY = -(_gridConfig.offsetTop + (row * _gridConfig.cellHeight) + (_gridConfig.cellHeight * 0.5f));
            Vector3 localPosition = new Vector3(worldX, worldY, 0f);
            Vector3 center = transform.TransformPoint(localPosition);
    
            return new GridCell(row, column, center);
        }
        

        public int GetDistance(GridCell finalCell, GridCell originCell)
        {
            Vector3 posCellOrigin = GetCellCenterWorldPosition(originCell);
            Vector3 posCellCenter = GetCellCenterWorldPosition(finalCell);
            
            float distance = Vector3.Distance(posCellOrigin, posCellCenter) / _gridConfig.gridWidth;
            
            return Mathf.RoundToInt(distance);
            
        }

        public static int CoordToIndex(int row, int col)
        {
            return row * GridConfiguration.GridWidth + col;
        }
    }
        
}