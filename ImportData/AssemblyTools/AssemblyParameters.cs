using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SequenceAssemblerLogic.ProteinAlignmentCode;

namespace SequenceAssemblerLogic.AssemblyTools
{
    public class AssemblyParameters
    {
        public static string GenerateConsensusSequence(List<Alignment> alignments, int referenceLength)
        {
            if (alignments == null || alignments.Count == 0)
            {
                return string.Empty;
            }

            // Assumindo que todas as sequências alinhadas têm o mesmo comprimento
            char[] consensusSequence = new char[referenceLength];

            for (int i = 0; i < referenceLength; i++)
            {
                Dictionary<char, int> frequency = new Dictionary<char, int>();

                foreach (var alignment in alignments)
                {
                    if (i < alignment.AlignedSmallSequence.Length)
                    {
                        char aminoAcid = alignment.AlignedSmallSequence[i];

                        if (aminoAcid != '-')
                        {
                            if (frequency.ContainsKey(aminoAcid))
                            {
                                frequency[aminoAcid]++;
                            }
                            else
                            {
                                frequency[aminoAcid] = 1;
                            }
                        }
                    }
                }

                // Encontrar o aminoácido mais frequente
                if (frequency.Count > 0)
                {
                    char consensusChar = frequency.OrderByDescending(f => f.Value).First().Key;
                    consensusSequence[i] = consensusChar;
                }
                else
                {
                    consensusSequence[i] = '-'; // Adicionar gap se não houver aminoácido dominante
                }
            }

            return new string(consensusSequence);
        }

        public static int FindAvailableRow(Dictionary<int, int> rowEndPositions, int startPosition, int length)
        {
            foreach (var row in rowEndPositions)
            {
                if (row.Value <= startPosition)
                {
                    return row.Key;
                }
            }

            int newRow = rowEndPositions.Count;
            rowEndPositions[newRow] = 0;
            return newRow;
        }

        public static int FindNextAvailableRow(Dictionary<int, int> rowEndPositions, int startPosition, int length)
        {
            foreach (var row in rowEndPositions)
            {
                if (row.Value <= startPosition + length)
                {
                    return row.Key;
                }
            }

            int newRow = rowEndPositions.Count;
            rowEndPositions[newRow] = 0;
            return newRow;
        }

        public static string CalculateConsensusSequence(List<(string ID, string Sequence, string Description)> alignedSequences, List<Alignment> alignments, out List<(int Position, char ConsensusChar, bool IsConsensus, bool IsDifferent)> consensusDetails)
        {
            consensusDetails = new List<(int Position, char ConsensusChar, bool IsConsensus, bool IsDifferent)>();

            if (alignedSequences == null || alignedSequences.Count == 0)
            {
                return string.Empty;
            }

            int sequenceLength = alignedSequences.First().Sequence.Length;
            char[] consensusSequence = new char[sequenceLength];

            for (int i = 0; i < sequenceLength; i++)
            {
                var positionChars = alignments
                    .Where(seq => seq.StartPositions.Max() <= i && i < seq.StartPositions.Max() + seq.AlignedSmallSequence.Length)
                    .Select(seq => seq.AlignedSmallSequence[i - seq.StartPositions.Max()])
                    .ToArray();

                var mostFrequentCharGroup = positionChars
                    .GroupBy(c => c)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                char mostFrequentChar = mostFrequentCharGroup?.Key ?? '-';
                bool isConsensus = mostFrequentCharGroup?.Count() == alignedSequences.Count;
                bool isDifferent = positionChars.Distinct().Count() > 1; // Check if there are different bases at the position

                consensusSequence[i] = mostFrequentChar;
                consensusDetails.Add((i, mostFrequentChar, isConsensus, isDifferent));
            }

            return new string(consensusSequence);
        }


        public static double CalculateCoverage(List<(string ID, string Sequence, string Description)> alignedSequences, List<Alignment> alignments)
        {
            if (alignedSequences == null || alignedSequences.Count == 0)
            {
                return 0.0;
            }

            int sequenceLength = alignedSequences.First().Sequence.Length;
            HashSet<int> coveredPositions = new HashSet<int>();

            foreach (var alignment in alignments)
            {
                for (int i = 0; i < alignment.AlignedSmallSequence.Length; i++)
                {
                    int position = alignment.StartPositions.Max() + i;
                    if (alignment.AlignedSmallSequence[i] != '-')
                    {
                        coveredPositions.Add(position);
                    }
                }
            }

            double coverage = (double)coveredPositions.Count / sequenceLength * 100;
            return coverage;
        }


    }

}

