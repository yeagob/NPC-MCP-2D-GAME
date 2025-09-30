using System;
using System.Numerics;

namespace Grid.Models.Grid
{
    [Serializable]
    public struct GridCell
    {
        public int row;
        public int column;
        public UnityEngine.Vector3 position;

        public GridCell(int row, int column, UnityEngine.Vector3 position)
        {
            this.row = row;
            this.column = column;
            this.position = position;
        }
        
        public GridCell(int row, int column)
        {
            this.row = row;
            this.column = column;
            this.position = UnityEngine.Vector3.zero;
        }

        public int GetLinearIndex(int gridWidth)
        {
            return row * gridWidth + column;
        }

        public bool IsValid(int gridWidth, int gridHeight)
        {
            return row >= 0 && row < gridHeight && column >= 0 && column < gridWidth;
        }

        public override bool Equals(object obj)
        {
            if (obj is GridCell other)
            {
                return row == other.row && column == other.column;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return row.GetHashCode() ^ column.GetHashCode();
        }

        public override string ToString()
        {
            return $"GridCell(Row: {row}, Column: {column}), position: {position}";
        }

        public static bool operator ==(GridCell left, GridCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridCell left, GridCell right)
        {
            return !left.Equals(right);
        }
    }
}