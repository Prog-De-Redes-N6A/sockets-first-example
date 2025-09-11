using System.Net.Sockets;
using System.Net;
using System.Text;

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
            while (true) // Mala practica
            {
                byte[] messageLengthBuffer = new byte[2];
                clientSocket.Receive(messageLengthBuffer);
                ushort messageLength = BitConverter.ToUInt16(messageLengthBuffer);

                byte[] buffer = new byte[messageLength];
                clientSocket.Receive(buffer);
                string message = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"Client sent: {message}");
            }
        }
    }
}
