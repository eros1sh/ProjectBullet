using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace RuriLib.Models.Data.DataPools
{
    public class FileDataPool : DataPool
    {
        public string FileName { get; private set; }

        public readonly int POOL_CODE = -2;

        /// <summary>
        /// Creates a DataPool by loading lines from a file with the given <paramref name="fileName"/>.
        /// </summary>
        public FileDataPool(string fileName, string wordlistType = "Default")
        {
            FileName = fileName;
            DataList = File.ReadLines(fileName);
            Size = CountLinesfast(fileName);
            WordlistType = wordlistType;
        }

        /// <inheritdoc/>
        public override void Reload()
        {
            DataList = File.ReadLines(FileName);
        }

        /// <summary>
        /// Counts lines using buffered byte scanning — much faster than LINQ Count() for large files.
        /// </summary>
        private static long CountLinesfast(string filePath)
        {
            const int bufferSize = 1024 * 128;
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            long lineCount = 0;

            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize);
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
                {
                    var span = buffer.AsSpan(0, bytesRead);
                    for (int i = 0; i < span.Length; i++)
                    {
                        if (span[i] == (byte)'\n')
                            lineCount++;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            // If file is non-empty and doesn't end with newline, count the last line
            if (lineCount == 0)
            {
                var info = new FileInfo(filePath);
                if (info.Length > 0) lineCount = 1;
            }

            return lineCount;
        }
    }
}
