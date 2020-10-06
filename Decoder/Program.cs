using System;
using System.Threading.Tasks;

namespace Decoder
{
    public class Program
    {
        private static bool _verbose;
        private static string _filePath = "Large.dat";

        public static bool Verbose => _verbose;

        private static async Task Main(string[] args)
        {
            ParseArgs(args);

            using var data = new Data(_filePath);

            await foreach (var t in data.Temperature)
            {
                if (Verbose) Console.Write($"{t} ");
            }

            if (Verbose) Console.WriteLine();
        }

        private static void ParseArgs(string []args) 
        {
            var isPath = false;
            foreach (var arg in args) 
            {
                if (isPath)
                {
                    _filePath = arg;
                    isPath = false;
                }

                switch (arg) 
                {
                    case "-v" : _verbose = true; break;
                    case "-f" : isPath = true; break;
                }
            }
        }
    }
}
