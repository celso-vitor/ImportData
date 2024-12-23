using SequenceAssemblerLogic.ResultParser;
using System.Text.RegularExpressions;


namespace SequenceAssemblerLogic.Tools
{
    public class DeNovoTagExtractor
    {
        // Method to clean the peptide by removing any text in parentheses
        public static string CleanPeptide(string peptide)
        {
            return Regex.Replace(peptide, @"\([^)]*\)", "");
        }

        // Method to convert a DeNovo registry into tags
        public static List<IDResult> DeNovoRegistryToTags(IDResult registry, int minLength, int maxLength, int minConfidence)
        {
            // Aplica o filtro com base nos parâmetros
            List<(string PeptideSequence, List<int> Scores)> tagPrecursors = FilterLocalConfidence(registry.Peptide, registry.AaScore, minConfidence, minLength, maxLength);

            if (tagPrecursors.Count == 0)
            {
                return new List<IDResult>();
            }
            else
            {
                List<IDResult> tags = new();

                // Cria os resultados baseados nos peptídeos filtrados
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
                        Score = registry.Score,  // O score geral (não o local)
                        Peptide = pt.PeptideSequence,  // A sequência filtrada
                        AaScore = pt.Scores,  // Os scores locais filtrados
                        File = registry.File
                    };

                    tags.Add(tag);

                }

                return tags;
            }
        
        }
        // Method to filter sequences based on Local Confidence (AaScore) and a minimum length of valid amino acids
        public static List<(string PeptideSequence, List<int> Scores)> FilterLocalConfidence(string sequence, List<int> scores, int minConfidence, int minLength, int maxLength)
        {
            // Extrair blocos da sequência (aminoácidos)
            List<string> blocks = ExtractBlocks(sequence);

            List<(string PeptideSequence, List<int> Scores)> validPeptides = new();
            string currentPeptide = "";
            List<int> localScores = new List<int>();

            // Iterar pelos blocos e seus scores, aplicando o filtro de confiança local
            for (int i = 0; i < blocks.Count && i < scores.Count; i++)
            {
                // Se o score de confiança for maior ou igual ao valor mínimo, adiciona o bloco (aminoácido) à sequência atual
                if (scores[i] >= minConfidence)
                {
                    currentPeptide += blocks[i]; // Adicionar o aminoácido atual à sequência filtrada
                    localScores.Add(scores[i]);  // Adicionar o score atual à lista de scores locais
                }
                else
                {
                    // Usar o método CleanPeptide para remover os conteúdos entre parênteses ao verificar o comprimento
                    if (CleanPeptide(currentPeptide).Length >= minLength && CleanPeptide(currentPeptide).Length <= maxLength)
                    {

                        validPeptides.Add((currentPeptide, localScores)); // Adiciona o peptídeo válido
                    }
                    // Reiniciar a sequência se a confiança local não atingir o valor mínimo
                    currentPeptide = "";
                    localScores = new List<int>();
                }
            }

            // Após a iteração, verificar se há uma sequência restante que atenda ao critério de comprimento
            if (CleanPeptide(currentPeptide).Length >= minLength && CleanPeptide(currentPeptide).Length <= maxLength)
            {

                validPeptides.Add((currentPeptide, localScores));
            }


            return validPeptides;
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
