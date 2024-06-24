using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Player1 : MonoBehaviour
{
    public float movementSpeed = 5.0f; // Speed at which the player moves

    public Vector3 targetPosition;
    public ClientBehaviour client;
    public float serverPingSeconds;
    public Tilemap tilemap;

    [SerializeField]
    SpriteRenderer gunSprite;

    public Wallhacks wallhacks;
    public bool canSeeP2 = false;
    public bool wallhacksOn = false;

    void Update()
    {
        client.player1Position = this.transform.position;

        if (client == null)
        {
            Debug.LogError("ClientBehaviour reference not set in Player1 script.");
            return;
        }

        // Handle movement
        HandleMovement();

        //client.Ping();
        client.RequestTargetPosition();
        targetPosition = client.GetTargetPosition();

        if (wallhacksOn)
        {
            targetPosition = client._targetPosition;
            Vector3 targetDirection = targetPosition - transform.position;
            float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle - 90));
            canSeeP2 = client.visibilityTracker.CanSeeEachOtherPlusAdjacency(transform.position, targetPosition);
        }
        else
        {
            if (client.hasTarget)
            {
                Vector3 targetDirection = targetPosition - transform.position;
                float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, angle - 90));
                canSeeP2 = true;
            }
            else
            {
                canSeeP2 = false;
            }
        }
        gunSprite.enabled = canSeeP2;
    }

    void HandleMovement()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, moveVertical, 0.0f);
        Vector3 newPosition = transform.position + movement * movementSpeed * Time.deltaTime;
        Vector3Int gridPosition = tilemap.WorldToCell(newPosition);

        // Check if the new position is within the bounds and not a wall
        if (tilemap.cellBounds.Contains(gridPosition) && !IsWall(gridPosition))
        {
            transform.position = newPosition;
        }
        else
        {
            // attempt pure vertical/horizontal
            movement = new Vector3(moveHorizontal, 0.0f, 0.0f);
            newPosition = transform.position + movement * movementSpeed * Time.deltaTime;
            gridPosition = tilemap.WorldToCell(newPosition);
            if (tilemap.cellBounds.Contains(gridPosition) && !IsWall(gridPosition))
            {
                transform.position = newPosition;
            }
            else
            {
                movement = new Vector3(0.0f, moveVertical, 0.0f);
                newPosition = transform.position + movement * movementSpeed * Time.deltaTime;
                gridPosition = tilemap.WorldToCell(newPosition);
                if (tilemap.cellBounds.Contains(gridPosition) && !IsWall(gridPosition))
                {
                    transform.position = newPosition;
                }
            }
        }
    }
    bool IsWall(Vector3Int gridPosition)
    {
        Vector3Int wallCheck = new Vector3Int(gridPosition.x, gridPosition.y, 1);
        return tilemap.GetTile(wallCheck) != null;
    }
}