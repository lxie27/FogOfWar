using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Tilemaps;

public class Tile
{
    public Vector3Int position;
    public Vector3[] corners;
    public Vector3 center;
    public bool isWall;

    public Tile(Vector3Int pos, bool wall, Grid grid)
    {
        position = pos;
        isWall = wall;
        corners = new Vector3[4];
        Vector3 worldPosition = grid.CellToWorld(pos);
        float cellSize = grid.cellSize.x; // Assuming square cells for simplicity

        corners[0] = worldPosition + new Vector3(-cellSize / 2, -cellSize / 2, 0);
        corners[1] = worldPosition + new Vector3(cellSize / 2, -cellSize / 2, 0);
        corners[2] = worldPosition + new Vector3(cellSize / 2, cellSize / 2, 0);
        corners[3] = worldPosition + new Vector3(-cellSize / 2, cellSize / 2, 0);
        center = worldPosition + new Vector3(0, 0, 0);
    }
}

public class VisibleSets : MonoBehaviour
{
    public Grid grid;
    public Tilemap tilemap;
    private string filepath = "./Assets/Resources/visibility_map.json";

    void Update()
    {
        // Create a new visibility map
        if (Input.GetKeyUp(KeyCode.F1))
        {
            var tiles = CollectTiles(tilemap, grid);
            var vismap = CalculateVisibilityMap(tiles);
            SaveVisibilityData(vismap, filepath);
            Debug.Log("Created visibility map at " + filepath);
        }
        // Create a new optimistic visibility map
        if (Input.GetKeyUp(KeyCode.F2))
        {
            var tiles = CollectTiles(tilemap, grid);
            var vismap = CalculateOptimisticVisibilityMap(tiles);
            SaveVisibilityData(vismap, filepath.Replace(".json", "_optimistic.json"));
            Debug.Log("Created optimistic visibility map at " + filepath.Replace(".json", "_optimistic.json"));
        }
    }

    public List<Tile> CollectTiles(Tilemap tilemap, Grid grid)
    {
        List<Tile> tiles = new List<Tile>();

        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                Vector3Int wallCheck = new Vector3Int(x, y, 1);
                if (tilemap.HasTile(pos))
                {
                    bool isWall = tilemap.GetTile(wallCheck) != null; // Ensure wallCheck is not null
                    Tile tile = new Tile(pos, isWall, grid);
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    public Dictionary<Tile, HashSet<Tile>> CalculateVisibilityMap(List<Tile> tiles)
    {
        Dictionary<Tile, HashSet<Tile>> visibilityMap = new Dictionary<Tile, HashSet<Tile>>();

        foreach (var tile in tiles)
        {
            if (!tile.isWall)
            {
                visibilityMap[tile] = new HashSet<Tile>();

                foreach (var otherTile in tiles)
                {
                    if (tile != otherTile && !otherTile.isWall)
                    {
                        bool visible = true;

                        // Check all corner-center combinations
                        foreach (var corner in tile.corners)
                        {
                            foreach (var otherCorner in otherTile.corners)
                            {
                                if (IsLineBlocked(corner, otherCorner, tiles))
                                {
                                    visible = false;
                                    break;
                                }
                            }
                            if (IsLineBlocked(corner, otherTile.center, tiles) || IsLineBlocked(tile.center, otherTile.center, tiles))
                            {
                                visible = false;
                                break;
                            }
                        }

                        if (visible)
                        {
                            visibilityMap[tile].Add(otherTile);
                        }
                    }
                }
            }
        }

        return visibilityMap;
    }

    public Dictionary<Tile, HashSet<Tile>> CalculateOptimisticVisibilityMap(List<Tile> tiles)
    {
        Dictionary<Tile, HashSet<Tile>> visibilityMap = new Dictionary<Tile, HashSet<Tile>>();

        foreach (var tile in tiles)
        {
            if (!tile.isWall)
            {
                visibilityMap[tile] = new HashSet<Tile>();

                foreach (var otherTile in tiles)
                {
                    if (tile != otherTile && !otherTile.isWall)
                    {
                        bool visible = false;

                        // Check all corner-center and corner-corner combinations
                        foreach (var corner in tile.corners)
                        {
                            foreach (var otherCorner in otherTile.corners)
                            {
                                if (!IsLineBlocked(corner, otherCorner, tiles))
                                {
                                    visible = true;
                                    break;
                                }
                            }
                            if (!IsLineBlocked(corner, otherTile.center, tiles) || !IsLineBlocked(tile.center, otherTile.center, tiles))
                            {
                                visible = true;
                                break;
                            }
                        }

                        if (visible)
                        {
                            visibilityMap[tile].Add(otherTile);
                        }
                    }
                }
            }
        }

        return visibilityMap;
    }

    bool IsLineBlocked(Vector3 start, Vector3 end, List<Tile> tiles)
    {
        foreach (var tile in tiles)
        {
            if (tile.isWall)
            {
                // Check intersection with each edge of the wall tile
                for (int i = 0; i < tile.corners.Length; i++)
                {
                    Vector3 corner1 = tile.corners[i];
                    Vector3 corner2 = tile.corners[(i + 1) % tile.corners.Length];

                    if (DoLinesIntersect(start, end, corner1, corner2))
                    {
                        return true; // Line is blocked
                    }
                }
            }
        }
        return false; // Line is not blocked
    }

    bool DoLinesIntersect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
        if (denominator == 0) return false; // Lines are parallel

        float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
        float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;

        return (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1);
    }

    [System.Serializable]
    public class VisibilityData
    {
        public List<SerializableTile> tiles = new List<SerializableTile>();
    }

    [System.Serializable]
    public class SerializableTile
    {
        public Vector3Int position;
        public List<Vector3Int> visibleTiles;

        public SerializableTile()
        {
            visibleTiles = new List<Vector3Int>();
        }
    }

    void SaveVisibilityData(Dictionary<Tile, HashSet<Tile>> visibilityMap, string filePath)
    {
        VisibilityData data = new VisibilityData();

        foreach (var entry in visibilityMap)
        {
            SerializableTile serializableTile = new SerializableTile();
            serializableTile.position = entry.Key.position;
            foreach (var visibleTile in entry.Value)
            {
                serializableTile.visibleTiles.Add(visibleTile.position);
            }
            data.tiles.Add(serializableTile);
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(filePath, json);
    }

    public static VisibilityData LoadVisibilityData(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<VisibilityData>(json);
        }
        else
        {
            Debug.Log("Could not load visibility map at path: " + filePath);
        }
        return null;
    }
}