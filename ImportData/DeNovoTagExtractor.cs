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

        public static string CleanPeptide (string peptide)
        {
            return Regex.Replace(peptide, @"\([^)]*\)", "");
        }


        
        public static List<DeNovoRegistry> DeNovoRegistryToTags (DeNovoRegistry registry, int minScore, int minLength)
        {
            
            List<(string PeptideSequence, List<int> Scores)> tagPrecursors = FindValidPeptides(registry.Peptide, registry.AaScore, minScore, minLength);

            if (tagPrecursors.Count == 0)
            {
                return new List<DeNovoRegistry>();
            } 
            else
            {
                List<DeNovoRegistry> tags = new();

                foreach (var pt in tagPrecursors)
                {
                    DeNovoRegistry tag = new DeNovoRegistry()
                    {
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

                    tags.Add(tag);
                }

                return tags;
            } 
        }



        public static List<(string PeptideSequence, List<int> Scores)> FindValidPeptides(string sequence, List<int> scores, int minScore, int minLength)
        {

            List<string> blocks = ExtractBlocks(sequence);

            List<(string PeptideSequence, List<int> Scores)> validPeptides = new ();
            
            string currentPeptide = "";
            List<int> localScores = new List<int>();

            for (int i = 0; i < blocks.Count; i++)
            {
                if (scores[i] >= minScore)
                {
                    currentPeptide += blocks[i];
                    localScores.Add(scores[i]);
                }
                else
                {
                    if (currentPeptide.Length > 0)
                    {
                        validPeptides.Add((currentPeptide, localScores));
                        currentPeptide = "";
                        localScores = new List<int>();

                    }
                }
            }

            if (currentPeptide.Length > 0)
            {
                validPeptides.Add((currentPeptide, localScores));
            }

            return validPeptides.Where(a => CleanPeptide(a.PeptideSequence).Length >= minLength).ToList();

        }

public static List<string> ExtractBlocks(string input)
{
    var matches = Regex.Matches(input, @"([A-Z](?!\())|([A-Z]\([A-Za-z]+(-[A-Za-z]+)*\))");
    var list = new List<string>();

    foreach (Match match in matches)
    {
        list.Add(match.Value);
    }

    return list;
}


    }
}
