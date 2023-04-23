using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;

namespace SequenceAssemblerLogic.ResultParser
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8
            // P A U L O C O S T A C A R V A L H O
            // 1 2 3 4 5 6 7 8 9 1 2 3 4 5 7 8 7 6

            var results = NovorParser.GetSubSequences2("PAULOCOSTACARVALHO", new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 5, 7, 8, 7, 6 }, 5, 3);

            //peptides sequences
            string sequence = "ABCDEFGHIJ";
            List<int> scores = new List<int>() { 1, 3, 4, 5, 1, 1, 4, 4, 4, 5 };
            int minScore = 3;

            List<string> validPeptides = NovorParser.FindValidPeptides(sequence, scores, minScore);

            Console.WriteLine("Valid Peptides:");
            foreach (string peptide in validPeptides)
            {
                Console.WriteLine(peptide);
            }
           

        }
       
    }
      
}

