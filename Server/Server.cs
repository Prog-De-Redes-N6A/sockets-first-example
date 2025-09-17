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
                    byte[] fileNameLengthBuffer = networkDataHelper.Receive(Protocol.FileNameLengthSize);
                    int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer);

                    byte[] fileNameBytes = networkDataHelper.Receive(fileNameLength);
                    string fileName = Encoding.UTF8.GetString(fileNameBytes);

                    byte[] fileSizeBuffer = networkDataHelper.Receive(Protocol.FileLengthSize);
                    long fileSize = BitConverter.ToInt64(fileSizeBuffer);

                    long offset = 0; // bytes received
                    long partCount = Protocol.CalculateFileParts(fileSize);
                    long currentPart = 1;

                    FileStreamHelper fsh = new FileStreamHelper();

                    while (offset < fileSize)
                    {
                        byte[] buffer;
                        bool isLastPart = (currentPart == partCount);

                        if (!isLastPart)
                        {
                            Console.WriteLine($"Receiving segment #{currentPart} of size {Protocol.MaxFilePartSize}");
                            buffer = networkDataHelper.Receive(Protocol.MaxFilePartSize);
                            offset += Protocol.MaxFilePartSize;
                        }
                        else
                        {
                            long lastPartSize = fileSize - offset;
                            Console.WriteLine($"Receiving segment #{currentPart} of size {lastPartSize}");
                            buffer = networkDataHelper.Receive((int)lastPartSize);
                            offset += lastPartSize;
                        }
                        fsh.Write(fileName, buffer);
                        currentPart++;
                    }

                    Console.WriteLine($"Received message");
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
