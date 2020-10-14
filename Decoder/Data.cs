using System;
using System.Collections.Generic;
using System.IO;

namespace Decoder
{
    public sealed class Data : IDisposable
    {
        public Data(string path)
        {
            Stream = new BufferedStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read));
            Temperature = new MeasurementEnumerable(this, "Temp");
            Pressure = new MeasurementEnumerable(this, "Press");
        }

        internal object Lock { get; } = new object();
        internal Stream Stream { get; }

        public IAsyncEnumerable<int> Temperature { get; }
        public IAsyncEnumerable<int> Pressure { get; }

        public void Dispose() => Stream.Dispose();
    }
}
