using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.ResultParser;

namespace SequenceAssemblerLogic
{
    public static class Utils
    {
        public static List<Alignment> EliminateDuplicatesAndSubsequences(List<Alignment> input)
        {
            input.Sort((x, y) => x.AlignedSmallSequence.Length.CompareTo(y.AlignedSmallSequence.Length)); // Ordenar Alignment por comprimento de AlignedSmallSequence

            var result = new List<Alignment>();
            var set = new HashSet<string>();

            foreach (var alignment in input)
            {
                string alignedSequence = alignment.AlignedSmallSequence;
                if (set.Contains(alignedSequence))
                    continue;

                bool shouldAdd = true;
                foreach (var existingAlignment in result)
                {
                    string existingSequence = existingAlignment.AlignedSmallSequence;
                    if (IsSubsequence(existingSequence, alignedSequence))
                    {
                        shouldAdd = false;
                        break;
                    }
                    else if (IsSubsequence(alignedSequence, existingSequence))
                    {
                        set.Remove(existingSequence);
                    }
                }

                if (shouldAdd)
                {
                    result.Add(alignment);
                    set.Add(alignedSequence);
                }
            }

            return result;
        }

        private static bool IsSubsequence(string str1, string str2)
        {
            int i = 0, j = 0;
            while (i < str1.Length && j < str2.Length)
            {
                if (str1[i] == str2[j])
                    i++;
                j++;
            }
            return i == str1.Length;
        }


        public static List<string> FilterSequencesByNormalizedLength(List<string> sequences, string reference, int minConsecutiveAminoAcids)
        {
            // Filter sequences based on the minimum consecutive amino acids
            List<string> filteredSequences = new List<string>();
            foreach (string sequence in sequences)
            {
                if (ContainsConsecutiveAminoAcids(sequence, reference, minConsecutiveAminoAcids))
                {
                    filteredSequences.Add(sequence);
                }
            }

            return filteredSequences;
        }

        static bool ContainsConsecutiveAminoAcids(string sequence, string reference, int minConsecutiveAminoAcids)
        {
            int consecutiveCount = 0;
            foreach (char baseChar in sequence)
            {
                if (reference.Contains(baseChar))
                {
                    consecutiveCount++;
                    if (consecutiveCount >= minConsecutiveAminoAcids)
                    {
                        return true;
                    }
                }
                else
                {
                    consecutiveCount = 0;
                }
            }
            return false;
        }

        public static List<string> GetSourceOrigins(List<string> filteredSequences, Dictionary<string, List<IDResult>> deNovoDictTemp, Dictionary<string, List<IDResult>> psmDictTemp)
        {
            List<string> sourceOrigins = new List<string>();

            foreach (var seq in filteredSequences)
            {
                if (deNovoDictTemp.Values.SelectMany(v => v).Any(item => item.CleanPeptide == seq))
                {
                    var peptideorigin = deNovoDictTemp.Values.SelectMany(v => v).First(item => item.CleanPeptide == seq).Peptide;
                    var folder = deNovoDictTemp.Keys.First(key => deNovoDictTemp[key].Any(item => item.CleanPeptide == seq));

                    // Adiciona Peptide e Folder ao sourceOrigins
                    sourceOrigins.Add($"DeNovo - Peptide: {peptideorigin} - Folder: {folder}");
                }
                else if (psmDictTemp.Values.SelectMany(v => v).Any(item => item.CleanPeptide == seq))
                {
                    var peptideorigin = psmDictTemp.Values.SelectMany(v => v).First(item => item.CleanPeptide == seq).Peptide;
                    var folder = psmDictTemp.Keys.First(key => psmDictTemp[key].Any(item => item.CleanPeptide == seq));

                    // Adiciona Peptide e Folder ao sourceOrigins
                    sourceOrigins.Add($"PSM - Peptide: {peptideorigin} - Folder: {folder}");
                }
                else
                {
                    // Define uma origem padrão, caso não seja encontrada em deNovoDictTemp nem em psmDictTemp
                    sourceOrigins.Add("Unknown");
                }
            }

            return sourceOrigins;
        }


    }
}
