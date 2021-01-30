using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Net.NetworkInformation;
using System.Linq;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;

public class Networker : SerializedMonoBehaviour
{
    [SerializeField] private TextAsset ipAsset;
    [SerializeField, ReadOnly] private string ip;
    [SerializeField, ReadOnly] private int port;
    [SerializeField] private List<NetworkInterface> networkInterfaces = new List<NetworkInterface>();

    private List<NetworkedObject> allNetworkObjects = new List<NetworkedObject>();

    TcpClient client;
    NetworkStream stream;
    ConcurrentQueue<byte[]> writeQueue = new ConcurrentQueue<byte[]>();
    byte[] readBuffer;
    ConcurrentQueue<byte[]> readQueue = new ConcurrentQueue<byte[]>();
    List<byte> readMessageFragment = new List<byte>();
    bool sending = false;

    private void Awake() {
        DontDestroyOnLoad(gameObject);
        string[] addr = ipAsset.text.Split('\n');
        ip = addr[0].Trim();
        port = int.Parse(addr[1]);
        TryConnect();
    }

    private void Update() {
        foreach(NetworkedObject no in allNetworkObjects)
        {
            foreach(KeyValuePair<GameObject, List<NetworkedComponent>> kvp in no.objectsToControl)
            {
                foreach(NetworkedComponent comp in kvp.Value)
                {
                    if(!comp.snapshotHasChanged)
                        continue;
                    
                    byte[] data = comp.lastSnapshot;
                    comp.MarkSent();
                    byte[] messageWithHeader = GetMessage(comp.component, ref data);
                    SendMessage(ref messageWithHeader);
                }
            }
        }
    }

    public void RegisterNewNetworkObject(NetworkedObject networkObj)
    {
        if(!allNetworkObjects.Contains(networkObj))
            allNetworkObjects.Add(networkObj);
    }

    [Button]
    public void GetNetworkInterfaces()
    {
        networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().ToList();
    }

    public void Disconnect()
    {
        stream.Close();
        client.Close();
        Debug.Log("Sucessfully Disconnected.");
        // Load connection scene, destory the networker.
    }

    private void TryConnect()
    {
        Debug.Log($"Attempting to connect to {ip}:{port}.");
        try {
            client.BeginConnect(IPAddress.Parse(ip), port, new AsyncCallback(ConnectCallback), this);
        } catch (Exception e) {
            Debug.LogWarning($"Failed to connect to {ip}:{port} with error:\n{e}");
        }
    }

    private void ConnectCallback(IAsyncResult ar)
    {
        Networker networker = (Networker)ar.AsyncState;
        if(networker == null)
            return;

        try {
            networker.client.EndConnect(ar);
            networker.stream = networker.client.GetStream();
            networker.readBuffer = new byte[1024];
            networker.stream.BeginRead(networker.readBuffer, 0, 1024, new AsyncCallback(RecieveMessage), networker);
        } catch (Exception e) {
            Debug.LogWarning($"Failed to connect with error:\n{e}");
        }
        Debug.Log("Sucessfully connected.");
        // Load scene
    }

    private void RecieveMessage(IAsyncResult ar)
    {
        Networker networker = (Networker)ar.AsyncState;
        if(networker == null)
            return;

        try {
            int bytesRead = networker.stream.EndRead(ar);
            if(bytesRead == 0)
            {
                networker.stream.BeginRead(networker.readBuffer, 0, 1024, new AsyncCallback(networker.RecieveMessage), networker);
                return;
            }
            byte[] readBytes = new byte[bytesRead];
            Buffer.BlockCopy(networker.readBuffer, 0, readBytes, 0, bytesRead);
            networker.readQueue.Enqueue(readBytes);
            networker.CheckCompleteMessage();
        } catch(Exception e) {
            Debug.LogWarning($"Failed to read from socket:\n{e}");
        }   
    }

    private void CheckCompleteMessage()
    {
        // This queue may have more entries if it's being added to faster than ready from. This may become an issue later.
        if(readQueue.TryDequeue(out byte[] result))
        {
            readMessageFragment.AddRange(result);
            if(readMessageFragment.Count >= 22)
            {
                byte[] msgLength = new byte[2];
                Buffer.BlockCopy(readMessageFragment.ToArray(), 20, msgLength, 0, 2);
                ushort length = BitConverter.ToUInt16(msgLength, 0);
                if(readMessageFragment.Count >= 26 + length)
                {
                    byte[] completeMessage = new byte[26 + length];
                    Buffer.BlockCopy(readMessageFragment.ToArray(), 0, completeMessage, 0, 26 + length);
                    readMessageFragment.RemoveRange(0, 26 + length);
                    Dispatch(ref completeMessage);
                }
            }
        }
    }

    public void Dispatch(ref byte[] responseData)
    {
        byte[] header = new byte[26];
        Buffer.BlockCopy(responseData, 0, header, 0, 26);
        
        byte[] typeHash = new byte[20];
        Buffer.BlockCopy(header, 0, typeHash, 0, 20);
        Type type = TypeHash.GetType(typeHash);

        byte[] length = new byte[2];
        Buffer.BlockCopy(header, 20, length, 0, 2);
        ushort msgLength = BitConverter.ToUInt16(length, 0);

        byte[] id = new byte[4];
        Buffer.BlockCopy(header, 22, id, 0, 4);

        byte[] data = new byte[msgLength];
        Buffer.BlockCopy(responseData, 26, data, 0, msgLength);

        if(type == typeof(Transform))
            data.Deserialize<Transform>(gameObject);
    }

    private void SendMessage(ref byte[] messageWithHeader)
    {
        if(sending)
            writeQueue.Enqueue(messageWithHeader);
        else
        {
            sending = true;
            try {
                stream.BeginWrite(messageWithHeader, 0, messageWithHeader.Length, new AsyncCallback(SendMessageCallback), this);
            } catch (Exception e) {
                Debug.LogWarning($"Failed to write to socket.\n{e}");
            }
        }
    }

    private void SendMessageCallback(IAsyncResult ar)
    {
        Networker networker = (Networker)ar.AsyncState;
        if(networker == null)
            return;
        
        try {
            networker.stream.EndWrite(ar);
            Debug.Log("Message Sent");
            
            networker.sending = networker.writeQueue.Count switch {
                0 => false,
                _ => ContinueWriteIfNeeded(networker)
            };
        } catch (Exception e) {
            Debug.LogWarning($"Failed to write to socket:\n{e}");
        }
    }

    public bool ContinueWriteIfNeeded(Networker networker)
    {
        if(networker.writeQueue.TryDequeue(out byte[] writeBuffer))
        {
            networker.stream.BeginWrite(writeBuffer, 0, writeBuffer.GetLength(0), new AsyncCallback(SendMessageCallback), networker);
            return true;
        }
        return false;
    }

    public byte[] GetMessage(Component component, ref byte[] data)
    {
        byte[] msg = new byte[26 + data.Length];
        // Get the type of the message
        Buffer.BlockCopy(TypeHash.GetHash(component.GetType()), 0, msg, 0, 20);
        // Get the length of the message
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)data.Length), 0, msg, 20, 2);
        // Get ID from object, which is spawned from message from server
        
        // Add data after header
        Buffer.BlockCopy(data, 0, msg, 26, data.Length);
        return msg;
    }
}