using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OldDecoder
{
    public class Program
    {
        private static bool verbose;
        private static string filePath = "Large.dat";
        
        private static async Task Main(string[] args)
        {
            ParseArgs(args);

            Console.WriteLine("Bad solution");
            var badData = await LoadBadAsync(filePath);

            foreach (var t in badData.Temperature)
            {
                if (verbose) Console.Write($"{t} ");
            }

            if (verbose) Console.WriteLine();

            badData = null;
            GC.Collect();

            Console.WriteLine("Better solution");
            var data = await LoadAsync(filePath);

            foreach (var t in data.Temperature)
            {
                if (verbose) Console.Write($"{t} ");
            }

            if (verbose) Console.WriteLine();
        }

        private static void ParseArgs(string []args) 
        {
            var isPath = false;
            foreach (var arg in args) 
            {
                if (isPath)
                {
                    filePath = arg;
                    isPath = false;
                }

                switch (arg) 
                {
                    case "-v" : verbose = true; break;
                    case "-f" : isPath = true; break;
                }
            }
        }

        private static async Task<BadData> LoadBadAsync(string path) 
        {
            // read lines from file
            var lines = await File.ReadAllLinesAsync(path);
            if (verbose)
            {
                Console.WriteLine("R:");
                foreach (var line in lines)
                    Console.WriteLine(line);
            } 

            // create return value
            var data = new BadData();

            // interprete lines
            foreach (var line in lines)
            {
                var split = line.Split('=');
                if (split.Length != 2)
                    continue;

                var values = Decode(split[1]);
                switch (split[0])
                {
                    case "Temp": data.Temperature = values; break;
                    case "Press": data.Pressure = values; break;
                }
            }

            return data;

            static List<int> Decode(string line) 
            {
                // each value repetition is separated by a ';'
                var parts = line.Split(';', StringSplitOptions.RemoveEmptyEntries);

                // create return value
                var list = new List<int>();
                foreach (var part in parts)
                {
                    // the value and repetition count are separated by a ':'
                    var valueSplit = part.Split(':');

                    // create the number of values and add them to the list
                    var value = int.Parse(valueSplit[0]);
                    if (verbose) Console.Write($"P{value} ");
                    var range = int.Parse(valueSplit[1]);
                    if (verbose) Console.Write($"P{range} ");

                    for (var i = 0; i < range; i++)
                        list.Add(value);
                        
                    if (verbose) Console.WriteLine();
                }

                return list;
            }
        }
    
        private static async Task<Data> LoadAsync(string path) 
        {
            // use reader to read the file
            using var reader = new StreamReader(path);

            // create return collections
            IEnumerable<int> temp = null;
            IEnumerable<int> press = null;

            // iterate over each line separatly
            var line = await reader.ReadLineAsync();

            while(line != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                    
                if (verbose) Console.WriteLine($"R{line}");

                // interprete the current line
                var split = line.Split('=');
                var values = Decode(split[1]);

                switch (split[0])
                {
                    case "Temp": temp = values; break;
                    case "Press": press = values; break;
                }

                line = await reader.ReadLineAsync();
            }

#if NET5
            return new Data { Temperature = temp, Pressure = press };
#else
            return new Data(temp, press);
#endif

            static IEnumerable<int> Decode(string line) 
            {
                 // each value repetition is separated by a ';'
                var parts = line.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    // the value and repetition count are separated by a ':'
                    var valueSplit = part.Split(':');

                    // use yield return to only return values when they are asked
                    var value = int.Parse(valueSplit[0]);
                    if (verbose) Console.Write($"P{value} ");
                    var range = int.Parse(valueSplit[1]);
                    if (verbose) Console.Write($"P{range} ");

                    for (var i = 0; i < range; i++)
                        yield return value;

                    if (verbose) Console.WriteLine();
                }
             }
        }
    }
}
