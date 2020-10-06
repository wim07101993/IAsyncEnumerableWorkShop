using System.Collections.Generic;

namespace Encoder
{
    public class Data
    {
        public IAsyncEnumerable<int> Temperature { get; set; }
        public IAsyncEnumerable<int> Pressure { get; set; }
    }
}
