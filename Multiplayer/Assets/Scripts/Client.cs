using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine.UIElements;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;

    private delegate void PacketHandler(Packet packet);
    private static Dictionary<int, PacketHandler> packetHandlers;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }

    }

    private void Start()
    {
        tcp = new TCP();
    }

    public void ConnectToServer()
    {
        InitialiseClientData();

        tcp.Connect();
    }


    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet recievedData;
        private byte[] recieveBuffer;
        

        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            recieveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult _result)
        {
            socket.EndConnect(_result);

            if(!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            recievedData = new Packet();

            stream.BeginRead(recieveBuffer, 0, dataBufferSize, RecieveCallback, null);

        }

        public void SendData(Packet _packet)
        {
            try 
            {
                if(socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch(Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        private void RecieveCallback(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    //TODO: disconnect
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(recieveBuffer, _data, _byteLength);

                //TODO: handle data
                recievedData.Reset(HandleData(_data));
                stream.BeginRead(recieveBuffer, 0, dataBufferSize, RecieveCallback, null);
            }
            catch
            {
                //TODO: Disconnect
            }
        }

        private bool HandleData(byte[] _data) 
        {
            int _packetLength = 0;

            recievedData.SetBytes(_data);

            if(recievedData.UnreadLength() >= 4)
            {
                _packetLength = recievedData.ReadInt();
                if(_packetLength <= 0)
                {
                    return true;
                }
            }


            while(_packetLength > 0 && _packetLength <= recievedData.UnreadLength())
            {
                byte[] _packetBytes = recievedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
                    }
                });

                _packetLength = 0;

                if (recievedData.UnreadLength() >= 4)
                {
                    _packetLength = recievedData.ReadInt();
                    if (_packetLength >= 0)
                    {
                        return true;
                    }
                }
            }

            if(_packetLength <= 1)
            {
                return true;
            }

            return false;
        }
    }

    private void InitialiseClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ServerPackets.welcome, ClientHandle.Welcome }
            };
        Debug.Log("Initialized packets.");
    }
}
//tcp stream based protocall, garunteed to arrive, not in one piece