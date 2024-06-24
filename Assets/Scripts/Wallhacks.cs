using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallhacks : MonoBehaviour
{
    public ClientBehaviour client;
    public Vector3 wallhackTargetPosition;

    void Update()
    {
        wallhackTargetPosition = client._targetPosition;
    }
}
