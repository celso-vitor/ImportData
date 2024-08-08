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

        //--------------------------------------------------------------------------

        // Multiple Alignment
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

        public static void SaveMSALogToFile(List<(string ID, string Sequence, string Description)> references, List<char> consensusChars, string id, string description, bool isFirstEntry)
        {
            string path = Path.Combine("..", "..", "..", "Debug", "msa_consensus_log.txt");
            StringBuilder consensusString = new StringBuilder();

            if (isFirstEntry)
            {
                File.WriteAllText(path, string.Empty); 
                consensusString.AppendLine("Method used: Multiple Sequence Alignment");
            }

            consensusString.AppendLine($"ID: {id} - Description: {description}");
            consensusString.AppendLine("Reference Sequences:");
            foreach (var reference in references)
            {
                consensusString.AppendLine($"ID: {reference.ID} - Description: {reference.Description}");
                consensusString.AppendLine(reference.Sequence);
            }
            consensusString.AppendLine("Consensus Sequence:");
            foreach (var consensusChar in consensusChars)
            {
                consensusString.Append(consensusChar);
            }
            consensusString.AppendLine();

            File.AppendAllText(path, consensusString.ToString());
        }

        //---------------------------------------------------------------------------------------

        // Consenso Local Alignment
        public static (List<(char Char, bool IsFromReference, bool IsDifferent)>, double) BuildConsensus(List<Alignment> sequencesToAlign, string referenceSequence)
        {
            if (sequencesToAlign == null || !sequencesToAlign.Any())
            {
                throw new InvalidOperationException("The list of sequences to align is empty.");
            }

        
            int maxLength = Math.Max(
                sequencesToAlign
                    .Where(seq => seq.StartPositions != null && seq.StartPositions.Any())
                    .Max(seq => seq.StartPositions.Min() - 1 + seq.AlignedSmallSequence.Length),
                referenceSequence.Length);

            List<(char Char, bool IsFromReference, bool IsDifferent)> consensusSequence = new List<(char Char, bool IsFromReference, bool IsDifferent)>();
            int totalSequences = sequencesToAlign.Count;
            int coveredPositions = 0; 
            int referenceLength = referenceSequence.Length;

            for (int i = 0; i < maxLength; i++)
            {
                var column = new List<char>();
                bool fromReferenceOnly = false;

                if (i < referenceSequence.Length)
                {
                    column.Add(referenceSequence[i]);
                    fromReferenceOnly = true;
                }

                foreach (var seq in sequencesToAlign)
                {
                    if (seq.StartPositions == null || !seq.StartPositions.Any())
                    {
                        continue;
                    }

                    int pos = i - (seq.StartPositions.Min() - 1);
                    if (pos >= 0 && pos < seq.AlignedSmallSequence.Length)
                    {
                        char charToAdd = seq.AlignedSmallSequence[pos];
                        if (charToAdd != '-')
                        {
                            column.Add(charToAdd);
                            fromReferenceOnly = false;
                        }
                    }
                }

                char consensusChar = column.GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
                bool isDifferent = column.Any(c => c != consensusChar && c != '-');
                consensusSequence.Add((consensusChar, fromReferenceOnly, isDifferent));

                if (!fromReferenceOnly)
                {
                    coveredPositions++; 
                }

               
            }


            double overallCoverage = (double)coveredPositions / referenceLength * 100;

            return (consensusSequence, overallCoverage);
        }

        public static void SaveConsensusToFile(string referenceSequence, List<char> consensusChars, string id, string description, bool isFirstEntry)
        {
            string path = Path.Combine("..", "..", "..", "Debug", "local_consensus_log.txt");
            StringBuilder consensusString = new StringBuilder();

            if (isFirstEntry)
            {
                File.WriteAllText(path, string.Empty); 
                consensusString.AppendLine("Method used: Local Alignment");
            }

            consensusString.AppendLine($"ID: {id} - Description: {description}");
            consensusString.AppendLine("Reference Sequence:");
            consensusString.AppendLine(referenceSequence);
            consensusString.AppendLine("Consensus Sequence:");
            foreach (var consensusChar in consensusChars)
            {
                consensusString.Append(consensusChar);
            }
            consensusString.AppendLine();

            File.AppendAllText(path, consensusString.ToString());
        }

    }

}


