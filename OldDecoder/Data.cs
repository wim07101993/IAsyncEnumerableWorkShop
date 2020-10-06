using System.Collections.Generic;

namespace OldDecoder
{
    public struct Data
    {
#if !NET5
        public Data(IEnumerable<int> temperature, IEnumerable<int> pressure) 
        {
            Temperature = temperature;
            Pressure = pressure;
        }
#endif

        public IEnumerable<int> Temperature 
        {
            get;
#if NET5
            init;
#endif
        }

        public IEnumerable<int> Pressure 
        { 
            get; 
#if NET5
            init;
#endif
        }
    }
}