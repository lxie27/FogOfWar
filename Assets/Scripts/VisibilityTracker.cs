using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VisibilityTracker : MonoBehaviour
{
    public TileHighlighter tileHighlighter;
    public Tilemap tilemap;
    public bool playersVisible = false;
    private Dictionary<Vector3Int, List<Vector3Int>> visibilityMap;

    private Vector3Int lastPlayer1CellPosition;
    private Vector3Int lastPlayer2CellPosition;

    public Player1 player1;
    public Vector3 player1Position;
    public Vector3 player2Position;

    void Start()
    {
        visibilityMap = tileHighlighter.visibilityMap;
        lastPlayer1CellPosition = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
        lastPlayer2CellPosition = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
    }

    void Update()
    {
        Vector3Int currentCellPosition1 = tilemap.WorldToCell(player1Position);
        Vector3Int currentCellPosition2 = tilemap.WorldToCell(player2Position);

        if (currentCellPosition1 != lastPlayer1CellPosition || currentCellPosition2 != lastPlayer2CellPosition)
        {
            playersVisible = CanSeeEachOtherPlusAdjacency(player1Position, player2Position);
            lastPlayer1CellPosition = currentCellPosition1;
            lastPlayer2CellPosition = currentCellPosition2;
        }
    }
    public bool CanSeeEachOther(Vector3 positionA, Vector3 positionB)
    {
        Vector3Int cellPositionA = tilemap.WorldToCell(positionA);
        Vector3Int cellPositionB = tilemap.WorldToCell(positionB);

        // Check if A can see B using the visibility map
        return visibilityMap.ContainsKey(cellPositionA) && visibilityMap[cellPositionA].Contains(cellPositionB);
    }

    public bool CanSeeEachOtherPlusAdjacency(Vector3 positionA, Vector3 positionB)
    {
        Vector3Int cellPositionA = tilemap.WorldToCell(positionA);
        Vector3Int cellPositionB = tilemap.WorldToCell(positionB);

        // Direct line of sight check
        bool directVisibility = visibilityMap.ContainsKey(cellPositionA) && visibilityMap[cellPositionA].Contains(cellPositionB);

        // Additional check for adjacency in line of sight
        if (!directVisibility)
        {
            // Get neighbors of A and check if any of these can see B
            List<Vector3Int> neighbors = GetAdjacentCells(cellPositionA);
            foreach (Vector3Int neighbor in neighbors)
            {
                if (visibilityMap.ContainsKey(neighbor) && visibilityMap[neighbor].Contains(cellPositionB))
                {
                    return true;
                }
                // Check if B's neighbors are visible from A or its neighbors
                List<Vector3Int> neighborsB = GetAdjacentCells(cellPositionB);
                foreach (Vector3Int neighborB in neighborsB)
                {
                    if (visibilityMap.ContainsKey(cellPositionA) && visibilityMap[cellPositionA].Contains(neighborB))
                    {
                        return true;
                    }
                }
            }
        }

        return directVisibility;
    }
    private List<Vector3Int> GetAdjacentCells(Vector3Int cell)
    {
        List<Vector3Int> adjacentCells = new List<Vector3Int>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x != 0 || y != 0) // exclude the cell itself
                {
                    adjacentCells.Add(new Vector3Int(cell.x + x, cell.y + y, cell.z));
                }
            }
        }
        return adjacentCells;
    }
}
