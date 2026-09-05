using System;
using System.Collections.Generic;
using UnityEngine;

public interface IGrid<T>
{
    int Width  { get; }
    int Height { get; }
    T   Get(int x, int y);
    void Set(int x, int y, T value);
    bool InBounds(int x, int y);
}

public enum TileType { Empty, Floor, Wall, Door, Water, Mountain }

public class Grid2D<T> : IGrid<T>
{
    private readonly T[,] _cells;
    public int Width  { get; }
    public int Height { get; }

    public Grid2D(int width, int height, T defaultValue = default)
    {
        Width = width; Height = height;
        _cells = new T[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _cells[x, y] = defaultValue;
    }

    public T    Get(int x, int y)          => _cells[x, y];
    public void Set(int x, int y, T value) => _cells[x, y] = value;
    public bool InBounds(int x, int y)     => x >= 0 && y >= 0 && x < Width && y < Height;
}

public class BSPDungeonGenerator
{
    private readonly System.Random _rng;

    public List<RectInt> Rooms { get; private set; }

    public BSPDungeonGenerator(int seed)
    {
        _rng = new System.Random(seed);
        Rooms = new List<RectInt>();
    }

    public Grid2D<TileType> Generate(int width, int height, int minRoomSize = 6)
    {
        var grid = new Grid2D<TileType>(width, height, TileType.Empty);
        Rooms.Clear();
        Split(new RectInt(1, 1, width - 2, height - 2), minRoomSize, Rooms);

        foreach (var room in Rooms)
            CarveRoom(grid, room);

        for (int i = 0; i < Rooms.Count - 1; i++)
        {
            Vector2Int centerA = new Vector2Int(Mathf.RoundToInt(Rooms[i].center.x), Mathf.RoundToInt(Rooms[i].center.y));
            Vector2Int centerB = new Vector2Int(Mathf.RoundToInt(Rooms[i + 1].center.x), Mathf.RoundToInt(Rooms[i + 1].center.y));
            CarveCorridor(grid, centerA, centerB);
        }

        // Convert anything that isn't floor to wall
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid.Get(x, y) != TileType.Floor)
                {
                    grid.Set(x, y, TileType.Wall);
                }
            }
        }

        return grid;
    }

    private void Split(RectInt area, int minSize, List<RectInt> rooms)
    {
        bool splitH = _rng.NextDouble() > 0.5;

        if (area.width < minSize * 2 && area.height < minSize * 2)
        { 
            rooms.Add(Shrink(area)); 
            return; 
        }

        if (splitH && area.height >= minSize * 2)
        {
            int split = _rng.Next(minSize, area.height - minSize);
            Split(new RectInt(area.x, area.y, area.width, split), minSize, rooms);
            Split(new RectInt(area.x, area.y + split, area.width, area.height - split), minSize, rooms);
        }
        else if (!splitH && area.width >= minSize * 2)
        {
            int split = _rng.Next(minSize, area.width - minSize);
            Split(new RectInt(area.x, area.y, split, area.height), minSize, rooms);
            Split(new RectInt(area.x + split, area.y, area.width - split, area.height), minSize, rooms);
        }
        else
        {
            rooms.Add(Shrink(area)); 
        }
    }

    private RectInt Shrink(RectInt r) =>
        new RectInt(r.x + 1, r.y + 1, Mathf.Max(2, r.width - 2), Mathf.Max(2, r.height - 2));

    private void CarveRoom(Grid2D<TileType> g, RectInt r)
    {
        for (int x = r.x; x < r.x + r.width; x++)
            for (int y = r.y; y < r.y + r.height; y++)
                g.Set(x, y, TileType.Floor);
    }

    private void CarveCorridor(Grid2D<TileType> g, Vector2Int a, Vector2Int b)
    {
        int x = a.x;
        while (x != b.x) 
        { 
            g.Set(x, a.y, TileType.Floor); 
            // Make corridor 2 wide
            if (g.InBounds(x, a.y + 1)) g.Set(x, a.y + 1, TileType.Floor);
            x += x < b.x ? 1 : -1; 
        }
        int y = a.y;
        while (y != b.y) 
        { 
            g.Set(b.x, y, TileType.Floor); 
            // Make corridor 2 wide
            if (g.InBounds(b.x + 1, y)) g.Set(b.x + 1, y, TileType.Floor);
            y += y < b.y ? 1 : -1; 
        }
    }
}
