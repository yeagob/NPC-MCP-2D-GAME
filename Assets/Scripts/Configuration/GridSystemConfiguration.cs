using UnityEngine;

namespace Grid.Configuration
{
    public static class GridSystemConfiguration
    {
        public const float DEFAULT_CELL_WIDTH = 1.0f;
        public const float DEFAULT_CELL_HEIGHT = 1.0f;
        public const int DEFAULT_GRID_WIDTH = 10;
        public const int DEFAULT_GRID_HEIGHT = 10;
        
        public const float DEFAULT_OFFSET_LEFT = 0.0f;
        public const float DEFAULT_OFFSET_TOP = 0.0f;
        
        public static readonly Color DEFAULT_GRID_COLOR = Color.white;
        public static readonly Color DEFAULT_GIZMO_COLOR = Color.cyan;
        public static readonly Color DEFAULT_CELL_NUMBER_COLOR = Color.yellow;
        
        public const bool DEFAULT_SHOW_GRID_NUMBERS = false;
        public const bool DEFAULT_SHOW_GRID_GIZMOS = true;
        
        public const float MULTI_OBJECT_SPACING_RATIO = 0.1f;
        public const int MAX_OBJECTS_PER_CELL = 4;
        
        public const float GIZMO_LINE_THICKNESS = 1.0f;
        public const float GIZMO_TEXT_SIZE = 0.3f;
        
        public const string GRID_SYSTEM_LAYER = "GridSystem";
    }
}