using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;



namespace SequenceAssemblerLogic.ContigCode
{

    public class ContigAssembler
    {
        private ConcurrentBag<Contig> contigSeeds;

        public ContigAssembler() { }

        public int GetOverlapLength(Contig c1, Contig c2, int minOverlap)
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

        private bool MergeSequences(int minOverlap, Stopwatch stopwatch, int maxTimeMilliseconds)
        {
            var mergedContigs = new ConcurrentBag<Contig>();
            var contigList = contigSeeds.ToList(); // Convert to list to avoid modifying the bag during iteration

            Parallel.For(0, contigList.Count, i =>
            {
                for (int j = 0; j < contigList.Count; j++)
                {
                    if (i != j && stopwatch.ElapsedMilliseconds < maxTimeMilliseconds)
                    {
                        var c1 = contigList[i];
                        var c2 = contigList[j];

                        int overlap = GetOverlapLength(c1, c2, minOverlap);
                        if (overlap >= minOverlap)
                        {
                            string newSequence = c1.Sequence + c2.Sequence.Substring(overlap);
                            Contig c = new Contig()
                            {
                                Sequence = newSequence,
                                IDs = c1.IDs.Concat(c2.IDs).ToList()
                            };

                            lock (mergedContigs)
                            {
                                mergedContigs.Add(c);
                            }

                            lock (contigSeeds)
                            {
                                contigSeeds.TryTake(out c1);
                                contigSeeds.TryTake(out c2);
                            }
                        }
                    }
                }
            });

            foreach (var c in mergedContigs)
            {
                contigSeeds.Add(c);
            }

            return mergedContigs.Count > 0;
        }


        public List<Contig> AssembleContigSequencesWithLimits(List<IDResult> results, int minOverlap, int maxContigs, int maxTimeMilliseconds)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            if (minOverlap <= 0)
                throw new ArgumentException("Minimum overlap must be a positive integer.", nameof(minOverlap));

            var groupedResults = results.GroupBy(result => result.CleanPeptide);
            Console.WriteLine(groupedResults.Count());

            contigSeeds = new ConcurrentBag<Contig>(
                from gr in groupedResults
                select new Contig()
                {
                    Sequence = Regex.Replace(gr.Key, @"\([^)]*\)", ""),
                    IDs = gr.ToList()
                });

            var stopwatch = Stopwatch.StartNew();
            bool merged = true;
            while (merged && contigSeeds.Count < maxContigs && stopwatch.ElapsedMilliseconds < maxTimeMilliseconds)
            {
                merged = MergeSequences(minOverlap, stopwatch, maxTimeMilliseconds);
            }

            stopwatch.Stop();
            return contigSeeds.ToList();
        }


        //---------------------------------------------------------------------------------
        public static string FindBestKmerSize(List<string> sequences, List<int> kValues)
        {
            int bestK = kValues.First();
            string bestConsensus = "";
            int maxLength = 0;

            foreach (int k in kValues)
            {
                string consensus = AssembleContigSequencesWithDeBruijnGraph(sequences, k);
                if (consensus.Length > maxLength)
                {
                    maxLength = consensus.Length;
                    bestK = k;
                    bestConsensus = consensus;
                }
            }

            Console.WriteLine($"Best k-mer size: {bestK}");
            return bestConsensus;
        }

        public static string AssembleContigSequencesWithDeBruijnGraph(List<string> sequences, int k)
        {
            var kmers = GetKmers(sequences, k);
            var graph = new DeBruijnGraph(kmers);
            var path = graph.GetEulerianPath();
            return ReconstructSequence(path);
        }

        public static List<string> GetKmers(List<string> sequences, int k)
        {
            var kmers = new List<string>();
            foreach (var sequence in sequences)
            {
                for (int i = 0; i <= sequence.Length - k; i++)
                {
                    kmers.Add(sequence.Substring(i, k));
                }
            }
            return kmers;
        }

        public static string ReconstructSequence(List<string> path)
        {
            var sequence = path.First();
            foreach (var kmer in path.Skip(1))
            {
                sequence += kmer.Last();
            }
            return sequence;
        }
    }
}
