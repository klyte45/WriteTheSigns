using System.IO;

namespace SpriteFontPlus.Utility
{
    internal static class IOUtils
    {
        public static byte[] ToByteArray(this Stream stream)
        {

            // Rewind stream if it is at end
            if (stream.CanSeek && stream.Length == stream.Position)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            byte[] buffer = new byte[16 * 1024];
            // Copy it's data to memory
            using (var ms = new MemoryStream())
            {

                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }

        }
    }
}
