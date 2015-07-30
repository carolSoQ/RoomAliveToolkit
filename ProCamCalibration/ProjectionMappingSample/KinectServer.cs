using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace RoomAliveToolkit
{

    public class KinectServer
    {
        private TcpListener server;
        private Thread serverThread;

        public void Start(string address, int port)
        {
            this.server = new TcpListener(IPAddress.Any, port);
            this.server.Start();
            this.serverThread = new Thread(new ThreadStart(this.ServerWorkerThread));
            this.serverThread.Start();
            System.Diagnostics.Debug.WriteLine("Server running...");
        }

        private void ServerWorkerThread()
        {
            while (true)
            {
                TcpClient client = this.server.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(this.ClientWorkerThread));
                clientThread.Start(client);
            }
        }

        private void ClientWorkerThread(object clientObj)
        {
            TcpClient client = clientObj as TcpClient;
            NetworkStream clientStream = client.GetStream();

            while (true)
            {
                string data = "";
                string bodyFrameJSON = "";
                try
                {
                    if (!client.Connected) break;

                    while (!clientStream.DataAvailable) ;

                    Byte[] bytes = new Byte[1024000];
                    clientStream.Read(bytes, 0, 1024000);
                    data = Encoding.ASCII.GetString(bytes);

                    if (data.StartsWith("Bodyframe"))
                    {
                        string bodyFrameDecoded = HttpUtility.UrlDecode(data);
                        bodyFrameJSON = bodyFrameDecoded.Substring("Bodyframe=".Length);
                        JObject bodyframe = JObject.Parse(bodyFrameJSON);
                        System.Diagnostics.Debug.WriteLine((string)bodyframe["Timestamp"]);
                        //bodyframe["Bodies"][0]["Joints"]["SpineBase"][]

                        
                        //clientStream.Write()
                        //var response = new Dictionary<string, string>();
                        //response.Add("message", "OK");
                        //string responseJSON = JsonConvert.SerializeObject(response);
                        //byte[] msg = Encoding.ASCII.GetBytes(responseJSON);
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(bodyFrameJSON);
                    System.Diagnostics.Debug.WriteLine(e.StackTrace);
                    clientStream.Close();
                    client.Close();
                }
            }
            string response = "HTTP/1.1 200 OK\nConent-Length: 0\n\r\n";
            byte[] msg = Encoding.ASCII.GetBytes(response);
            clientStream.Write(msg, 0, msg.Length);
            clientStream.Flush();
            clientStream.Close();
            client.Close();
        }
    }
}
