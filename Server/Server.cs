using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;
using Server.Domain;

namespace Server
{
    internal class Server
    {
        static readonly int maxClientsAllowed = 3;
        static readonly object clientThreadsLock = new object();
        static int currentClients = 0;
        static readonly SettingsManager settingsMgr = new SettingsManager();
        static List<User> users = new List<User>();
        static readonly object usersLock = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server Application..");

            Socket serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            IPAddress serverIp = IPAddress.Parse(settingsMgr.ReadSetting(ServerConfig.ServerIpConfigKey));
            int serverPort = int.Parse(settingsMgr.ReadSetting(ServerConfig.SeverPortConfigKey));

            IPEndPoint serverEndpoint = new IPEndPoint(serverIp, serverPort);

            // If the port isn't free it throws SocketException
            serverSocket.Bind(serverEndpoint);

            serverSocket.Listen(); // We listen for connections

            Console.WriteLine("Waiting for clients to connect...");

            while (currentClients < maxClientsAllowed)
            {
                Socket clientSocket = serverSocket.Accept(); // Blocking
                lock (clientThreadsLock)
                {
                    currentClients++;
                }
                int clientNum = currentClients;
                Thread t = new Thread(() => HandleClient(clientSocket, clientNum));
                t.Start();
            }
            serverSocket.Close();
        }

        static void HandleClient(Socket clientSocket, int clientNum)
        {
            bool clientActive = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);
            while (clientActive)
            {
                try
                {
                    byte[] messageType = networkDataHelper.Receive(Protocol.MessageTypeLength);

                    byte[] commandStringBytes = networkDataHelper.Receive(Protocol.CommandStringLength);
                    string commandString = Encoding.UTF8.GetString(commandStringBytes);
                    Command command = (Command)int.Parse(commandString);

                    byte[] commandDataLength = networkDataHelper.Receive(sizeof(int));
                    int length = BitConverter.ToInt32(commandDataLength);
                    byte[] commandData = networkDataHelper.Receive(length);

                    switch (command)
                    {
                        case Command.Register:
                            Register(networkDataHelper, commandData);
                            break;
                        case Command.LogIn:
                            LogIn(networkDataHelper, commandData);
                            break;
                        case Command.SendFile:
                            ReceiveFile(networkDataHelper, commandData);
                            break;
                        default:
                            break;
                    }
                }
                catch (SocketException e)
                {
                    clientActive = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Client {clientNum} sent badly formatted data");
                    clientActive = false;
                }
            }
            Console.WriteLine($"Client {clientNum} disconnected");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            lock (clientThreadsLock)
            {
                currentClients--;
            }
        }

        static void Register(NetworkDataHelper networkDataHelper, byte[] data)
        {
            string dataString = Encoding.UTF8.GetString(data);
            string[] nameAndPass = dataString.Split('|');
            string username = nameAndPass[0].Trim();
            string password = nameAndPass[1];

            bool added = false;

            lock (usersLock)
            {
                bool exists = users.Any(u => string.Equals(u.username, username));
                if (!exists)
                {
                    users.Add(new User(username, password));
                    added = true;
                }
            }

            byte[] resultBytes = BitConverter.GetBytes(added);

            networkDataHelper.Send(resultBytes);
        }

        static void LogIn(NetworkDataHelper networkDataHelper, byte[] data)
        {
            string dataString = Encoding.UTF8.GetString(data);
            string[] nameAndPass = dataString.Split('|');
            string username = nameAndPass[0].Trim();
            string password = nameAndPass[1];

            bool success = false;

            lock (usersLock)
            {
                var user = users.FirstOrDefault(u => string.Equals(u.username, username));
                if (user != null)
                {
                    if (user.password == password)
                    {
                        success = true;
                    }
                }
            }

            byte[] resultBytes = BitConverter.GetBytes(success);

            networkDataHelper.Send(resultBytes);
        }

        static void ReceiveFile(NetworkDataHelper networkDataHelper, byte[] fileNameData)
        {
            string fileName = Encoding.UTF8.GetString(fileNameData);

            byte[] fileSizeBuffer = networkDataHelper.Receive(Protocol.FileLengthSize);
            long fileSize = BitConverter.ToInt64(fileSizeBuffer);

            long offset = 0; // bytes received
            long partCount = Protocol.CalculateFileParts(fileSize);
            long currentPart = 1;

            FileStreamHelper fsh = new FileStreamHelper();

            try
            {
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
                Console.WriteLine($"Received file {fileName}");
            }
            catch (SocketException ex)
            {
                File.Delete(fileName);
                throw new SocketException();
            }
        }
    }
}
