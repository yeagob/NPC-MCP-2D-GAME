using System;
using System.Collections.Generic;
using Grid.Models.Grid;
using MapSystem.Enums;
using UnityEngine;

namespace MapSystem.Models.Map
{
    [Serializable]
    public class MapCell
    {
        public GridCell gridCell;
        public List<MapElement> elements;
        public bool isTraversable;
        public float traversalCost;
        
        public MapCell(GridCell gridCell)
        {
            this.gridCell = gridCell;
            this.elements = new List<MapElement>();
            this.isTraversable = true;
            this.traversalCost = 1.0f;
        }

        public Vector3 WorldPosition => gridCell.position;

        public void AddElement(MapElement element)
        {
            if (element != null && !elements.Contains(element))
            {
                elements.Add(element);
                UpdateTraversability();
            }
        }
        
        public bool RemoveElement(MapElement element)
        {
            bool removed = elements.Remove(element);
            if (removed)
            {
                UpdateTraversability();
            }
            return removed;
        }
        
        public void ClearElements()
        {
            elements.Clear();
            UpdateTraversability();
        }
        
        public bool HasElements()
        {
            return elements.Count > 0;
        }
        
        public int GetElementCount()
        {
            return elements.Count;
        }
        
        public MapElement[] GetElements()
        {
            return elements.ToArray();
        }
        
        public MapElement[] GetElementsByType(MapElementType elementType)
        {
            List<MapElement> filteredElements = new List<MapElement>();
            
            foreach (MapElement element in elements)
            {
                if (element.ElementType == elementType)
                {
                    filteredElements.Add(element);
                }
            }
            
            return filteredElements.ToArray();
        }
        
        public bool HasElementOfType(MapElementType elementType)
        {
            foreach (MapElement element in elements)
            {
                if (element.ElementType == elementType)
                {
                    return true;
                }
            }
            return false;
        }
        
        public int GetElementCountOfType(MapElementType elementType)
        {
            int count = 0;
            foreach (MapElement element in elements)
            {
                if (element.ElementType == elementType)
                {
                    count++;
                }
            }
            return count;
        }
        
        public MapElement GetFirstElementOfType(MapElementType elementType)
        {
            foreach (MapElement element in elements)
            {
                if (element.ElementType == elementType)
                {
                    return element;
                }
            }
            return null;
        }
        
        public bool ContainsElement(MapElement element)
        {
            return elements.Contains(element);
        }
        
        private void UpdateTraversability()
        {
            isTraversable = true;
            traversalCost = 1.0f;
            
            foreach (MapElement element in elements)
            {
                if (!element.CanBeTraversed())
                {
                    isTraversable = false;
                    traversalCost = float.MaxValue;
                    return;
                }
            }
            
            if (elements.Count > 1)
            {
                traversalCost += elements.Count * 0.1f;
            }
        }
        
        public void SetTraversable(bool traversable)
        {
            isTraversable = traversable;
            if (!traversable)
            {
                traversalCost = float.MaxValue;
            }
            else
            {
                UpdateTraversability();
            }
        }
        
        public void SetTraversalCost(float cost)
        {
            traversalCost = Mathf.Max(0.1f, cost);
            if (traversalCost >= float.MaxValue)
            {
                isTraversable = false;
            }
        }
        
        public bool IsEmpty()
        {
            return elements.Count == 0;
        }
        
        public override string ToString()
        {
            return $"MapCell({gridCell}) - Elements: {elements.Count}, Traversable: {isTraversable}";
        }
    }
}