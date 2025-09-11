using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Cliente
{
    internal class Client
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client Application..");

            Socket clientSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            IPEndPoint localEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            clientSocket.Bind(localEndpoint);

            IPEndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            // We connect to the server
            clientSocket.Connect(serverEndpoint); // Blocking
            
            Console.WriteLine("Connected to server!!");

            bool clientRunning = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);

            while (clientRunning)
            {
                Console.WriteLine("Type a message for the server:");
                string message = Console.ReadLine();
                if (message == "exit")
                {
                    clientRunning = false;
                    break;
                }
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                ushort messageLength = (ushort)messageBytes.Length;
                byte[] messageLengthBytes = BitConverter.GetBytes(messageLength);

                try
                {
                    networkDataHelper.Send(messageLengthBytes);

                    networkDataHelper.Send(messageBytes);

                    Console.WriteLine("Sent message...");
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Connection interrupted");
                    clientRunning = false;
                }
            }

            Console.WriteLine("Closing connection...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }
}
