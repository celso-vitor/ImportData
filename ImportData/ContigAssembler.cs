using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    using SequenceAssemblerLogic.ResultParser;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ContigAssembler
    {
        private List<string> sequences;
        private List<string> finalSequences;

        public ContigAssembler()
        {
            sequences = new List<string>();
            finalSequences = new List<string>();
        }

        // Improved method for calculating overlap length.
        private int GetOverlapLength(string seq1, string seq2, int minOverlap)
        {
            int len1 = seq1.Length;
            int len2 = seq2.Length;
            if (len1 == 0 || len2 == 0)
                return 0;

            for (int i = minOverlap; i <= Math.Min(len1, len2); i++)
            {
                if (seq1.EndsWith(seq2.Substring(0, i)))
                    return i;
            }

            return 0;
        }

        // Improved MergeSequences method.
        private bool MergeSequences(int minOverlap)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                for (int j = 0; j < sequences.Count; j++)
                {
                    if (i != j)
                    {
                        int overlap = GetOverlapLength(sequences[i], sequences[j], minOverlap);

                        if (overlap >= minOverlap)
                        {
                            // Merge sequences
                            string newSequence = sequences[i] + sequences[j].Substring(overlap);
                            // Add new merged sequence
                            sequences.Add(newSequence);
                            // Remove merged sequences
                            sequences.RemoveAt(Math.Max(i, j));
                            sequences.RemoveAt(Math.Min(i, j));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Assembles contig sequences based on minimum overlap.
        public List<string> AssembleContigSequences(List<string> inputSequences, int minOverlap)
        {
            if (inputSequences == null)
                throw new ArgumentNullException(nameof(inputSequences));

            if (minOverlap <= 0)
                throw new ArgumentException("Minimum overlap must be a positive integer.", nameof(minOverlap));

            sequences = new List<string>(inputSequences);
            bool merged = true;

            while (merged)
            {
                merged = MergeSequences(minOverlap);
            }

            finalSequences = new List<string>(sequences);
            return finalSequences;
        }

     
    }


}
