using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    public static void Main()
    {
        List<string> sequences = new List<string>()
        {
            "ABCDEFGHI",
            "GHIJKLMN",
            "MNOPQESTUVXZ",
            "ZZZ",
            "DEFGH",
            "FGHBBBJ",
            "bbJklLL",
            "AAVCASF"
        };

        var contigs = AssembleContigs(sequences);

        Console.WriteLine("Contigs:");
        foreach (var contig in contigs)
        {
            Console.WriteLine(contig);
        }
    }

    public static List<string> AssembleContigs(List<string> sequences)
    {
        var contigs = new List<string>();
        while (sequences.Count > 0)
        {
            var currentContig = sequences[0];
            sequences.RemoveAt(0);

            while (true)
            {
                var overlapIndex = -1;
                var overlapSize = 0;
                var overlapString = "";

                // Find the sequence with maximum overlap
                for (int i = 0; i < sequences.Count; i++)
                {
                    var size = GetOverlapLength(currentContig, sequences[i]);
                    if (size > overlapSize)
                    {
                        overlapSize = size;
                        overlapIndex = i;
                        overlapString = sequences[i].Substring(size);
                    }
                }

                if (overlapIndex != -1)
                {
                    currentContig += overlapString;
                    sequences.RemoveAt(overlapIndex);
                }
                else
                {
                    break;
                }
            }

            contigs.Add(currentContig);
        }

        return contigs;
    }

    public static int GetOverlapLength(string seq1, string seq2)
    {
        seq1 = seq1.ToUpper();
        seq2 = seq2.ToUpper();

        int length = Math.Min(seq1.Length, seq2.Length);

        for (int i = length; i > 0; i--)
        {
            if (seq1.EndsWith(seq2.Substring(0, i)))
            {
                return i;
            }
        }

        return 0;
    }
}