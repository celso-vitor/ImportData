using SequenceAssemblerLogic.ResultParser;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

namespace SequenceAssemblerLogic
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8
            // P A U L O C O S T A C A R V A L H O
            // 1 2 3 4 5 6 7 8 9 1 2 3 4 5 7 8 7 6

            Stopwatch sw = Stopwatch.StartNew();

            sw.Start();
            for (int i = 0; i < 1000000; i++)
            {
                var results = NovorParser.GetSubSequences2("PAULOCOSTACARVALHO", new List<int>() { 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 7, 8, 7, 6 }, 5, 3);
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed.ToString());


            Console.WriteLine("Done");
        }
    }
}