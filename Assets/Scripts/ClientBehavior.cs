using UnityEngine;
using UnityEngine.Assertions;
using System; //BitConverter
using System.Collections.Generic; //List
using Unity.Collections;
using Unity.Networking.Transport;

public class ClientBehaviour : MonoBehaviour
{
    public string ipAddress = "144.126.223.189"; // digitalocean droplet server ip
    public ushort port = 9000;
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool done;
    public float rttToServerSeconds; 
    public bool connectingToHeadlessServer = true;

    public float simulatedLagMs = 0f; // Lag in milliseconds

    public bool hasTarget = false;
    private Queue<(Vector3 position, float timestamp)> positionQueue = new Queue<(Vector3 position, float timestamp)>();
    public Vector3 _targetPosition;
    public Vector3 player1Position;

    [SerializeField]
    Player2 player2Representative;

    [SerializeField]
    GameObject vtObject;
    public VisibilityTracker visibilityTracker;

    void Start()
    {
        visibilityTracker = vtObject.GetComponent<VisibilityTracker>();
        m_Driver = NetworkDriver.Create();
        if (connectingToHeadlessServer)
        {
            var endpoint = NetworkEndpoint.Parse(ipAddress, port);
            m_Connection = m_Driver.Connect(endpoint);
            if (!m_Connection.IsCreated)
            {
                Debug.LogError("Failed to create connection.");
            }
            else
            {
                Debug.Log("Connection initiated with headless server at" + ipAddress + ":" + port);
            }
        }
        else //local server
        {
            var endpoint = NetworkEndpoint.LoopbackIpv4;
            endpoint.Port = port;
            m_Connection = m_Driver.Connect(endpoint);
            if (!m_Connection.IsCreated)
            {
                Debug.LogError("Failed to create connection.");
            }
            else
            {
                Debug.Log("Connection initiated with local server, port: " + port);
            }
        }
        player2Representative.ResetPositionToTopOfCycle();
    }

    void OnDestroy()
    {
        m_Driver.Dispose();
    }

    void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            if (!done)
                Debug.Log("Something went wrong during connect");
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("Player1 connected to the server");

                uint value = 1;
                m_Driver.BeginSend(m_Connection, out var writer);
                writer.WriteUInt(value);
                m_Driver.EndSend(writer);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                uint responseCode = stream.ReadUInt();
                if (responseCode == 0) // Getting target position
                {
                    float x = stream.ReadFloat();
                    float y = stream.ReadFloat();
                    float z = stream.ReadFloat();
                    _targetPosition = new Vector3(x, y, z);

                    // ONLY if directly visible (no adjacency), send position to player
                    if (visibilityTracker.CanSeeEachOther(player1Position, _targetPosition))
                    {
                        positionQueue.Enqueue((_targetPosition, Time.realtimeSinceStartup));
                        hasTarget = true;
                    }
                    else
                    {
                        hasTarget = false;
                    }
                }
                else if (responseCode == 1)
                {
                    hasTarget = false;
                }
                /*else if (responseCode == 99) // Getting ping
                {
                    rttToServerSeconds = stream.ReadFloat();
                }*/
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Player1 got disconnected from server");
                m_Connection = default(NetworkConnection);
            }
        }
        SimulateLagOnPlayerPosition(); 
        SendPlayerPosition();
    }

    // Process positions in the queue, applying the lag
    private void SimulateLagOnPlayerPosition()
    {
        float lagInSeconds = simulatedLagMs / 1000.0f;

        while (positionQueue.Count > 0 && (Time.realtimeSinceStartup - positionQueue.Peek().timestamp) >= lagInSeconds)
        {
            var (position, _) = positionQueue.Dequeue();
            _targetPosition = position;
        }
    }
    public void RequestTargetPosition()
    {
        if (!m_Connection.IsCreated)
        {
            Debug.LogError("Connection not established or already disconnected.");
            return;
        }

        var writerResult = m_Driver.BeginSend(m_Connection, out var writer);
        if (writerResult != 0)
        {
            Debug.LogError($"Failed to send target position request");
            return;
        }

        writer.WriteUInt(0);
        m_Driver.EndSend(writer);
    }
    public void Ping()
    {
        if (!m_Connection.IsCreated)
        {
            Debug.LogError("Connection not established or already disconnected.");
            return;
        }

        var writerResult = m_Driver.BeginSend(m_Connection, out var writer);
        if (writerResult != 0)
        {
            Debug.LogError("Failed to send player ping");
            return;
        }

        writer.WriteUInt(99);
        writer.WriteFloat(Time.time);
        m_Driver.EndSend(writer);
    }

    public void SetSimulatedLag(float milliseconds)
    {
        simulatedLagMs = milliseconds;
    }

    public Vector3 GetTargetPosition()
    {
        return _targetPosition;
    }

    public void SendPlayerPosition()
    {
        if (!m_Connection.IsCreated)
        {
            Debug.LogError("Connection not established or already disconnected.");
            return;
        }

        Vector3 position = player1Position; // Ensure player1Position is updated in Update() from Player1's transform

        var writerResult = m_Driver.BeginSend(m_Connection, out var writer);
        if (writerResult != 0)
        {
            Debug.LogError("Failed to send player position");
            return;
        }

        // Define a unique code for sending position, here using '2' arbitrarily
        writer.WriteUInt(2);
        writer.WriteFloat(player1Position.x);
        writer.WriteFloat(player1Position.y);
        writer.WriteFloat(player1Position.z);

        m_Driver.EndSend(writer);
    }
}