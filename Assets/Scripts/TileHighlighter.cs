using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileHighlighter : MonoBehaviour
{
    public Tilemap tilemap;
    public Tilemap highlightTilemap;
    public Dictionary<Vector3Int, List<Vector3Int>> visibilityMap;
    public string filePath = "./Assets/Resources/visibility_map_optimistic.json";
    private Vector3Int lastHoveredTile;
    private List<Vector3Int> lastVisibleTiles;

    public Player1 player1;
    private Color highlightColor = new Color(0, 0, 0, .3f);

    public bool headlessMode = false;

    void Awake()
    {
        visibilityMap = new Dictionary<Vector3Int, List<Vector3Int>>();
        LoadVisibilityData(filePath);
        lastHoveredTile = Vector3Int.zero;
        lastVisibleTiles = new List<Vector3Int>();
    }

    void Update()
    {
        // Reload the visibility map
        if (Input.GetKeyUp(KeyCode.F3))
        {
            LoadVisibilityData(filePath);
        }

        if (!headlessMode)
        {
            Vector3Int cellPosition = tilemap.WorldToCell(player1.transform.position);
            if (cellPosition != lastHoveredTile)
            {
                SetAllTilesDarker();
                HighlightVisibleTiles(cellPosition);
                lastHoveredTile = cellPosition;
            }
        }
    }

    void SetAllTilesDarker()
    {
        BoundsInt bounds = tilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                highlightTilemap.SetTileFlags(pos, TileFlags.None);
                highlightTilemap.SetColor(pos, highlightColor);
            }
        }
    }

    Vector3Int GetMouseHoverTilePosition()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = tilemap.WorldToCell(mouseWorldPos);
        return cellPosition;
    }

    void HighlightVisibleTiles(Vector3Int hoveredTile)
    {
        lastVisibleTiles.Clear();
        Color visibleColor = new Color(1, 1, 1, 1); // Bright white for visibility

        if (visibilityMap.ContainsKey(hoveredTile))
        {
            List<Vector3Int> visibleTiles = visibilityMap[hoveredTile];
            foreach (var tilePos in visibleTiles)
            {
                highlightTilemap.SetTileFlags(tilePos, TileFlags.None);
                highlightTilemap.SetColor(tilePos, visibleColor);
                lastVisibleTiles.Add(tilePos);
            }
            highlightTilemap.SetColor(hoveredTile, visibleColor);
        }
    }

    void ResetLastHighlightedTiles()
    {
        foreach (var tilePos in lastVisibleTiles)
        {
            highlightTilemap.SetTileFlags(tilePos, TileFlags.None);
            highlightTilemap.SetColor(tilePos, Color.white); // Reset to original color
        }
    }

    void LoadVisibilityData(string filePath)
    {
        var data = VisibleSets.LoadVisibilityData(filePath);
        foreach (var tile in data.tiles)
        {
            visibilityMap[tile.position] = tile.visibleTiles;
        }
    }
}