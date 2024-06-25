using UnityEngine;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using System; //BitConverter
using System.Collections.Generic; //List
using System.Net;


public class ServerBehaviour : MonoBehaviour
{
    public Vector3 player1Position;
    public Player2 player2;
    public ushort port = 9000;
    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    private Queue<(Vector3 position, float timestamp, uint responseCode)> positionQueue = new Queue<(Vector3, float, uint)>();
    private float latency;
    public VisibilityTracker visibilityTracker;

    void Start()
    {
        Debug.Log("Starting up server...");
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = port;

        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind server driver to port" + endpoint.Port);
        else
        {
            m_Driver.Listen();
            Debug.Log("Server started, listening on IP: " + GetLocalIPAddress() + " and port: " + port);
        }

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }
    string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "Local IP Address Not Found";
    }
    public void OnDestroy()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }

    void Update()
    {
        visibilityTracker.player2Position = player2.transform.position;
        visibilityTracker.player1Position = player1Position;
        m_Driver.ScheduleUpdate().Complete();

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection"); 
            player2.ResetPositionToTopOfCycle();
        }

        while (positionQueue.Count > 0)
        {
            (Vector3 position, float timestamp, uint responseCode) = positionQueue.Dequeue();
            SendPositionToAllClients(position, responseCode);
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;

            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    uint requestCode = stream.ReadUInt();
                    if (requestCode == 0) // Target position
                    {
                        bool isVisible = visibilityTracker.playersVisible;
                        Vector3 position = isVisible ? player2.transform.position : new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        QueuePosition(position, isVisible);
                    }
                    else if (requestCode == 2) //Player1 position
                    {
                        player1Position.x = stream.ReadFloat();
                        player1Position.y = stream.ReadFloat();
                        player1Position.z = stream.ReadFloat();
                    }
                    else if (requestCode == 99) //Ping
                    {
                        float clientTime = stream.ReadFloat();
                        float serverTime = Time.time;
                        latency = serverTime - clientTime;
                        Pong(latency);
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }
    }

    public void QueuePosition(Vector3 position, bool isVisible)
    {
        uint responseCode = isVisible ? (byte)0 : (byte)1;
        positionQueue.Enqueue((position, Time.realtimeSinceStartup, responseCode));
    }

    private void SendPositionToAllClients(Vector3 position, uint responseCode)
    {
        List<byte> message = new List<byte>();
        message.AddRange(BitConverter.GetBytes(responseCode));  // Add response code to the message
        message.AddRange(BitConverter.GetBytes(position.x));
        message.AddRange(BitConverter.GetBytes(position.y));
        message.AddRange(BitConverter.GetBytes(position.z));

        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;

            m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i], out var writer);
            foreach (byte b in message)
            {
                writer.WriteByte(b);
            }
            m_Driver.EndSend(writer);
        }
    }

    private void Pong(float latency)
    {
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;
            m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i], out var writer);
            writer.WriteUInt(99);
            writer.WriteFloat(latency);
            m_Driver.EndSend(writer);
        }
    }
}
