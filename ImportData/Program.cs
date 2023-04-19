using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;

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
            
            NovorParser.FindPeptides("ABCDEFGHIJKLMN", new List<int>() { 1, 2, 3, 4, 5, 1, 2, 4, 4, 4, 1, 6, 6, 6 }, 3);
            {
                string peptide = "ABCDEFGHIJKLMN";
                List<int> score = new List<int>() { 1, 2, 3, 4, 5, 1, 2, 4, 4, 4, 1, 6, 6, 6 };
                int minscore = 3;

                List<int> validCharacters = new List<int>();
                for (int i = 0; i < peptide.Length; i++)
                {
                    if (score[i] >= minscore)
                    {
                        validCharacters.Add(peptide[i]);
                    }

                }
                Console.WriteLine("Valid Characters:");
                Console.WriteLine(string.Join(", ", validCharacters));

            }
            
        }
       
    }
      
}

