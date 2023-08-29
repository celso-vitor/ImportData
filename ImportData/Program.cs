using SequenceAssemblerLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Xml;

public class Program
{
    private static void Main()
    {
        List<FASTA> alignments = FastaParser.ParseFastaFile("output.txt");
        Console.WriteLine(alignments.Count + " alignments read");

        if (alignments.Count > 1)
        {
            // Reference String
            string reference = alignments[0].Sequence;

            for (int i = 1; i < alignments.Count; i++)
            {
                // The current alignment and its name (Header)
                string contig = alignments[i].Sequence;
                string contigName = alignments[i].ID;

                // Initialize positions
                int contigPos = 0;
                int refPos = 0;

                // To store the positions of the contigs
                List<(string word, int startPos)> contigPosList = new();

                string word = "";
                for (int j = 0; j < contig.Length; j++)
                {
                    if (contig[j] != '-')
                    {
                        word += contig[j];
                        contigPos++;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(word))
                        {
                            contigPosList.Add((word, refPos + 1 - word.Length));
                            word = "";
                        }
                    }

                    if (j < reference.Length && reference[j] != '-')
                    {
                        refPos++;
                    }
                }

                // Print the positions found
                Console.WriteLine($"{contigName}:");
                foreach (var (wordFound, startPosition) in contigPosList)
                {
                    Console.WriteLine($"{wordFound}, Position: {startPosition}");
                }
            }
        }

        Console.WriteLine("Done");
    }
}







