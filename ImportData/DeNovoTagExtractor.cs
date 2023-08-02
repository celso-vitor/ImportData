using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public class DeNovoTagExtractor
    {
        // Method to clean the peptide by removing any text in parentheses
        public static string CleanPeptide(string peptide)
        {
            return Regex.Replace(peptide, @"\([^)]*\)", "");
        }

        // Method to convert a DeNovo registry into tags
        public static List<IDResult> DeNovoRegistryToTags(IDResult registry, int minScore, int minLength)
        {
            // Find valid peptides based on minimum score and length
            List<(string PeptideSequence, List<int> Scores)> tagPrecursors = FindValidPeptides(registry.Peptide, registry.AaScore, minScore, minLength);

            // If no valid peptides are found, return an empty list
            if (tagPrecursors.Count == 0)
            {
                return new List<IDResult>();
            }
            else
            {
                List<IDResult> tags = new();

                // For each valid peptide found, create a new IDResult with appropriate information
                foreach (var pt in tagPrecursors)
                {
                    IDResult tag = new()
                    {
                        IsPSM = false,
                        IsTag = true,
                        ScanNumber = registry.ScanNumber,
                        RT = registry.RT,
                        Mz = registry.Mz,
                        Z = registry.Z,
                        PepMass = registry.PepMass,
                        Err = registry.Err,
                        Score = registry.Score,
                        Peptide = pt.PeptideSequence,
                        AaScore = pt.Scores,
                        File = registry.File
                    };

                    // Add the new IDResult to the list of tags
                    tags.Add(tag);
                }

                // Return the list of tags
                return tags;
            }
        }

        // Method to find valid peptides based on minimum score and length
        public static List<(string PeptideSequence, List<int> Scores)> FindValidPeptides(string sequence, List<int> scores, int minScore, int minLength)
        {
            // Extract blocks from peptide sequence
            List<string> blocks = ExtractBlocks(sequence);

            List<(string PeptideSequence, List<int> Scores)> validPeptides = new();

            string currentPeptide = "";
            List<int> localScores = new List<int>();

            // Iterate through the blocks
            for (int i = 0; i < blocks.Count; i++)
            {
                // If block's score is greater than or equal to minScore, add the block to the current peptide
                if (scores[i] >= minScore)
                {
                    currentPeptide += blocks[i];
                    localScores.Add(scores[i]);
                }
                else
                {
                    // If block's score is less than minScore, check if the current peptide is not empty
                    if (currentPeptide.Length > 0)
                    {
                        // If current peptide is not empty, add it to the list of valid peptides
                        validPeptides.Add((currentPeptide, localScores));
                        currentPeptide = "";
                        localScores = new List<int>();
                    }
                }
            }

            // Check if the current peptide is not empty after the iteration ends
            if (currentPeptide.Length > 0)
            {
                // If current peptide is not empty, add it to the list of valid peptides
                validPeptides.Add((currentPeptide, localScores));
            }

            // Return only those valid peptides that have length greater than or equal to minLength
            return validPeptides.Where(a => CleanPeptide(a.PeptideSequence).Length >= minLength).ToList();
        }

        // Method to extract blocks from a peptide sequence
        public static List<string> ExtractBlocks(string input)
        {
            // Use a regular expression to find blocks in the input string
            var matches = Regex.Matches(input, @"([A-Z](?!\())|([A-Z]\([A-Za-z]+(-[A-Za-z]+)*\))");
            var list = new List<string>();

            // For each block found, add it to the list
            foreach (Match match in matches)
            {
                list.Add(match.Value);
            }

            // Return the list of blocks
            return list;
        }
    }
}
