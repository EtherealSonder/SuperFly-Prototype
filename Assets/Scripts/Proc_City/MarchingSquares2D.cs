using System.Collections.Generic;
using UnityEngine;

public static class MarchingSquares2D
{
    /// <summary>
    /// Extracts the outer contour from a boolean 2D grid using Marching Squares.
    /// </summary>
    /// <param name="grid">bool[,] grid of filled cells</param>
    /// <returns>List of Vector2 points forming the outline polygon</returns>
    public static List<Vector2> GetOutline(bool[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        Vector2Int start = FindStartingEdge(grid);
        if (start == new Vector2Int(-1, -1))
        {
            Debug.LogWarning("No region found to trace.");
            return new List<Vector2>();
        }

        List<Vector2> outline = new List<Vector2>();
        Vector2Int pos = start;
        int dir = 0; // Start moving right

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),  // Right
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(0, 1),  // Up
        };

        int safety = 0;
        do
        {
            int caseIndex = GetCase(grid, pos.x, pos.y);
            Vector2 corner = GetCorner(pos, caseIndex);
            if (outline.Count == 0 || (outline[outline.Count - 1] - corner).sqrMagnitude > 0.01f)
                outline.Add(corner);

            // Decide movement direction based on caseIndex (simplified pathing)
            switch (caseIndex)
            {
                case 1:
                case 5:
                case 13: dir = 3; break; // Up
                case 8:
                case 10:
                case 11: dir = 2; break; // Left
                case 4:
                case 12:
                case 14: dir = 1; break; // Down
                case 2:
                case 3:
                case 7: dir = 0; break; // Right
                default: dir = (dir + 1) % 4; break;
            }

            pos += directions[dir];
            safety++;
            if (safety > 1000)
            {
                Debug.LogError("MarchingSquares2D: Infinite loop safety break.");
                break;
            }

        } while (pos != start);

        return outline;
    }

    private static int GetCase(bool[,] grid, int x, int y)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        int index = 0;
        if (x >= 0 && y >= 0 && x < width && y < height && grid[x, y]) index |= 1;
        if (x + 1 >= 0 && y >= 0 && x + 1 < width && y < height && grid[x + 1, y]) index |= 2;
        if (x + 1 >= 0 && y + 1 >= 0 && x + 1 < width && y + 1 < height && grid[x + 1, y + 1]) index |= 4;
        if (x >= 0 && y + 1 >= 0 && x < width && y + 1 < height && grid[x, y + 1]) index |= 8;

        return index;
    }

    private static Vector2 GetCorner(Vector2Int pos, int caseIndex)
    {
        // Return approximate center of the edge for visual smoothness
        switch (caseIndex)
        {
            case 1: return pos + new Vector2(0, 0.5f);
            case 2: return pos + new Vector2(0.5f, 0);
            case 3: return pos + new Vector2(0.5f, 0.5f);
            case 4: return pos + new Vector2(1, 0.5f);
            case 6: return pos + new Vector2(0.5f, 0.5f);
            case 7: return pos + new Vector2(0.5f, 1);
            case 8: return pos + new Vector2(0.5f, 1);
            case 9: return pos + new Vector2(0, 0.5f);
            case 10: return pos + new Vector2(0.5f, 1);
            case 11: return pos + new Vector2(0.5f, 0.5f);
            case 12: return pos + new Vector2(1, 0.5f);
            case 13: return pos + new Vector2(0, 0.5f);
            case 14: return pos + new Vector2(0.5f, 0);
            default: return pos;
        }
    }

    private static Vector2Int FindStartingEdge(bool[,] grid)
    {
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (grid[x, y])
                    return new Vector2Int(x, y);

        return new Vector2Int(-1, -1);
    }
}
