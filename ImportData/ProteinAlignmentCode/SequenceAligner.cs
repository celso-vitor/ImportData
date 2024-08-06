using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public bool IgnoreDifference { get; set; }



        public SequenceAligner(int maxGaps = 1, int gapPenalty = -2, bool ignoreDifference = true)
        {
            MaxGaps = maxGaps;
            GapPenalty = gapPenalty;
            IgnoreDifference = ignoreDifference;
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

            // Initialize substitution matrix with scores from provided data
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

                    // Add each score to the dictionary, ensuring no duplicates
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

        public int GetSubstitutionScore(char a, char b)
        {
            if (IgnoreDifference)
            {
                if ((a == 'I' && b == 'L') || (a == 'L' && b == 'I') ||
                    (a == 'V' && (b == 'I' || b == 'L')) || (b == 'V' && (a == 'I' || a == 'L')))
                {
                    return SubstitutionMatrix.TryGetValue("ILV", out int ilvScore) ? ilvScore : 0;
                }
                else if ((a == 'M' && b == 'I') || (a == 'I' && b == 'M'))
                {
                    return SubstitutionMatrix.TryGetValue("MI", out int miScore) ? miScore : 0;
                }
                else if ((a == 'M' && b == 'L') || (a == 'L' && b == 'M'))
                {
                    return SubstitutionMatrix.TryGetValue("ML", out int mlScore) ? mlScore : 0;
                }
                else if ((a == 'F' && b == 'Y') || (a == 'Y' && b == 'F'))
                {
                    return SubstitutionMatrix.TryGetValue("FY", out int fyScore) ? fyScore : 0;
                }
                else if ((a == 'N' && b == 'D') || (a == 'D' && b == 'N'))
                {
                    return SubstitutionMatrix.TryGetValue("ND", out int ndScore) ? ndScore : 0;
                }
                else if ((a == 'E' && b == 'D') || (a == 'D' && b == 'E'))
                {
                    return SubstitutionMatrix.TryGetValue("ED", out int edScore) ? edScore : 0;
                }
                else if ((a == 'S' && b == 'T') || (a == 'T' && b == 'S'))
                {
                    return SubstitutionMatrix.TryGetValue("ST", out int stScore) ? stScore : 0;
                }
                else if ((a == 'K' && b == 'R') || (a == 'R' && b == 'K'))
                {
                    return SubstitutionMatrix.TryGetValue("KR", out int krScore) ? krScore : 0;
                }
                else if ((a == 'Q' && b == 'N') || (a == 'N' && b == 'Q'))
                {
                    return SubstitutionMatrix.TryGetValue("QN", out int qnScore) ? qnScore : 0;
                }
                else if ((a == 'C' && b == 'S') || (a == 'S' && b == 'C'))
                {
                    return SubstitutionMatrix.TryGetValue("CS", out int csScore) ? csScore : 0;
                }
                else if ((a == 'A' && b == 'V') || (a == 'V' && b == 'A') ||
                         (a == 'A' && b == 'L') || (a == 'L' && b == 'A') ||
                         (a == 'A' && b == 'I') || (a == 'I' && b == 'A') ||
                         (a == 'V' && b == 'L') || (a == 'L' && b == 'V') ||
                         (a == 'I' && b == 'L') || (a == 'L' && b == 'I') ||
                         (a == 'V' && b == 'I') || (a == 'I' && b == 'V'))
                {
                    return SubstitutionMatrix.TryGetValue("ALIV", out int alivScore) ? alivScore : 0;
                }
            }
            if (SubstitutionMatrix.TryGetValue($"{a}{b}", out int defaultScore))
            {
                return defaultScore;
            }
            return -1;
        }




        double GetMaximumSimilarity(string sequence)
        {
            double maxValue = 0;

            foreach (char aminoAcid in sequence)
            {
                string key = aminoAcid.ToString() + aminoAcid.ToString();

                if (SubstitutionMatrix.ContainsKey(key))
                {
                    maxValue += SubstitutionMatrix[key];
                }
            }

            return maxValue;
        }


        public void DisplayAlignment(Alignment alignment)
        {
            Console.WriteLine("Alinhamento:");
            Console.WriteLine("Consensus Aligned:      " + new string(alignment.AlignedLargeSequence));
            Console.WriteLine("Small Sequence Aligned: " + new string(alignment.AlignedSmallSequence));
            Console.WriteLine("Identity:               " + alignment.Identity);
            Console.WriteLine("Similarity Score:       " + alignment.SimilarityScore);
            Console.WriteLine("Gaps Used:              " + alignment.GapsUsed);
            Console.WriteLine("Normalized Identity:    " + alignment.NormalizedIdentityScore);
            Console.WriteLine("Normalized Similarity:  " + alignment.NormalizedSimilarity);
            Console.WriteLine("Aligned Amino Acids:    " + alignment.AlignedAA);
            Console.WriteLine("Normalized Aligned AA:  " + alignment.NormalizedAlignedAA);
        }


        public static Alignment AlignerPCC1(List<char>[] consensus, string smallSequence)
        {
            Dictionary<(char, char), int> substitutionScoreCache = new Dictionary<(char, char), int>();

            List<int> scores = new List<int>();
            List<int> startPositions = new List<int>();
            int totalMatches = 0;
            int totalAlignedPositions = 0;
            int totalSimilarities = 0;
            string alignedLargeSequence = "";
            int maxScore = int.MinValue;
            int bestStartIndex = 0;

            StringBuilder logBuilder = new StringBuilder(); // Para registro de log
            SequenceAligner aligner = new SequenceAligner();

            for (int i = 0; i < consensus.Length - smallSequence.Length + 1; i++) // Ajuste no limite para evitar gaps
            {
                int score = 0;
                int currentTotalMatches = 0;
                int currentTotalSimilarities = 0;
                int currentAlignedPositions = 0;
                StringBuilder tempLargeSequenceBuilder = new StringBuilder();

                logBuilder.AppendLine($"Alignment starting at position {i}:");

                for (int j = 0; j < smallSequence.Length; j++)
                {
                    List<int> tmpScore = new List<int>();
                    foreach (var cons in consensus[i + j])
                    {
                        // Usar o dicionário para armazenar e recuperar os scores de substituição
                        int substitutionScore;
                        if (!substitutionScoreCache.TryGetValue((cons, smallSequence[j]), out substitutionScore))
                        {
                            substitutionScore = aligner.GetSubstitutionScore(cons, smallSequence[j]);
                            substitutionScoreCache[(cons, smallSequence[j])] = substitutionScore;
                        }
                        tmpScore.Add(substitutionScore);
                    }

                    int maxTmpScore = tmpScore.Max();
                    score += maxTmpScore;

                    if (maxTmpScore > 0)
                    {
                        tempLargeSequenceBuilder.Append(consensus[i + j][tmpScore.IndexOf(maxTmpScore)]);
                        currentTotalMatches++;
                        currentTotalSimilarities += maxTmpScore;
                    }
                    else
                    {
                        // Adicionar a letra do consenso caso não haja correspondência direta
                        tempLargeSequenceBuilder.Append(consensus[i + j][0]);
                    }
                    currentAlignedPositions++;

                    logBuilder.AppendLine($"Pos {j}: consensus {consensus[i + j][0]} vs smallSeq {smallSequence[j]} => score {maxTmpScore}");
                }

                scores.Add(score);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestStartIndex = i;
                    alignedLargeSequence = tempLargeSequenceBuilder.ToString();
                    totalMatches = currentTotalMatches;
                    totalSimilarities = currentTotalSimilarities;
                    totalAlignedPositions = currentAlignedPositions;
                }

                logBuilder.AppendLine($"Score: {score}, Matches: {currentTotalMatches}, Similarities: {currentTotalSimilarities}, Aligned positions: {currentAlignedPositions}");
            }

            startPositions.Add(bestStartIndex);

            // Calcular Identidade e Métricas Normalizadas
            int matchedIdentity = 0;
            int alignedAA = 0;
            for (int i = 0; i < alignedLargeSequence.Length; i++)
            {
                if (alignedLargeSequence[i] == smallSequence[i])
                {
                    alignedAA++;
                    matchedIdentity++;
                }
            }

            // Máxima similaridade possível para a smallSequence
            double maxSimilarity = aligner.GetMaximumSimilarity(smallSequence);

            double identity = (double)matchedIdentity / smallSequence.Length * 100;
            double normalizedIdentityScore = (double)matchedIdentity / totalAlignedPositions * 100;
            double similarityScore = totalSimilarities;
            double normalizedSimilarity = (similarityScore / maxSimilarity) * 100;
            double alignedAANormalized = (double)alignedAA / smallSequence.Length * 100;
            double normalizedAlignedAA = (double)alignedAA / totalAlignedPositions * 100;

            Alignment aln = new Alignment()
            {
                StartPositions = startPositions,
                Identity = matchedIdentity,
                NormalizedIdentityScore = Math.Round(normalizedIdentityScore, 2),
                SimilarityScore = similarityScore,
                NormalizedSimilarity = Math.Round(normalizedSimilarity, 2),
                AlignedAA = alignedAA,
                NormalizedAlignedAA = Math.Round(normalizedAlignedAA, 2),
                AlignedLargeSequence = alignedLargeSequence,
                AlignedSmallSequence = smallSequence,
                GapsUsed = 0 // Gaps não utilizados
            };

            // Salvar o log em um arquivo
            string logFilePath = Path.Combine("..", "..", "..", "Debug", "alignment_details.log");
            File.AppendAllText(logFilePath, logBuilder.ToString());

            return aln;
        }


        public Alignment AlignerMSA(List<char>[] consensus, string smallSequence, string sourceOrigin)
        {
            Dictionary<(char, char), int> substitutionScoreCache = new Dictionary<(char, char), int>();

            List<int> scores = new List<int>();
            List<int> startPositions = new List<int>();
            int totalMatches = 0;
            int totalAlignedPositions = 0;
            int totalSimilarities = 0;
            string alignedLargeSequence = "";
            int maxScore = int.MinValue;
            int bestStartIndex = 0;

            for (int i = 0; i < consensus.Length - smallSequence.Length + 1; i++) // Ajuste no limite para evitar gaps
            {
                int score = 0;
                int currentTotalMatches = 0;
                int currentTotalSimilarities = 0;
                int currentAlignedPositions = 0;
                StringBuilder tempLargeSequenceBuilder = new StringBuilder();

                for (int j = 0; j < smallSequence.Length; j++)
                {
                    List<int> tmpScore = new List<int>();
                    foreach (var cons in consensus[i + j])
                    {
                        // Usar o dicionário para armazenar e recuperar os scores de substituição
                        int substitutionScore;
                        if (!substitutionScoreCache.TryGetValue((cons, smallSequence[j]), out substitutionScore))
                        {
                            substitutionScore = GetSubstitutionScore(cons, smallSequence[j]);
                            substitutionScoreCache[(cons, smallSequence[j])] = substitutionScore;
                        }
                        tmpScore.Add(substitutionScore);
                    }

                    int maxTmpScore = tmpScore.Max();
                    score += maxTmpScore;

                    if (maxTmpScore > 0)
                    {
                        tempLargeSequenceBuilder.Append(consensus[i + j][tmpScore.IndexOf(maxTmpScore)]);
                        currentTotalMatches++;
                        currentTotalSimilarities += maxTmpScore;
                    }
                    else
                    {
                        // Adicionar a letra do consenso caso não haja correspondência direta
                        tempLargeSequenceBuilder.Append(consensus[i + j][0]);
                    }
                    currentAlignedPositions++;
                }

                scores.Add(score);
                if (score > maxScore)
                {
                    maxScore = score;
                    bestStartIndex = i;
                    alignedLargeSequence = tempLargeSequenceBuilder.ToString();
                    totalMatches = currentTotalMatches;
                    totalSimilarities = currentTotalSimilarities;
                    totalAlignedPositions = currentAlignedPositions;
                }
            }

            startPositions.Add(bestStartIndex);

            // Calcular Identidade e Métricas Normalizadas
            int matchedIdentity = 0;
            int alignedAA = 0;
            for (int i = 0; i < alignedLargeSequence.Length; i++)
            {
                if (alignedLargeSequence[i] == smallSequence[i])
                {
                    alignedAA++;
                    matchedIdentity++;
                }
            }

            // Máxima similaridade possível para a smallSequence
            double maxSimilarity = GetMaximumSimilarity(smallSequence);

            double identity = (double)matchedIdentity / smallSequence.Length * 100;
            double normalizedIdentityScore = (double)matchedIdentity / totalAlignedPositions * 100;
            double similarityScore = totalSimilarities;
            double normalizedSimilarity = (similarityScore / maxSimilarity) * 100;
            double alignedAANormalized = (double)alignedAA / smallSequence.Length * 100;
            double normalizedAlignedAA = (double)alignedAA / totalAlignedPositions * 100;

            Alignment aln = new Alignment()
            {
                SourceOrigin = sourceOrigin,
                StartPositions = startPositions,
                Identity = matchedIdentity,
                NormalizedIdentityScore = Math.Round(normalizedIdentityScore, 2),
                SimilarityScore = similarityScore,
                NormalizedSimilarity = Math.Round(normalizedSimilarity, 2),
                AlignedAA = alignedAA,
                NormalizedAlignedAA = Math.Round(normalizedAlignedAA, 2),
                AlignedLargeSequence = alignedLargeSequence,
                AlignedSmallSequence = smallSequence,
                GapsUsed = 0 // Gaps não utilizados
            };

            return aln;
        }





        // Local Alignment
        public Alignment AlignerLocal(string largeSeq, string smallSeq, string sourceOrigin)
        {
            int largeLen = largeSeq.Length;
            int smallLen = smallSeq.Length;
            int[,] dp = new int[largeLen + 1, smallLen + 1];
            int maxScore = 0;
            int endLarge = 0;
            int endSmall = 0;
            int matchesSimilarity = 0;
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

            while (endLarge > 0 && endSmall > 0 && gapsUsed < MaxGaps)
            {
                int substitutionScore = GetSubstitutionScore(largeSeq[endLarge - 1], smallSeq[endSmall - 1]);
                if (dp[endLarge, endSmall] == dp[endLarge - 1, endSmall - 1] + substitutionScore)
                {
                    alignedSmall = smallSeq[endSmall - 1] + alignedSmall;
                    alignedLarge = largeSeq[endLarge - 1] + alignedLarge;
                    similarityScore += substitutionScore;
                    if (substitutionScore > 0) matchesSimilarity++;
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

            // Remove initial gaps and record how many were removed
            int initialGaps = 0;
            while (alignedLarge.Length > initialGaps && alignedLarge[initialGaps] == '-')
            {
                initialGaps++;
            }
            string alignedPortion = alignedLarge.Substring(initialGaps).Replace("-", "");

            // If alignedPortion is empty, log the issue and handle it gracefully
            if (string.IsNullOrEmpty(alignedPortion))
            {
                Console.WriteLine("Aligned portion of the sequence is empty. Skipping alignment.");
                return null; // Or handle it in another way, such as returning a special value or throwing a specific exception
            }

            // Search for the start positions in the large sequence, adjusting for the gaps removed
            List<int> startPositions = new List<int>();
            int index = 0;
            while ((index = largeSeq.IndexOf(alignedPortion, index)) != -1)
            {
                int adjustedIndex = index + 1 - initialGaps;
                if (adjustedIndex >= 0 && adjustedIndex < largeSeq.Length)
                {
                    startPositions.Add(adjustedIndex); // Adjusting the index for the initial gaps
                }
                index++;
            }

            // Calculate identity
            int matchedIdentity = 0;
            int alignedAA = 0;
            for (int i = 0; i < alignedLarge.Length; i++)
            {
                if (alignedLarge[i] != '-' && alignedSmall[i] != '-')
                {
                    alignedAA++;
                    if (alignedLarge[i] == alignedSmall[i])
                    {
                        matchedIdentity++;
                    }
                }
            }

            double normalizedIdentityScore = (double)matchedIdentity / alignedSmall.Length * 100;

            return new SequenceAssemblerLogic.ProteinAlignmentCode.Alignment
            {
                SourceOrigin = sourceOrigin,
                Identity = matchedIdentity,
                AlignedLargeSequence = alignedLarge,
                AlignedSmallSequence = alignedSmall,
                StartPositions = startPositions,
                NormalizedIdentityScore = Math.Round(normalizedIdentityScore),
                GapsUsed = gapsUsed,
                SimilarityScore = similarityScore,
                NormalizedSimilarity = Math.Round((similarityScore / GetMaximumSimilarity(smallSeq)) * 100),
                AlignedAA = alignedAA,
                NormalizedAlignedAA = Math.Round(((double)alignedAA / alignedSmall.Length) * 100),
            };
        }

    }
}
