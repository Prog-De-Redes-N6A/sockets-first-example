using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Protocol
    {
        public static int MessageTypeLength = 3;
        public static int CommandStringLength = 2;
        public static string Request = "REQ";
        public static string Response = "RES";
        public const int MaxFilePartSize = 32768; // 32K
        public const int FileNameLengthSize = 4;
        public const int FileLengthSize = 8;

        public static long CalculateFileParts(long fileSize)
        {
            long fileParts = fileSize / MaxFilePartSize;
            return fileParts * MaxFilePartSize == fileSize ? fileParts : fileParts + 1;
        }
    }

    public enum Command
    {
        Register,
        LogIn,
        SendFile,
        Exit,
    }
}
