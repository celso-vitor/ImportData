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

            int sequenceLength = alignedSequences.First().Sequence.Length;  // Assumes all sequences have the same length
            char[] consensusSequence = new char[sequenceLength];

            for (int i = 0; i < sequenceLength; i++)
            {
                // Collect characters at position i from all aligned fragments that have an aligned character at this position
                var positionChars = alignments
                    .Where(seq => seq.StartPositions.Max() <= i && i < seq.StartPositions.Max() + seq.AlignedSmallSequence.Length)
                    .Select(seq => seq.AlignedSmallSequence[i - seq.StartPositions.Max()])
                    .Where(c => c != '-') // Exclude gaps for consensus calculation
                    .ToArray();

                char consensusChar;

                // If there are no aligned fragments at this position, check the template sequences
                if (positionChars.Length == 0)
                {
                    // Collect the characters at this position from all template sequences
                    var templateChars = alignedSequences
                        .Where(seq => seq.Sequence.Length > i) // Ensure the sequence is long enough
                        .Select(seq => seq.Sequence[i])
                        .Distinct()
                        .ToArray();

                    // If all template sequences have the same letter, use it as the consensus character
                    if (templateChars.Length == 1)
                    {
                        consensusChar = templateChars[0];
                    }
                    else
                    {
                        // If template sequences don't agree, set the consensus to 'X'
                        consensusChar = 'X';
                    }

                    // No fragments aligned, so it's neither a consensus nor a difference
                    consensusDetails.Add((i, consensusChar, false, false));
                }
                else
                {
                    // Find the most frequent character among aligned fragments (excluding gaps)
                    var mostFrequentCharGroup = positionChars
                        .GroupBy(c => c)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();

                    char mostFrequentChar = mostFrequentCharGroup?.Key ?? '-';

                    // Check if this character is the consensus (i.e., appears in all valid sequences at this position)
                    bool isConsensus = mostFrequentCharGroup?.Count() == positionChars.Length;

                    // Determine if there is disagreement at this position (i.e., more than one unique character)
                    bool isDifferent = positionChars.Distinct().Count() > 1;

                    // Set the consensus character for this position
                    consensusChar = mostFrequentChar;

                    // Add details for this position
                    consensusDetails.Add((i, mostFrequentChar, isConsensus, isDifferent));
                }

                // Assign the consensus character to the consensus sequence
                consensusSequence[i] = consensusChar;
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
        public static (List<(char Char, bool IsFromReference, bool IsDifferent)>, List<char>, List<char>, double) BuildConsensus(List<Alignment> sequencesToAlign, string referenceSequence)
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
            List<char> consensusWithTemplateChars = new List<char>();
            List<char> consensusWithGaps = new List<char>();
            int referenceLength = referenceSequence.Length;
            HashSet<int> coveredPositions = new HashSet<int>(); // Usado para calcular a cobertura

            for (int i = 0; i < maxLength; i++)
            {
                var column = new List<char>();
                bool fromReferenceOnly = false;

                // Adiciona o caractere da sequência de referência na posição atual
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
                        int refIndex = seq.StartPositions.Min() - 1 + pos; // Posição na referência

                        if (charToAdd != '-' && refIndex >= 0 && refIndex < referenceLength)
                        {
                            column.Add(charToAdd);
                            fromReferenceOnly = false;
                            coveredPositions.Add(refIndex); // ✅ Correto: Usa 'refIndex' para mapear corretamente a referência
                        }
                    }

                }

                char consensusChar = column.GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
                bool isDifferent = column.Any(c => c != consensusChar && c != '-');
                consensusSequence.Add((consensusChar, fromReferenceOnly, isDifferent));

                // Adiciona o caractere correspondente para as duas versões de consenso
                consensusWithTemplateChars.Add(fromReferenceOnly ? referenceSequence[i] : consensusChar);
                consensusWithGaps.Add(fromReferenceOnly ? '-' : consensusChar);
            }

            // Calcula a cobertura baseada nas posições cobertas e o comprimento da sequência de referência
            double overallCoverage = (double)coveredPositions.Count / referenceLength * 100;

            return (consensusSequence, consensusWithTemplateChars, consensusWithGaps, overallCoverage);
        }

        public static void SaveConsensusToFile(string referenceSequence, List<char> consensusWithTemplateChars, List<char> consensusWithGaps, string id, string description)
        {
            string path = Path.Combine("..", "..", "..", "Debug", "local_consensus_log.txt");
            StringBuilder consensusString = new StringBuilder();

            // Montar o conteúdo que será salvo
            consensusString.AppendLine($"ID: {id} - Description: {description}");
            consensusString.AppendLine("Reference Sequence:");
            consensusString.AppendLine(referenceSequence);

            // Salvando o consenso com as letras do template
            consensusString.AppendLine("Consensus with Template Characters:");
            foreach (var consensusChar in consensusWithTemplateChars)
            {
                consensusString.Append(consensusChar);
            }
            consensusString.AppendLine();

            // Salvando o consenso com gaps nos lugares não preenchidos
            consensusString.AppendLine("Consensus with Gaps:");
            foreach (var consensusChar in consensusWithGaps)
            {
                consensusString.Append(consensusChar);
            }
            consensusString.AppendLine();

            // Verificar se o conteúdo já existe no arquivo
            if (File.Exists(path) && File.ReadAllText(path).Contains(consensusString.ToString()))
            {
                // Se o conteúdo já existir, não faça nada
                return;
            }

            // Append o novo conteúdo ao arquivo
            File.AppendAllText(path, consensusString.ToString());
        }



    }

}


