using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public class ContigAssembler
    {
        private Dictionary<string, string> sequencesDict;
        private List<string> finalSequences;

        public ContigAssembler()
        {
            sequencesDict = new Dictionary<string, string>();
            finalSequences = new List<string>();
        }

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

        private void MergeSequences(int minOverlap)
        {
            bool merged;

            do
            {
                merged = false;

                var keys = new List<string>(sequencesDict.Keys);
                foreach (var key1 in keys)
                {
                    foreach (var key2 in keys)
                    {
                        if (key1 != key2)
                        {
                            var overlap = GetOverlapLength(key1, key2);
                            if (overlap >= minOverlap)
                            {
                                var newKey = key1 + key2.Substring(overlap);
                                sequencesDict.Remove(key1);
                                sequencesDict.Remove(key2);
                                sequencesDict.Add(newKey, "");
                                merged = true;
                                break;
                            }
                        }
                    }

                    if (merged) break;
                }
            }
            while (merged);
        }

        public List<string> AssembleContigSequences(List<string> sequences, int minOverlap)
        {
            sequencesDict = sequences.ToDictionary(x => x, x => "");
            MergeSequences(minOverlap);
            finalSequences = sequencesDict.Keys.ToList();
            return finalSequences;
        }
    }
}
