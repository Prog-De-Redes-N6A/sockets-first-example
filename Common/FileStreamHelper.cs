using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FileStreamHelper
    {
        public async Task<byte[]> ReadAsync(string path, long offset, int length)
        {
            byte[] data = new byte[length];

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                fs.Position = offset;
                int bytesRead = 0;
                while (bytesRead < length)
                {
                    int read = await fs.ReadAsync(data, bytesRead, length - bytesRead);
                    if (read == 0)
                    {
                        throw new Exception("File could not be read");
                    }
                    bytesRead += read;
                }
            }
            return data;
        }

        public async Task WriteAsync(string path, byte[] data)
        {
            if (File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Append))
                {
                    await fs.WriteAsync(data, 0, data.Length);
                }
            }
            else
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    await fs.WriteAsync(data, 0, data.Length);
                }
            }
        }
    }
}
