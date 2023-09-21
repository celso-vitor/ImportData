using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ContigCode
{
    using SequenceAssemblerLogic.ResultParser;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text.RegularExpressions;

    public class ContigAssembler
    {
        private List<Contig> contigSeeds;

        public ContigAssembler()
        {

        }

      

        // Improved method for calculating overlap length.
        private int GetOverlapLength(Contig c1, Contig c2, int minOverlap)
        {
            int len1 = c1.Sequence.Length;
            int len2 = c2.Sequence.Length;
            if (len1 == 0 || len2 == 0)
                return 0;

            for (int i = minOverlap; i <= Math.Min(len1, len2); i++)
            {
                if (c1.Sequence.EndsWith(c2.Sequence.Substring(0, i)))
                    return i;
            }

            return 0;
        }

        // Improved MergeSequences method.
        private bool MergeSequences(int minOverlap)
        {
            for (int i = 0; i < contigSeeds.Count; i++)
            {
                for (int j = 0; j < contigSeeds.Count; j++)
                {
                    if (i != j)
                    {
                        int overlap = GetOverlapLength(contigSeeds[i], contigSeeds[j], minOverlap);

                        if (overlap >= minOverlap)
                        {
                            // Merge sequences
                            string newSequence = contigSeeds[i].Sequence + contigSeeds[j].Sequence.Substring(overlap);
                            // Add new merged sequence
                            Contig c = new Contig()
                            {
                                Sequence = newSequence,
                                IDs = contigSeeds[i].IDs.Concat(contigSeeds[j].IDs).ToList()
                            };

                            contigSeeds.Add(c);
                            // Remove merged sequences
                            contigSeeds.RemoveAt(Math.Max(i, j));
                            contigSeeds.RemoveAt(Math.Min(i, j));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Assembles contig sequences based on minimum overlap.
        public List<Contig> AssembleContigSequences(List<IDResult> results, int minOverlap)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            if (minOverlap <= 0)
                throw new ArgumentException("Minimum overlap must be a positive integer.", nameof(minOverlap));


            var groupedResults = results.GroupBy(result => result.CleanPeptide);
            Console.WriteLine(groupedResults.Count());

            contigSeeds = (from gr in groupedResults
                           select new Contig()
                           {
                               Sequence = Regex.Replace(gr.Key, @"\([^)]*\)", ""),
                               IDs = gr.ToList()
                           }).ToList();



            bool merged = true;

            while (merged)
            {
                merged = MergeSequences(minOverlap);
            }

            return contigSeeds;

        }
    }


}
