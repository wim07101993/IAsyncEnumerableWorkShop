using System;
using System.Threading.Tasks;

namespace WorkShop
{
    public class Program
    {
        private const string FilePath = "Large.dat";

        private static async Task Main(string[] args)
        {
            var data = new Data(FilePath);

            await foreach(var t in data.Temperature) 
            {
                Console.WriteLine($"{t} ");
            }
        }
    }
}
