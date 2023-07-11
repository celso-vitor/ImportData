using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public class gContigAssemblerNotWorkin
    {
        int minOverlap;
        public gContigAssemblerNotWorkin(int MinOverlap)
        {
            minOverlap = MinOverlap;
        }

        public List<string> AssembleContigs(List<string> sequences)
        {

            sequences = sequences.Select(a => a.ToUpper()).OrderByDescending(a => a.Length).ToList();
            // List to store the resulting contigs
            var contigs = new List<string>();


            // Continue as long as there are strings to process
            while (sequences.Count > 0)
            {
                // Initialize the current contig with the first sequence in the list
                var currentContig = sequences[0];
                sequences.RemoveAt(0);

                // Infinite loop until finding a non-overlapping sequence
                while (true)
                {
                    var overlapIndex = -1;
                    var overlapSize = 0;
                    var overlapString = "";

                    // Find the sequence with the most overlap
                    for (int i = 0; i < sequences.Count; i++)
                    {
                        // Get the size of the overlay
                        var overlap = GetOverlapLength(currentContig, sequences[i]);
                        // If the overlap is greater than the previously found overlap,
                        // update the overlay index and size
                        if (overlap > overlapSize)
                        {
                            overlapSize = overlap;
                            overlapIndex = i;
                            overlapString = sequences[i].Substring(overlap);
                        }
                    }

                    // If an overlapping string was found, append it to the current contig
                    if (overlapIndex != -1)
                    {
                        currentContig += overlapString;
                        sequences.RemoveAt(overlapIndex);
                    }
                    else
                    {
                        // If there are no more overlaps, exit the loop
                        break;
                    }
                }

                // Add current contig to list of contigs
                contigs.Add(currentContig);
            }

            // Returns the list of mounted contigs
            return contigs;
        }

        // Method to calculate overlap size between two sequences
        private int GetOverlapLength(string seq1, string seq2)
        {
            int length = 0;

            // loop through the first sequence
            for (int i = 0; i < seq1.Length; i++)
            {
                // loop through the second sequence
                for (int j = 0; j < seq2.Length; j++)
                {
                    int k = 0;

                    // count the number of overlapping characters
                    while (i + k < seq1.Length && j + k < seq2.Length && seq1[i + k] == seq2[j + k])
                    {
                        k++;
                    }

                    // if the current overlap is greater than the previous one, update the length
                    if (k > length)
                    {
                        length = k;
                    }
                }
            }

            return length;
        }

    }
}
