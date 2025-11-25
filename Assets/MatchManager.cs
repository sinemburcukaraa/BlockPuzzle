using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public GridManager Grid;
    public int MinMatchSize = 3;

    public List<List<(int x, int y)>> EvaluateMatches()
    {
        var removedGroups = new List<List<(int x, int y)>>();
        var visited = new bool[Grid.Width, Grid.Height];

        for (int x = 0; x < Grid.Width; x++)
        {
            for (int y = 0; y < Grid.Height; y++)
            {
                if (visited[x, y])
                    continue;
                var cell = Grid.GetCell(x, y);
                if (cell == null || !cell.IsFilled)
                    continue;

                var group = FloodFill(x, y, cell.Block.Type, visited);
                if (group.Count >= MinMatchSize)
                    removedGroups.Add(group);
            }
        }

        return removedGroups;
    }

    List<(int x, int y)> FloodFill(int sx, int sy, BlockType type, bool[,] visited)
    {
        var result = new List<(int x, int y)>();
        var stack = new Stack<(int x, int y)>();
        stack.Push((sx, sy));

        int[,] dirs = new int[,]
        {
            { 1, 0 },
            { -1, 0 },
            { 0, 1 },
            { 0, -1 },
        };

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            if (!Grid.InBounds(x, y))
                continue;
            if (visited[x, y])
                continue;
            visited[x, y] = true;

            var c = Grid.GetCell(x, y);
            if (c == null || !c.IsFilled)
                continue;
            if (c.Block.Type != type)
                continue;

            result.Add((x, y));

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dirs[i, 0];
                int ny = y + dirs[i, 1];
                if (Grid.InBounds(nx, ny) && !visited[nx, ny])
                    stack.Push((nx, ny));
            }
        }

        return result;
    }

    // Removes groups and returns total removed count
    public int RemoveGroups(List<List<(int x, int y)>> groups)
    {
        int removed = 0;
        foreach (var group in groups)
        {
            foreach (var pos in group)
            {
                var cell = Grid.GetCell(pos.x, pos.y);
                if (cell != null && cell.IsFilled)
                {
                    Destroy(cell.Block.gameObject);
                    cell.Block = null;
                    removed++;
                }
            }
        }
        return removed;
    }
}
