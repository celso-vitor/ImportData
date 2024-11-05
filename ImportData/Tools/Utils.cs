using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.ResultParser;

namespace SequenceAssemblerLogic.Tools
{
    public static class Utils
    {
        public static List<Alignment> EliminateDuplicatesAndSubsequences(List<Alignment> input)
        {
            // Ordena os alinhamentos pela extensão da AlignedSmallSequence.
            input.Sort((x, y) => x.AlignedSmallSequence.Length.CompareTo(y.AlignedSmallSequence.Length));

            var result = new List<Alignment>();
            var set = new HashSet<string>();

            foreach (var alignment in input)
            {
                string alignedSequence = alignment.AlignedSmallSequence;
                string startPositionString = alignment.StartPositionsString;

                if (set.Contains(alignedSequence))
                    continue;

                bool shouldAdd = true;

                foreach (var existingAlignment in result)
                {
                    string existingSequence = existingAlignment.AlignedSmallSequence;
                    string existingStartPositionString = existingAlignment.StartPositionsString;

                    // Verifica se a posição inicial é a mesma antes de comparar as sequências
                    if (startPositionString == existingStartPositionString)
                    {
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


        public static List<(string folder, string sequence, string identificationMethod)> GetSourceOrigins(List<string> filteredSequences, Dictionary<string, List<IDResult>> deNovoDictTemp, Dictionary<string, List<IDResult>> psmDictTemp)
        {
            List<(string folder, string sequence, string identificationMethod)> sourceOrigins = new();

            foreach (var seq in filteredSequences)
            {
                if (deNovoDictTemp.Values.SelectMany(v => v).Any(item => item.CleanPeptide == seq))
                {
                    var peptideorigin = deNovoDictTemp.Values.SelectMany(v => v).First(item => item.CleanPeptide == seq).Peptide;
                    var folder = deNovoDictTemp.Keys.First(key => deNovoDictTemp[key].Any(item => item.CleanPeptide == seq));

                    // Add Peptide and Folder as source Origins
                    sourceOrigins.Add((folder, peptideorigin, "DeNovo"));
                }
                else if (psmDictTemp.Values.SelectMany(v => v).Any(item => item.CleanPeptide == seq))
                {
                    var peptideorigin = psmDictTemp.Values.SelectMany(v => v).First(item => item.CleanPeptide == seq).Peptide;
                    var folder = psmDictTemp.Keys.First(key => psmDictTemp[key].Any(item => item.CleanPeptide == seq));

                    // Add Peptide and Folder as source Origins
                    sourceOrigins.Add((folder, peptideorigin, "PSM"));
                }
                else
                {
                    // Defines a default origin if it is not found in deNovoDictTemp or psmDictTemp
                    throw new Exception("Problems parsing peptide results");
                }
            }

            return sourceOrigins;
        }


    }
}
