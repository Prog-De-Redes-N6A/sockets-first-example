using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Server
{
    internal class Server
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server Application..");

            Socket serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            // Port between 0 and 65535
            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000); 
            
            // If the port isn't free it throws SocketException
            serverSocket.Bind(serverEndpoint);

            serverSocket.Listen(); // We listen for connections

            Console.WriteLine("Waiting for clients to connect...");

            while (true)
            {
                Socket clientSocket = serverSocket.Accept(); // bloqueante
                Thread t = new Thread(() => HandleClient(clientSocket));
                t.Start();
            }
        }

        static void HandleClient(Socket clientSocket)
        {
            bool clientActive = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);
            while (clientActive) // Mala practica
            {
                try
                {
                    byte[] messageLengthBuffer = networkDataHelper.Receive(2);
                    ushort messageLength = BitConverter.ToUInt16(messageLengthBuffer);

                    byte[] buffer = networkDataHelper.Receive(messageLength);

                    string message = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine($"Client sent: {message}");
                }
                catch (SocketException e)
                {
                    clientActive = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Client send badly formatted data");
                    clientActive = false;
                }
            }
            Console.WriteLine("Client disconnected");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}
