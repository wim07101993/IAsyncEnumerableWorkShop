﻿using System;
using System.Threading.Tasks;

namespace WorkShop
{
    public class Program
    {
        private const string FilePath = "Tiny.dat";

        private static async Task Main()
        {
            var data = new Data(FilePath);

            await foreach(var t in data.Temperature) 
            {
                Console.WriteLine($"{t} ");
            }
        }
    }
}
