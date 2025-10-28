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

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Server Application..");

            Socket serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            string serverHostnameString = Environment.GetEnvironmentVariable(ServerConfig.ServerIpConfigKey) ?? "0.0.0.0";
            string serverPortString = Environment.GetEnvironmentVariable(ServerConfig.SeverPortConfigKey) ?? "5000";

            IPAddress[] addresses = Dns.GetHostAddresses(serverHostnameString);
            IPAddress? serverIp = addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (serverIp == null)
            {
                throw new Exception($"Cannot resolve hostname: {serverHostnameString}");
            }
            int serverPort = int.Parse(serverPortString);

            IPEndPoint serverEndpoint = new IPEndPoint(serverIp, serverPort);

            // If the port isn't free it throws SocketException
            serverSocket.Bind(serverEndpoint);

            serverSocket.Listen(); // We listen for connections

            Console.WriteLine("Waiting for clients to connect...");

            while (currentClients < maxClientsAllowed)
            {
                Socket clientSocket = await serverSocket.AcceptAsync();
                lock (clientThreadsLock)
                {
                    currentClients++;
                }
                _ = Task.Run(async () =>
                {
                    int clientNum = currentClients;
                    await HandleClientAsync(clientSocket, clientNum);
                });
            }
            serverSocket.Close();
        }

        static async Task HandleClientAsync(Socket clientSocket, int clientNum)
        {
            bool clientActive = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);
            while (clientActive)
            {
                try
                {
                    byte[] messageType = await networkDataHelper.ReceiveAsync(Protocol.MessageTypeLength);

                    byte[] commandStringBytes = await networkDataHelper.ReceiveAsync(Protocol.CommandStringLength);
                    string commandString = Encoding.UTF8.GetString(commandStringBytes);
                    Command command = (Command)int.Parse(commandString);

                    byte[] commandDataLength = await networkDataHelper.ReceiveAsync(sizeof(int));
                    int length = BitConverter.ToInt32(commandDataLength);
                    byte[] commandData = await networkDataHelper.ReceiveAsync(length);

                    switch (command)
                    {
                        case Command.Register:
                            await Register(networkDataHelper, commandData);
                            break;
                        case Command.LogIn:
                            await LogIn(networkDataHelper, commandData);
                            break;
                        case Command.SendFile:
                            await ReceiveFile(networkDataHelper, commandData);
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

        static async Task Register(NetworkDataHelper networkDataHelper, byte[] data)
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

            await networkDataHelper.SendAsync(resultBytes);
        }

        static async Task LogIn(NetworkDataHelper networkDataHelper, byte[] data)
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

            await networkDataHelper.SendAsync(resultBytes);
        }

        static async Task ReceiveFile(NetworkDataHelper networkDataHelper, byte[] fileNameData)
        {
            string fileName = Encoding.UTF8.GetString(fileNameData);
            string receiveDirectory = Environment.GetEnvironmentVariable(ServerConfig.ReceivedFilesFolder) ?? "ReceivedFiles";
            if (!Directory.Exists(receiveDirectory))
            {
                Directory.CreateDirectory(receiveDirectory);
            }
            fileName = Path.Combine(receiveDirectory, fileName);
            byte[] fileSizeBuffer = await networkDataHelper.ReceiveAsync(Protocol.FileLengthSize);
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
                        buffer = await networkDataHelper.ReceiveAsync(Protocol.MaxFilePartSize);
                        offset += Protocol.MaxFilePartSize;
                    }
                    else
                    {
                        long lastPartSize = fileSize - offset;
                        Console.WriteLine($"Receiving segment #{currentPart} of size {lastPartSize}");
                        buffer = await networkDataHelper.ReceiveAsync((int)lastPartSize);
                        offset += lastPartSize;
                    }
                    await fsh.WriteAsync(fileName, buffer);
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
