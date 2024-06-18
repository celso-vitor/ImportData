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

    }
}
