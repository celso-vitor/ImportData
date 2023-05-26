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
            string sequence = "ABC(Cam)DEFGHIJKLMN";
            List<int> scores = new List<int>() { 1, 4, 4, 5, 1, 1, 4, 4, 4, 5, 1, 2, 6, 6 };
            int minScore = 4;

            List<(string PeptideSequence, List<int> Scores)> validPeptides = DeNovoTagExtractor.FindValidPeptides(sequence, scores, minScore, 3).ToList();

            Console.WriteLine("Valid Peptides:");
            foreach (var peptide in validPeptides)
            {
                Console.WriteLine(peptide.PeptideSequence);
            }    

        }
       
    }
      
}

