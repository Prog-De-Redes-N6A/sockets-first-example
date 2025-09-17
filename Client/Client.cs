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
                try
                {
                    Console.WriteLine("Type the path of file:");
                    string filePath = Console.ReadLine();
                    //if (message == "exit")
                    //{
                    //    clientRunning = false;
                    //    break;
                    //}
                    FileInfo fileInfo = new FileInfo(filePath);
                    string fileName = fileInfo.Name;
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                    int fileNameLength = fileNameBytes.Length;
                    byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameLength);

                    networkDataHelper.Send(fileNameLengthBytes);

                    networkDataHelper.Send(fileNameBytes);

                    long fileSize = fileInfo.Length;
                    byte[] fileSizeBytes = BitConverter.GetBytes(fileSize);
                    networkDataHelper.Send(fileSizeBytes);

                    long offset = 0; // bytes sent
                    long partCount = Protocol.CalculateFileParts(fileSize);
                    long currentPart = 1;

                    FileStreamHelper fsh = new FileStreamHelper();

                    while (offset < fileSize)
                    {
                        byte[] buffer;
                        bool isLastPart = (currentPart == partCount);

                        if (!isLastPart)
                        {
                            Console.WriteLine($"Sending segment #{currentPart} of size {Protocol.MaxFilePartSize}");
                            buffer = fsh.Read(filePath, offset, Protocol.MaxFilePartSize);
                            offset += Protocol.MaxFilePartSize;
                        }
                        else
                        {
                            long lastPartSize = fileSize - offset;
                            Console.WriteLine($"Sending segment #{currentPart} of size {lastPartSize}");
                            buffer = fsh.Read(filePath, offset, (int)lastPartSize);
                            offset += lastPartSize;
                        }
                        networkDataHelper.Send(buffer);
                        currentPart++;
                    }

                    Console.WriteLine("Sent file...");
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
