using System;
using System.Collections.Generic;

namespace WorkShop
{
    public class Data : IDisposable
    {
        public Data(string filePath)
        {
            // TODO
        }

        
        public IAsyncEnumerable<ushort> Temperature { get; }
        public IAsyncEnumerable<ushort> Pressure { get; }

        public void Dispose() 
        {
            // TODO
        }
    }
}