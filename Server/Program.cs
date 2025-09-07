using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Server
{
    internal class Program
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

            Socket clientSocket = serverSocket.Accept(); // Blocking

            byte[] buffer = new byte[8];
            // Buffer is empty
            clientSocket.Receive(buffer); // Blocking
            // Buffer has the message
            string message = Encoding.ASCII.GetString(buffer);
            Console.WriteLine($"Client sent: {message}");

            Console.ReadLine();
        }
    }
}
