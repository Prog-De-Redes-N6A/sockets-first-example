using System.Net.Sockets;
using System.Net;
using System.Text;
using Common;

namespace Cliente
{
    internal class Client
    {
        static readonly SettingsManager settingsMgr = new SettingsManager();

        static void PrintMenu()
        {
            Console.WriteLine("OPTIONS MENU");
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Log In");
            Console.WriteLine("3. Send File");
            Console.WriteLine("4. Exit");
            Console.Write("Please select option: ");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Client Application..");

            Socket clientSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            IPAddress clientIp = IPAddress.Parse(settingsMgr.ReadSetting(ClientConfig.ClientIpConfigKey));
            int clientPort = int.Parse(settingsMgr.ReadSetting(ClientConfig.ClientPortConfigKey));
            IPEndPoint localEndpoint = new IPEndPoint(clientIp, clientPort);
            clientSocket.Bind(localEndpoint);

            IPAddress serverIp = IPAddress.Parse(settingsMgr.ReadSetting(ServerConfig.ServerIpConfigKey));
            int serverPort = int.Parse(settingsMgr.ReadSetting(ServerConfig.SeverPortConfigKey));
            IPEndPoint serverEndpoint = new IPEndPoint(serverIp, serverPort);

            try
            {
                clientSocket.Connect(serverEndpoint); // Blocking
            }
            catch (Exception e)
            {
                Console.WriteLine("Could not connect to server, shutting down");
                Thread.Sleep(3000);
                return;
            }

            Console.WriteLine("Connected to server!!");

            bool clientRunning = true;
            NetworkDataHelper networkDataHelper = new NetworkDataHelper(clientSocket);

            while (clientRunning)
            {
                PrintMenu();

                string commandString = Console.ReadLine();
                int commandNum = -1;
                try
                {
                    commandNum = int.Parse(commandString) - 1;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Invalid command format");
                    continue;
                }
                Command command = (Command)commandNum;
                if (command == Command.Exit) break;

                byte[] messageType = Encoding.UTF8.GetBytes(Protocol.Request);

                string commandStringData = commandNum.ToString("D2");
                byte[] commandStringDataBytes = Encoding.UTF8.GetBytes(commandStringData);

                try
                {
                    networkDataHelper.Send(messageType);

                    networkDataHelper.Send(commandStringDataBytes);
                }
                catch (SocketException e)
                {
                    clientRunning = false;
                    break;
                }

                switch (command)
                {
                    case Command.Register:
                        clientRunning = Register(networkDataHelper);
                        break;
                    case Command.LogIn:
                        clientRunning = LogIn(networkDataHelper);
                        break;
                    case Command.SendFile:
                        clientRunning = SendFile(networkDataHelper);
                        break;
                    default:
                        Console.WriteLine("Invalid option");
                        break;
                }
            }

            Console.WriteLine("Connection interrupted...");
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }

        internal static bool Register(NetworkDataHelper networkDataHelper)
        {
            Console.Write("Username:");
            string? userName = Console.ReadLine();
            if (userName == null)
            {
                Console.WriteLine("Username cannot be null");
                return true;
            }

            Console.Write("Password:");
            string? pass = Console.ReadLine();
            if (pass == null)
            {
                Console.WriteLine("Password cannot be null");
                return true;
            }

            string userAndPass = userName + "|" + pass;
            byte[] commandData = Encoding.UTF8.GetBytes(userAndPass);
            int commandLength = commandData.Length;
            byte[] commandDataLength = BitConverter.GetBytes(commandLength);

            try
            {
                networkDataHelper.Send(commandDataLength);
                networkDataHelper.Send(commandData);

                byte[] result = networkDataHelper.Receive(1);
                bool loggedIn = BitConverter.ToBoolean(result);

                if (loggedIn)
                {
                    Console.WriteLine("Registered correctly...");
                }
                else
                {
                    Console.WriteLine("Could not register, that username already exists...");
                }
                return true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Connection interrupted");
                return false;
            }
        }

        internal static bool LogIn(NetworkDataHelper networkDataHelper)
        {
            Console.Write("Username:");
            string? userName = Console.ReadLine();
            if (userName == null)
            {
                Console.WriteLine("Username cannot be null");
                return true;
            }

            Console.Write("Password:");
            string? pass = Console.ReadLine();
            if (pass == null)
            {
                Console.WriteLine("Password cannot be null");
                return true;
            }

            string userAndPass = userName + "|" + pass;
            byte[] commandData = Encoding.UTF8.GetBytes(userAndPass);
            int commandLength = commandData.Length;
            byte[] commandDataLength = BitConverter.GetBytes(commandLength);

            try
            {
                networkDataHelper.Send(commandDataLength);
                networkDataHelper.Send(commandData);

                byte[] result = networkDataHelper.Receive(1);
                bool loggedIn = BitConverter.ToBoolean(result);

                if (loggedIn)
                {
                    Console.WriteLine("Logged in...");
                }
                else
                {
                    Console.WriteLine("Failed to log in...");
                }
                return true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Connection interrupted");
                return false;
            }
        }

        internal static bool SendFile(NetworkDataHelper networkDataHelper)
        {
            try
            {
                Console.WriteLine("Type the path of file:");
                string? filePath = Console.ReadLine();
                if (filePath == null)
                {
                    Console.WriteLine("Path cannot be null");
                    return true;
                }
                filePath = filePath?.Trim().Trim('"') ?? string.Empty;

                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    Console.WriteLine("File does not exist");
                    return true;
                }
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
                return true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("Connection interrupted");
                return false;
            }
        }
    }
}
