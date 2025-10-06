using System.Collections.Generic;
using System.Threading;
using Grid;
using UnityEngine;
using Grid.Models.Grid;
using MapSystem.Elements;
using MapSystem.Models.Map;
using MapSystem.Enums;
using UnityEngine.Serialization;

namespace MapSystem
{
    public class MapSystem : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GridSystem _gridSystem;

        [FormerlySerializedAs("_autoCreateCells")]
        [Header("Map Configuration")]
        private bool _initialized = false;

        [SerializeField]
        private bool _debugMode = false;

        private MapCell[] _mapCells;
        private Dictionary<string, MapElement> _registeredElements;

        public GridSystem GridSystem => _gridSystem;
        public bool IsInitialized => _initialized;

        private void Start()
        {
            InitializeMapSystem();
        }

        private void InitializeMapSystem()
        {
            if (!_initialized)
            {
                _registeredElements = new Dictionary<string, MapElement>();
                _gridSystem.Initialize();
                CreateMapCells();
                RegisterElements();
                _initialized = true;
            }
        }

        private void RegisterElements()
        {
            foreach (Transform child in transform)
            {
                MapElement mapElement = child.GetComponent<MapElement>();
                if (mapElement != null)
                {
                    RegisterElement(mapElement);
                }
            }
        }

        private void CreateMapCells()
        {
            if (_gridSystem == null)
            {
                Debug.LogError("MapSystem: GridSystem dependency not found!");
                return;
            }

            GridConfiguration config = _gridSystem.GetGridConfiguration();
            int totalCells = config.GetTotalCells();

            _mapCells = new MapCell[totalCells];

            for (int i = 0; i < totalCells; i++)
            {
                GridCell gridCell = _gridSystem.GetGridCellFromLinearIndex(i);
                _mapCells[i] = new MapCell(gridCell);
            }

            if (_debugMode)
            {
                Debug.Log($"MapSystem: Created {totalCells} map cells");
            }
        }

        public void RegisterElement(MapElement element)
        {
            if (element == null)
            {
                return;
            }

            string elementId = element.Id.ToString();

            if (!_registeredElements.ContainsKey(elementId))
            {
                _registeredElements[elementId] = element;

                GridCell elementCell = _gridSystem.GetGridCellFromLinearIndex(element.CurrentGridIndex);

                element.SetGridPosition(elementCell, this);

                MapCell mapCell = GetMapCell(elementCell);

                if (mapCell != null)
                {
                    mapCell.AddElement(element);

                    if (_debugMode)
                    {
                        Debug.Log($"MapSystem: Registered element {element.name} at {elementCell}");
                    }
                }
            }
        }

        public void UnregisterElement(MapElement element)
        {
            if (element == null)
            {
                return;
            }

            string elementId = element.Context.elementId;

            if (_registeredElements.ContainsKey(elementId))
            {
                _registeredElements.Remove(elementId);

                GridCell elementCell = element.CurrentGridCell;
                MapCell mapCell = GetMapCell(elementCell);

                if (mapCell != null)
                {
                    mapCell.RemoveElement(element);

                    if (_debugMode)
                    {
                        Debug.Log($"MapSystem: Unregistered element {element.name} from {elementCell}");
                    }
                }
            }
        }

        public void MoveElement(MapElement element, GridCell newCell)
        {
            if (element == null)
            {
                return;
            }

            GridCell oldCell = element.CurrentGridCell;
            MapCell oldMapCell = GetMapCell(oldCell);
            MapCell newMapCell = GetMapCell(newCell);

            if (oldMapCell != null && newMapCell != null)
            {
                oldMapCell.RemoveElement(element);
                newMapCell.AddElement(element);
                
                element.SetGridPosition(newCell, this);
                if (newMapCell.elements.Count > 1)
                {
                    Vector3 [] positions = _gridSystem.GetCellMultiplePositions(newCell, newMapCell.elements.Count);
                    for (int i = 0; i < newMapCell.elements.Count; i++)
                    {
                        newMapCell.elements[i].transform.position = positions[i];
                    }
                }

                if (_debugMode)
                {
                    Debug.Log($"MapSystem: Moved element {element.name} from {oldCell} to {newCell}");
                }
            }
        }

        public MapCell GetMapCell(GridCell gridCell)
        {
            if (_mapCells == null)
            {
                return null;
            }

            GridConfiguration config = _gridSystem.GetGridConfiguration();
            if (!config.IsValidGridCell(gridCell))
            {
                return null;
            }

            int linearIndex = _gridSystem.GetLinearIndex(gridCell);
            return _mapCells[linearIndex];
        }

        public GridCell GetGridCell(int row, int column)
        {
            GridCell gridCell = _gridSystem.GetGridCell(row, column);
            return gridCell;
        }

        public Vector3 GetWorldPositionFromGridCell(GridCell gridCell)
        {
            if (_gridSystem != null)
            {
                return _gridSystem.GetCellCenterWorldPosition(gridCell);
            }

            return Vector3.zero;
        }

        public GridResult<GridCell> GetGridCellFromWorldPosition(Vector3 worldPosition)
        {
            if (_gridSystem != null)
            {
                return _gridSystem.WorldPointToGridCell(worldPosition);
            }

            return GridResult<GridCell>.Failure();
        }

        public float GetDistanceBetweenElements(MapElement elementA, MapElement elementB)
        {
            if (elementA == null || elementB == null || _gridSystem == null)
            {
                return float.MaxValue;
            }

            Vector3 positionA = GetWorldPositionFromGridCell(elementA.CurrentGridCell);
            Vector3 positionB = GetWorldPositionFromGridCell(elementB.CurrentGridCell);

            return Vector3.Distance(positionA, positionB);
        }

        public int GetGridDistanceBetweenElements(MapElement elementA, MapElement elementB)
        {
            if (elementA == null || elementB == null)
            {
                return int.MaxValue;
            }

            return Grid.Grid.GridUtilities.GetManhattanDistance(
                elementA.CurrentGridCell,
                elementB.CurrentGridCell
            );
        }

        public bool IsCellTraversable(GridCell gridCell)
        {
            MapCell mapCell = GetMapCell(gridCell);
            return mapCell != null && mapCell.isTraversable;
        }

        public bool IsCellTraversable(int row, int column)
        {
            return IsCellTraversable(new GridCell(row, column));
        }

        public MapElement[] GetAllElements()
        {
            MapElement[] elements = new MapElement[_registeredElements.Count];
            _registeredElements.Values.CopyTo(elements, 0);
            return elements;
        }

        public MapElement[] GetElementsByType(MapElementType elementType)
        {
            List<MapElement> filteredElements = new List<MapElement>();

            foreach (MapElement element in _registeredElements.Values)
            {
                if (element.ElementType == elementType)
                {
                    filteredElements.Add(element);
                }
            }

            return filteredElements.ToArray();
        }

        public MapElement GetElementById(string elementId)
        {
            _registeredElements.TryGetValue(elementId, out MapElement element);
            return element;
        }

        public int GetTotalElementCount()
        {
            return _registeredElements.Count;
        }

        public int GetElementCountByType(MapElementType elementType)
        {
            int count = 0;
            foreach (MapElement element in _registeredElements.Values)
            {
                if (element.ElementType == elementType)
                {
                    count++;
                }
            }

            return count;
        }

        public MapCell[] GetAllMapCells()
        {
            InitializeMapSystem();

            return _mapCells;
        }

        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
        }

        public MapCell[] GetAllCellsWithElements()
        {
            List<MapCell> result = new List<MapCell>();

            foreach (MapCell cell in GetAllMapCells())
            {
                if (cell.HasElements())
                {
                    result.Add(cell);
                }
            }

            return result.ToArray();
        }

        public List<CharacterElement> GetAllCharactesAtDistance(int cellDistance, GridCell originCell)
        {
            List<CharacterElement> result = new List<CharacterElement>();

            foreach (MapCell cell in GetAllMapCells())
            {
                if (cell.HasElements() && GridSystem.GetDistance(cell.gridCell, originCell) < cellDistance)
                {
                    foreach (MapElement mapElement in cell.elements)
                    {
                        if (mapElement is CharacterElement characterElement && mapElement.CurrentGridCell != originCell)
                        {
                            result.Add(characterElement);
                        }
                    }
                }
            }

            return result;
        }
    }
}