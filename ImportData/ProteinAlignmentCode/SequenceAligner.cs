using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ProteinAlignmentCode
{
    public class SequenceAligner
    {
        public Dictionary<string, int> SubstitutionMatrix { get; set; }
        public int MaxGaps { get; set; }
        public int GapPenalty { get; set; }
        public bool IgnoreILDifference { get; set; }

        public SequenceAligner(int maxGaps = 1, int gapPenalty = -2, bool ignoreILDifference = true) 
        {
            MaxGaps = maxGaps;
            GapPenalty = gapPenalty;
            IgnoreILDifference = ignoreILDifference;
            InitializeSubstitutionMatrix();
        }


        void InitializeSubstitutionMatrix()
        {
            SubstitutionMatrix = new Dictionary<string, int>();
            string[] lines = new string[]
            {
        "A R N D C Q E G H I L K M F P S T W Y V B Z J X U *",
        "A 6 -7 -4 -3 -6 -4 -2 -2 -7 -5 -6 -7 -5 -8 -2 0 -1 -13 -8 -2 -7 -6 0 0 0 -17",
        "R -7 8 -6 -10 -8 -2 -9 -9 -2 -5 -7 0 -4 -9 -4 -3 -6 -2 -10 -8 5 -1 0 0 0 -17",
        "N -4 -6 8 2 -11 -3 -2 -3 0 -5 -6 -1 -9 -9 -6 0 -2 -8 -4 -8 -4 -2 0 0 0 -17",
        "D -3 -10 2 8 -14 -2 2 -3 -4 -7 -10 -4 -11 -15 -8 -4 -5 -15 -11 -8 -7 -3 0 0 0 -17",
        "C -6 -8 -11 -14 10 -14 -14 -9 -7 -6 -11 -14 -13 -13 -8 -3 -8 -15 -4 -6 -11 -14 0 0 0 -17",
        "Q -4 -2 -3 -2 -14 8 1 -7 1 -8 -7 -3 -4 -13 -3 -5 -5 -13 -12 -7 -3 4 0 0 0 -17",
        "E -2 -9 -2 2 -14 1 8 -4 -5 -5 -7 -4 -7 -14 -5 -4 -6 -17 -8 -6 -7 -2 0 0 0 -17",
        "G -2 -9 -3 -3 -9 -7 -4 6 -9 -11 -11 -7 -8 -9 -6 -2 -6 -15 -14 -5 -8 -7 0 0 0 -17",
        "H -7 -2 0 -4 -7 1 -5 -9 9 -9 -8 -6 -10 -6 -4 -6 -7 -7 -3 -6 -4 -3 0 0 0 -17",
        "I -5 -5 -5 -7 -6 -8 -5 -11 -9 8 5 -6 -1 -2 -8 -7 -2 -14 -6 2 -6 -7 0 0 0 -17",
        "L -6 -7 -6 -10 -11 -7 -7 -11 -8 5 5 -7 0 -3 -8 -8 -5 -10 -7 0 -7 -7 0 0 0 -17",
        "K -7 0 -1 -4 -14 -3 -4 -7 -6 -6 -7 7 -2 -14 -6 -4 -3 -12 -9 -9 5 4 0 0 0 -17",
        "M -5 -4 -9 -11 -13 -4 -7 -8 -10 -1 0 -2 11 -4 -8 -5 -4 -13 -11 -1 -3 -3 0 0 0 -17",
        "F -8 -9 -9 -15 -13 -13 -14 -9 -6 -2 -3 -14 -4 9 -10 -6 -9 -4 2 -8 -12 -14 0 0 0 -17",
        "P -2 -4 -6 -8 -8 -3 -5 -6 -4 -8 -8 -6 -8 -10 8 -2 -4 -14 -13 -6 -5 -5 0 0 0 -17",
        "S 0 -3 0 -4 -3 -5 -4 -2 -6 -7 -8 -4 -5 -6 -2 6 0 -5 -7 -6 -4 -5 0 0 0 -17",
        "T -1 -6 -2 -5 -8 -5 -6 -6 -7 -2 -5 -3 -4 -9 -4 0 7 -13 -6 -3 -5 -4 0 0 0 -17",
        "W -13 -2 -8 -15 -15 -13 -17 -15 -7 -14 -10 -12 -13 -4 -14 -5 -13 13 -5 -15 -7 -13 0 0 0 -17",
        "Y -8 -10 -4 -11 -4 -12 -8 -14 -3 -6 -7 -9 -11 2 -13 -7 -6 -5 10 -7 -10 -11 0 0 0 -17",
        "V -2 -8 -8 -8 -6 -7 -6 -5 -6 2 0 -9 -1 -8 -6 -6 -3 -15 -7 7 -9 -8 0 0 0 -17",
        "B -7 5 -4 -7 -11 -3 -7 -8 -4 -6 -7 5 -3 -12 -5 -4 -5 -7 -10 -9 5 1 0 0 0 -17",
        "Z -6 -1 -2 -3 -14 4 -2 -7 -3 -7 -7 4 -3 -14 -5 -5 -4 -13 -11 -8 1 4 0 0 0 -17",
        "J 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -17",
        "X 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -17",
        "U 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 -17",
        "* -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 -17 1"
            };

            string[] headers = lines[0].Split(' ');

            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split(' ');
                char rowChar = values[0][0];

                for (int j = 1; j < values.Length; j++)
                {
                    char colChar = headers[j - 1][0];
                    int score = int.Parse(values[j]);
                    string key1 = rowChar.ToString() + colChar.ToString();
                    string key2 = colChar.ToString() + rowChar.ToString();

                    if (!SubstitutionMatrix.ContainsKey(key1))
                    {
                        SubstitutionMatrix.Add(key1, score);
                    }

                    if (!SubstitutionMatrix.ContainsKey(key2))
                    {
                        SubstitutionMatrix.Add(key2, score);
                    }
                }
            }
        }


        int GetSubstitutionScore(char a, char b)
        {
            return SubstitutionMatrix[a.ToString() + b.ToString()];
        }

        double GetMaximumSimilarity(string sequence)
        {
            double maxValue = 0;

            for (int i = 0; i < sequence.Length; i++)
            {
                maxValue += SubstitutionMatrix[sequence[i].ToString() + sequence[i].ToString()];
            }

            return maxValue;

        }

        public Alignment AlignSequences(string largeSeq, string smallSeq)
        {
            int largeLen = largeSeq.Length;
            int smallLen = smallSeq.Length;
            int[,] dp = new int[largeLen + 1, smallLen + 1];
            int maxScore = 0;
            int endLarge = 0;
            int endSmall = 0;
            int matches = 0;
            int gapsUsed = 0;
            int similarityScore = 0;

            for (int i = 0; i <= largeLen; i++) dp[i, 0] = 0;
            for (int j = 0; j <= smallLen; j++) dp[0, j] = 0;

            for (int i = 1; i <= largeLen; i++)
            {
                for (int j = 1; j <= smallLen; j++)
                {
                    int substitutionScore = GetSubstitutionScore(largeSeq[i - 1], smallSeq[j - 1]);
                    int match = dp[i - 1, j - 1] + substitutionScore;
                    int delete = dp[i - 1, j] + GapPenalty;
                    int insert = dp[i, j - 1] + GapPenalty;
                    dp[i, j] = Math.Max(0, Math.Max(match, Math.Max(delete, insert)));

                    if (dp[i, j] > maxScore)
                    {
                        maxScore = dp[i, j];
                        endLarge = i;
                        endSmall = j;
                    }
                }
            }

            string alignedSmall = "";
            string alignedLarge = "";
            int startLarge = endLarge;

            while (endLarge > 0 && endSmall > 0 && gapsUsed <= MaxGaps)
            {
                int substitutionScore = GetSubstitutionScore(largeSeq[endLarge - 1], smallSeq[endSmall - 1]);
                if (dp[endLarge, endSmall] == dp[endLarge - 1, endSmall - 1] + substitutionScore)
                {
                    alignedSmall = smallSeq[endSmall - 1] + alignedSmall;
                    alignedLarge = largeSeq[endLarge - 1] + alignedLarge;
                    similarityScore += substitutionScore;
                    if (substitutionScore > 0) matches++;
                    endLarge--;
                    endSmall--;
                }
                else if (dp[endLarge, endSmall] == dp[endLarge - 1, endSmall] + GapPenalty)
                {
                    alignedSmall = "-" + alignedSmall;
                    alignedLarge = largeSeq[endLarge - 1] + alignedLarge;
                    endLarge--;
                    gapsUsed++;
                }
                else
                {
                    alignedSmall = smallSeq[endSmall - 1] + alignedSmall;
                    alignedLarge = "-" + alignedLarge;
                    endSmall--;
                    gapsUsed++;
                }
            }

            string alignedPortion = alignedLarge.Replace("-", "");
            List<int> startPositions = new List<int>();
            int index = 0;
            while ((index = largeSeq.IndexOf(alignedPortion, index)) != -1)
            {
                startPositions.Add(index + 1);
                index++;
            }

            double identityScore = (double)matches / alignedSmall.Length * 100;



            return new SequenceAssemblerLogic.ProteinAlignmentCode.Alignment
            {
                AlignedLargeSequence = alignedLarge,
                AlignedSmallSequence = alignedSmall,
                StartPositions = startPositions,
                IdentityScore = Math.Round(identityScore),
                GapsUsed = gapsUsed,
                SimilarityScore = similarityScore,
                NormalizedSimilarity = Math.Round((similarityScore / GetMaximumSimilarity(smallSeq)) * 100),
                AlignedAA = alignedSmall.Count(a => a != '-'),
                NormalizedAlignedAA = Math.Round(((double)alignedSmall.Count(a => a != '-') / (double)alignedSmall.Length) * 100)
            };
        }

      
    }
}

