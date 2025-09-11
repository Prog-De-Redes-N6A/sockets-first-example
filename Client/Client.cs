using System.Net.Sockets;
using System.Net;
using System.Text;

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

            while (true)
            {
                Console.WriteLine("Type a message for the server:");
                string message = Console.ReadLine();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                ushort messageLength = (ushort)messageBytes.Length;
                byte[] messageLengthBytes = BitConverter.GetBytes(messageLength);

                clientSocket.Send(messageLengthBytes);

                clientSocket.Send(messageBytes);

                Console.WriteLine("Sent message...");
            }
        }
    }
}
