using System.Net.Sockets;

namespace Common
{
    public class NetworkDataHelper
    {
        public readonly Socket _socket;

        public NetworkDataHelper(Socket socket)
        {
            _socket = socket;
        }

        public async Task<byte[]> ReceiveAsync(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;

            while (offset < length)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, length-offset);
                int received = await _socket.ReceiveAsync(segment, SocketFlags.None);
                if (received == 0)
                {
                    throw new SocketException();
                }
                offset += received;
            }

            return buffer;
        }

        public async Task SendAsync(byte[] buffer)
        {
            int length = buffer.Length;
            int offset = 0;

            while (offset < length)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, length - offset);
                int sent = await _socket.SendAsync(segment, SocketFlags.None);
                if (sent == 0)
                {
                    throw new SocketException();
                }
                offset += sent;
            }
        }
    }
}
