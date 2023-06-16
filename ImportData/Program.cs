using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;


namespace SequenceAssemblerLogic.ResultParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            List<string> sequences = new List<string>
        {
            "ACGTYRGTPLKLNPR",
            "ACGTYRGTPLMNPS",
            "TYRGTPLSLMNPS",
            "TYRGTPLMNPS"
        };

            IdentifyContigs(sequences);
        }

        static void IdentifyContigs(List<string> sequences)
        {
            List<string> contigs = new List<string>();
            string overlapRegions = "";

            foreach (string sequence in sequences)
            {
                contigs.Add(sequence);
            }

            for (int i = 0; i < contigs.Count - 1; i++)
            {
                string overlap = "";
                for (int j = i + 1; j < contigs.Count; j++)
                {
                    if (contigs[i].Contains(contigs[j]))
                    {
                        overlap = contigs[i];
                        break;
                    }
                    else if (contigs[j].Contains(contigs[i]))
                    {
                        overlap = contigs[j];
                        break;
                    }
                }
                overlapRegions += overlap;
            }

            string finalSequence = string.Concat(contigs) + overlapRegions;

            // Imprimir as sequências
            Console.WriteLine("Sequências:");
            foreach (string sequence in sequences)
            {
                Console.WriteLine(sequence);
            }

            // Imprimir as regiões sobrepostas
            Console.WriteLine("\nRegiões sobrepostas:");
            Console.WriteLine(overlapRegions);
        }
    }
}