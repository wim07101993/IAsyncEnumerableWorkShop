using System;
using System.Collections.Generic;
using System.IO;

namespace WorkShop
{
    public class Data : IDisposable
    {
        public Data(string filePath)
        {
            Stream = new BufferedStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        internal object Lock { get; } = new object();
        internal Stream Stream { get; }

        public IAsyncEnumerable<ushort> Temperature { get; }
        public IAsyncEnumerable<ushort> Pressure { get; }

        public void Dispose() => Stream.Dispose();
    }
}