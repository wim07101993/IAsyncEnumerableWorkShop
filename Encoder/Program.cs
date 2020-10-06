using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Encoder
{
    public class Program
    {
        private static bool _verbose;
        private static Random _random = new Random(DateTime.Now.Millisecond);

        private static async Task Main(string[] args)
        {
            if (args.Contains("-v"))
                _verbose = true;

            Console.WriteLine("Generating tiny file");
            await SaveAsync(GenerateData((long)1e1), "Tiny.dat");
            
            Console.WriteLine("Generating small file");
            await SaveAsync(GenerateData((long)1e3), "Small.dat");
            
            Console.WriteLine("Generating large file");
            await SaveAsync(GenerateData((long)1e7), "Large.dat");
        }

        private static Data GenerateData(long measurementPoints) 
        {
            return new Data
            {
                Temperature = GenerateRandomAsync(measurementPoints, 0, 100),
                Pressure = GenerateRandomAsync(measurementPoints, 0, 10),
            };
        } 

        public static async IAsyncEnumerable<int> GenerateRandomAsync(long length, int min, int max)
        {
            long index = 0;
            var previous = 0;
            long factor = (max - min) * 2;
            while(index < length) 
            {
                previous = await Task.Run(() => 
                {
                    for (long i = 0; i < factor; i++)
                    {
                        var value = _random.Next(min, max);
                        if (value == previous)
                            return previous;
                    }    
                    return _random.Next(min, max);
                });

                yield return previous;
                index++;
            }
        }

        private static async Task SaveAsync(Data data, string path)
        {
            using var stream = new StreamWriter(path);
            Console.WriteLine("Writing temp");
            await WriteMeasurementsAsync(stream, data.Temperature, "Temp");
            Console.WriteLine("Writing press");
            await WriteMeasurementsAsync(stream, data.Pressure, "Press");
        }

        private static async Task WriteMeasurementsAsync(StreamWriter stream, IAsyncEnumerable<int> data, string name) 
        {
            var counter = 0;
            await stream.WriteAsync($"{name}=");
            if (_verbose) Console.Write($"{name}=");

            await foreach (var value in data.RunLengthEncode()) 
            {
                await stream.WriteAsync($"{value};");
                if (_verbose) Console.Write($"{value};");
                counter++;
                if (counter % 1e5 == 0)
                    Console.WriteLine($"Wrote {counter} sets");
            }

            await stream.WriteLineAsync();
            if (_verbose) Console.WriteLine();
        }
    }
}
