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

            //    List<string>  peptides = new List<string>() { "ABCDEFGHI", "GHIJKLMN", "MNOPQESTUVXZ", "ZZZ", "DEFGH", "EGHBBBJ", "AJKHJDDDD" };

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


            //            List<string> items = new List<string>()
            //{
            //    "ABCDEFGHI",
            //    "GHIJKLMN",
            //    "LMNGNHA",
            //    "NHaCCC"

            //};

            //            List<string> similarSequences = FindSimilarSequences(items);

            //            Console.WriteLine("Similar Sequences:");
            //            foreach (string sequence in similarSequences)
            //            {
            //                Console.WriteLine(sequence);
            //            }
            //        }

            //        static List<string> FindSimilarSequences(List<string> items)
            //        {
            //            List<string> similarSequences = new List<string>();

            //            for (int i = 0; i < items.Count - 1; i++)
            //            {
            //                string currentSequence = items[i];
            //                string nextSequence = items[i + 1];

            //                List<string> commonSubsequences = FindCommonSubsequences(currentSequence, nextSequence);

            //                similarSequences.AddRange(commonSubsequences);
            //            }

            //            return similarSequences;
            //        }

            //        static List<string> FindCommonSubsequences(string str1, string str2)
            //        {
            //            int[,] dp = new int[str1.Length + 1, str2.Length + 1];

            //            for (int i = 1; i <= str1.Length; i++)
            //            {
            //                for (int j = 1; j <= str2.Length; j++)
            //                {
            //                    if (Char.ToLower(str1[i - 1]) == Char.ToLower(str2[j - 1]))
            //                    {
            //                        dp[i, j] = dp[i - 1, j - 1] + 1;
            //                    }
            //                    else
            //                    {
            //                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
            //                    }
            //                }
            //            }

            //            List<string> commonSubsequences = new List<string>();
            //            FindCommonSubsequencesHelper(str1, str2, dp, str1.Length, str2.Length, "", commonSubsequences);

            //            return commonSubsequences;
            //        }

            //        static void FindCommonSubsequencesHelper(string str1, string str2, int[,] dp, int i, int j, string sequence, List<string> commonSubsequences)
            //        {
            //            if (i == 0 || j == 0)
            //            {
            //                commonSubsequences.Add(sequence);
            //                return;
            //            }

            //            if (Char.ToLower(str1[i - 1]) == Char.ToLower(str2[j - 1]))
            //            {
            //                FindCommonSubsequencesHelper(str1, str2, dp, i - 1, j - 1, str1[i - 1] + sequence, commonSubsequences);
            //            }
            //            else
            //            {
            //                if (dp[i - 1, j] >= dp[i, j - 1])
            //                {
            //                    FindCommonSubsequencesHelper(str1, str2, dp, i - 1, j, sequence, commonSubsequences);
            //                }

            //                if (dp[i, j - 1] >= dp[i - 1, j])
            //                {
            //                    FindCommonSubsequencesHelper(str1, str2, dp, i, j - 1, sequence, commonSubsequences);
            //                }
            //            }

            //        }
            //    }


            ////List<string> peptides = new List<string>() { "ABCDEFGHI", "GHIJKLMN", "MNOPQESTUVXZ", "ZZZ", "DEFGH", "EGHBBBJ", "AJKHJDDDD" };

            ////List<string> commonParts = new List<string>();

            ////for (int i = 0; i < peptides.Count - 1; i++)
            ////{
            ////    for (int j = i + 1; j < peptides.Count; j++)
            ////    {
            ////        string commonPart = FindCommonPart(peptides[i], peptides[j]);
            ////        if (!string.IsNullOrEmpty(commonPart) && commonPart.Length >= 2)
            ////        {
            ////            commonParts.Add($"Common part found between '{peptides[i]}' and '{peptides[j]}': {commonPart}");
            ////        }
            ////    }
            ////}

            ////if (commonParts.Count > 0)
            ////{
            ////    Console.WriteLine("Common parts found:");

            ////    foreach (string part in commonParts)
            ////    {
            ////        Console.WriteLine(part);
            ////    }
            ////}
            ////else
            ////{
            ////    Console.WriteLine("No common parts found.");
            ////}

            ////// Helper method to find the common part between two strings
            ////string FindCommonPart(string str1, string str2)
            ////{
            ////    int minLength = Math.Min(str1.Length, str2.Length);

            ////    for (int i = 0; i < minLength; i++)
            ////    {
            ////        for (int j = i + 2; j <= minLength; j++)
            ////        {
            ////            string substr = str1.Substring(i, j - i);
            ////            if (str2.Contains(substr))
            ////            {
            ////                return substr;
            ////            }
            ////        }
            ////    }

            ////    return "";

            List<string> peptides = new List<string>()
{
    "ABCDEFGHI",
    "GHIJKLMN",
    "MNOPQESTUVXZ",
    "ZZZ",
    "DEFGH",
    "FGHBBBJ",
    "AJKHJDDDD"
};

            List<string> commonParts = new List<string>();
            List<string> similarSequences = FindSimilarSequences(peptides);

            foreach (string sequence in similarSequences)
            {
                string[] pair = sequence.Split(',');
                string peptide1 = peptides[int.Parse(pair[0])];
                string peptide2 = peptides[int.Parse(pair[1])];

                string commonPart = FindCommonPart(peptide1, peptide2);
                if (!string.IsNullOrEmpty(commonPart) && commonPart.Length >= 2)
                {
                    commonParts.Add(commonPart);
                }
            }

            if (commonParts.Count > 0)
            {
                Console.WriteLine("Common parts found:");
                foreach (string part in commonParts)
                {
                    Console.WriteLine($"Common part: {part}");
                    Console.WriteLine("Sequences containing the common part:");
                    foreach (string peptide in peptides)
                    {
                        if (peptide.Contains(part))
                        {
                            Console.WriteLine(peptide);
                        }
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No common parts found.");
            }

            List<string> FindSimilarSequences(List<string> items)
            {
                List<string> similarSequences = new List<string>();

                for (int i = 0; i < items.Count - 1; i++)
                {
                    string currentSequence = items[i];
                    string nextSequence = items[i + 1];

                    List<string> commonSubsequences = FindCommonSubsequences(currentSequence, nextSequence);

                    foreach (string subsequence in commonSubsequences)
                    {
                        int currentIndex = items.IndexOf(currentSequence);
                        int nextIndex = items.IndexOf(nextSequence);
                        similarSequences.Add($"{currentIndex},{nextIndex}");
                    }
                }

                return similarSequences;
            }

            List<string> FindCommonSubsequences(string str1, string str2)
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

            void FindCommonSubsequencesHelper(string str1, string str2, int[,] dp, int i, int j, string sequence, List<string> commonSubsequences)
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

            string FindCommonPart(string str1, string str2)
            {
                int minLength = Math.Min(str1.Length, str2.Length);
                string commonPart = "";

                for (int i = 0; i < minLength; i++)
                {
                    for (int j = i + 2; j <= minLength; j++)
                    {
                        string substr = str1.Substring(i, j - i);
                        if (str2.Contains(substr) && substr.Length > commonPart.Length)
                        {
                            commonPart = substr;
                        }
                    }
                }

                return commonPart;
            }


        }
    }
}
