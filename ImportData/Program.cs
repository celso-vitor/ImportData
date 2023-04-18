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

            var results = NovorParser.GetSubSequences2("PAULOCOSTACARVALHO", new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 7, 8, 7, 6 }, 5, 3);

            string peptide = "ABCDEFGHIJKLMN";
            List<int> scores = new List<int>() { 1, 2, 3, 4, 5, 1, 2, 4, 4, 4, 1, 6, 6, 6 };
            int minscore = 3;

            List<string> subPeptides = ExtractPeptides(peptide, scores, minscore);


            Console.WriteLine("Done");
        }

        private static List<string> ExtractPeptides(string peptide, List<int> scores, int minscore)
        {
            throw new NotImplementedException();
        }
    }
}