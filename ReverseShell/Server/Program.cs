using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Server
{
    public class AsyncObject
    {
        public byte[] Buffer;
        public Socket WorkingSocket;
        public readonly int BufferSize;
        public AsyncObject(int bufferSize)
        {
            BufferSize = bufferSize;
            Buffer = new byte[BufferSize];
        }

        public void ClearBuffer()
        {
            Array.Clear(Buffer, 0, BufferSize);
        }
    }
    internal class Program
    {
        static Socket mainSock;
        static IPAddress thisAddress;

        static void Start_Server(int _port)
        {
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, _port);
            mainSock.Bind(serverEP);
            mainSock.Listen(10);

            mainSock.BeginAccept(AcceptCallback, null);
        }

        static List<Socket> connectedClients = new List<Socket>();
        static void AcceptCallback(IAsyncResult ar)
        {
            Socket client = mainSock.EndAccept(ar);

            mainSock.BeginAccept(AcceptCallback, null);

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = client;

            connectedClients.Add(client);

            Console.WriteLine("Client Connected.");

            client.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        static void DataReceived(IAsyncResult ar)
        {
            AsyncObject obj = (AsyncObject)ar.AsyncState;
            int received;
            try
            {
                received = obj.WorkingSocket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnected From Client.");
                return;
            }

            if (received <= 0)
            {
                obj.WorkingSocket.Close();
                return;
            }

            string text = Encoding.UTF8.GetString(obj.Buffer);
            string msg = text;
            msg = msg.Trim('\0');
            Console.WriteLine(msg);

            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                if (socket != obj.WorkingSocket)
                {
                    try { socket.Send(obj.Buffer); }
                    catch
                    {
                        try { socket.Dispose(); } catch { }
                        connectedClients.RemoveAt(i);
                    }
                }
            }

            obj.ClearBuffer();

            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }

        static void Send_Data(string tts)
        {
            if (!mainSock.IsBound)
            {
                Console.WriteLine("서버가 실행되고 있지 않습니다!");
                return;
            }
            if (string.IsNullOrEmpty(tts))
            {
                return;
            }
            byte[] bDts = Encoding.UTF8.GetBytes(tts);

            for (int i = connectedClients.Count - 1; i >= 0; i--)
            {
                Socket socket = connectedClients[i];
                try { socket.Send(bDts); }
                catch
                {
                    try { socket.Dispose(); } catch { }
                    connectedClients.RemoveAt(i);
                }
            }
        }

        static void Main(string[] args)
        {
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            Start_Server(15000);
            while(true)
            {
                string s = Console.ReadLine();
                Send_Data(s);
            }
        }
    }
}
