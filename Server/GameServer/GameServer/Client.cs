using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    class Client
    {
        public static int dataBufferSize = 4096;
        public int id;
        public TCP tcp;

        public Client(int _clientId)
        {
            id = _clientId;
            tcp = new TCP(id);
        }

        public class TCP
        {
            public TcpClient socket;
            private readonly int id;

            private NetworkStream stream;
            private byte[] recieveBuffer;


            public TCP(int _id)
            {
                id = _id;
            }

            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                recieveBuffer = new byte[dataBufferSize];

                stream.BeginRead(recieveBuffer, 0, dataBufferSize, RecieveCallback, null);

                //TODO: send welcome packet

            }

            private void RecieveCallback(IAsyncResult _result)
            {
                //try catch to prevent server crashing
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
                    stream.BeginRead(recieveBuffer, 0, dataBufferSize, RecieveCallback, null);
                }
                catch(Exception _ex)
                {
                    Console.WriteLine($"Error recieving TCP data: {_ex}");
                    //TODO: disconnect
                }
            }


        }

    }
}
