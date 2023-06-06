using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

namespace SequenceAssemblerLogic.ResultParser
{
    class Program
    {
        static void Main(string[] args)
        {

            //    List<string>  peptides = new List<string>() { "ABCDEFGHI", "GHIJKLMN", "MNOPQESTUVXZ", "ZZZ", "DFEGH", "EGHBBBJ", "AJKHJDDDD" };

            //    Console.WriteLine(peptides[0]);
            //    peptides.ForEach(",");

            //foreach (string peptide in peptides)
            //{
            //    string overlappingLetters = "";

            //    foreach (KeyValuePair<string, List<string>> entry in partsByLetters)
            //    {
            //        string existingLetters = entry.Key;
            //        List<string> existingParts = entry.Value;


            //        foreach (char letter in existingLetters)
            //        {
            //            if (peptide.Contains(letter.ToString()))
            //            {
            //                overlappingLetters += letter;
            //            }
            //        }

            //        if (!string.IsNullOrEmpty(overlappingLetters))
            //        {
            //            existingParts.Add(peptide);
            //            if (!overlappingParts.Contains(existingLetters))
            //            {
            //                overlappingParts.Add(existingLetters);
            //            }
            //            break;
            //        }
            //    }

            //    if (string.IsNullOrEmpty(overlappingLetters))
            //    {

            //        char[] sortedChars = peptide.ToCharArray();
            //        Array.Sort(sortedChars);
            //        string sortedString = new string(sortedChars);

            //        if (!partsByLetters.ContainsKey(sortedString))
            //        {
            //            partsByLetters.Add(sortedString, new List<string> { peptide });
            //        }
            //    }
            //}

            //foreach (string overlappingPart in overlappingParts)
            //{
            //    Console.WriteLine($"Letters: {peptides}");
            //    Console.WriteLine("Overlapping Parts:");

            //    foreach (string part in partsByLetters[overlappingPart])
            //    {
            //        string formattedPart = part.PadLeft(part.IndexOf(overlappingPart) + overlappingPart.Length);
            //        Console.WriteLine(formattedPart);
            //    }

            //    Console.WriteLine();
            //}

            static void Main()
            {

                List<string> items = new List<string>()
    {
        "ABCDEFGHI",
        "GHIJKLMN",
        "LMNGNHA",
        "NHaCCC"

    };

                List<string> similarSequences = FindSimilarSequences(items);

                Console.WriteLine("Similar Sequences:");
                foreach (string sequence in similarSequences)
                {
                    Console.WriteLine(sequence);
                }
            }

            static List<string> FindSimilarSequences(List<string> items)
            {
                List<string> similarSequences = new List<string>();

                for (int i = 0; i < items.Count - 1; i++)
                {
                    string currentSequence = items[i];
                    string nextSequence = items[i + 1];

                    List<string> commonSubsequences = FindCommonSubsequences(currentSequence, nextSequence);

                    similarSequences.AddRange(commonSubsequences);
                }

                return similarSequences;
            }

            static List<string> FindCommonSubsequences(string str1, string str2)
            {
                int[,] dp = new int[str1.Length + 1, str2.Length + 1];

                for (int i = 1; i <= str1.Length; i++)
                {
                    for (int j = 1; j <= str2.Length; j++)
                    {
                        if (Char.ToLower(str1[i - 1]) == Char.ToLower(str2[j - 1]))
                        {
                            dp[i, j] = dp[i - 1, j - 1] + 1;
                        }
                        else
                        {
                            dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                        }
                    }
                }

                List<string> commonSubsequences = new List<string>();
                FindCommonSubsequencesHelper(str1, str2, dp, str1.Length, str2.Length, "", commonSubsequences);

                return commonSubsequences;
            }

            static void FindCommonSubsequencesHelper(string str1, string str2, int[,] dp, int i, int j, string sequence, List<string> commonSubsequences)
            {
                if (i == 0 || j == 0)
                {
                    commonSubsequences.Add(sequence);
                    return;
                }

                if (Char.ToLower(str1[i - 1]) == Char.ToLower(str2[j - 1]))
                {
                    FindCommonSubsequencesHelper(str1, str2, dp, i - 1, j - 1, str1[i - 1] + sequence, commonSubsequences);
                }
                else
                {
                    if (dp[i - 1, j] >= dp[i, j - 1])
                    {
                        FindCommonSubsequencesHelper(str1, str2, dp, i - 1, j, sequence, commonSubsequences);
                    }

                    if (dp[i, j - 1] >= dp[i - 1, j])
                    {
                        FindCommonSubsequencesHelper(str1, str2, dp, i, j - 1, sequence, commonSubsequences);
                    }
                }

            }
        }
    }
}

