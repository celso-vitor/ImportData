using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

namespace SequenceAssemblerLogic.ResultParser
{
    class Program
    {
        static void Main(string[] args)
        {

            //peptides sequences
            string sequence = "Q(Pyro-Glu)GKGEW(O)SSGRR";


            
            List<int> scores = new List<int>() { 23, 20, 20, 20, 13, 4, 1, 20, 20, 20, 20 };
            int minScore = 19;

            List<(string PeptideSequence, List<int> Scores)> validPeptides = DeNovoTagExtractor.FindValidPeptides(sequence, scores, minScore, 4).ToList();

            Console.WriteLine("Valid Peptides:");
            foreach (var peptide in validPeptides)
            {
                Console.WriteLine(peptide.PeptideSequence);
            }    

        }
       
    }
      
}

