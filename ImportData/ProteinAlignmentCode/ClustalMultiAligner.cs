using PatternTools.FastaTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ProteinAlignmentCode
{
    public class ClustalMultiAligner
    {


        public ClustalMultiAligner()
        {

        }

        public (List<char>[] consensus, List<FastaItem> alignments) AlignSequences(List<FastaItem> FastaToAlign)
        {
            string inputFile = Path.Combine("..", "..", "..", "Debug", "tmp.fasta");
            string outputFile = Path.Combine("..", "..", "..", "Debug", "output.aln");
            string arguments = $"-i \"{inputFile}\" -o \"{outputFile}\" --outfmt=clu --force";
            string clustalOmegaPath = Path.Combine("..", "..", "..", "Clustal", "clustalo.exe"); // Caminho para o executável do Clustal Omega

            // Save the Fasta Sequences to the work directory
            using (StreamWriter sw = new StreamWriter(inputFile))
            {
                foreach (var fastaItem in FastaToAlign)
                {
                    sw.WriteLine(">" + fastaItem.SequenceIdentifier);
                    sw.WriteLine(fastaItem.Sequence);
                }
            }

            // Configurar o processo
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = clustalOmegaPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Executar o processo
            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                // Ler as saídas do processo
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();



                process.WaitForExit();

                // Verificar se houve algum erro
                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Erro ao executar Clustal Omega:");
                    Console.WriteLine(error);
                    return (null, null);
                }
                else
                {
                    // Ler e exibir a saída do arquivo

                    return ConstructConsensus(outputFile);

                }
            }
        }

        public static (List<char>[] consensus, List<FastaItem> fasta) ConstructConsensus(string file)
        {
            // Ler e exibir a saída do arquivo
            string result = File.ReadAllText(file);

            Console.WriteLine($"{result}");


            // Capture all lines of output
            List<FastaItem> fastaSequences = CaptureOutputLines(result);

            Dictionary<string, string> concatenatedSequences = new();
            foreach (var seq in fastaSequences)
            {
                if (concatenatedSequences.ContainsKey(seq.SequenceIdentifier))
                {
                    concatenatedSequences[seq.SequenceIdentifier] += seq.Sequence;
                }
                else
                {
                    concatenatedSequences.Add(seq.SequenceIdentifier, seq.Sequence);
                }
            }

            var sequences = concatenatedSequences.Select(a => a.Value).ToList();

            if (sequences == null || sequences.Count == 0)
            {
                throw new ArgumentException("The list of sequences is null or empty.");
            }

            // Find the length of the longest sequence
            int maxLength = sequences.Max(seq => seq.Length);
            var consensus = new List<char>[maxLength];

            // Initialize the positions array with empty lists
            for (int i = 0; i < maxLength; i++)
            {
                consensus[i] = new List<char>();
            }

            // Process each position in the sequences
            for (int i = 0; i < maxLength; i++)
            {
                var aminoAcids = new HashSet<char>();

                foreach (var sequence in sequences)
                {
                    // If the current sequence is shorter than the current position, treat it as a gap
                    if (sequence.Length > i && sequence[i] != '-')
                    {
                        aminoAcids.Add(sequence[i]); // Add amino acid to the set
                    }
                }

                // Convert the HashSet to a List and store it in the positions array
                consensus[i].AddRange(aminoAcids);
            }

            return (consensus, concatenatedSequences.Select(a => new FastaItem() { SequenceIdentifier = a.Key, Sequence = a.Value }).ToList());

        }

        private List<string> ReadSequencesFromOutput(string output)
        {
            var sequences = new List<string>();
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (!line.StartsWith(" ") && !line.StartsWith("CLUSTAL") && !line.Contains('*'))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        sequences.Add(parts[1]);
                    }
                }
            }

            return sequences;
        }


        private static List<FastaItem> CaptureOutputLines(string output)
        {
            List<FastaItem> fastaItems = new List<FastaItem>();
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (var line in lines)
            {
                if (!line.StartsWith(" ") && !line.StartsWith("CLUSTAL") && !line.Contains('*'))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        string sequenceIdentifier = parts[0];
                        string sequence = parts[1];

                        FastaItem fastaItem = new FastaItem
                        {
                            SequenceIdentifier = sequenceIdentifier,
                            Sequence = sequence
                        };

                        fastaItems.Add(fastaItem);
                    }
                }
            }

            return fastaItems;
        }

        public static void DisplayPositions(List<char>[] positions)
        {
            Console.WriteLine("Posições com variações de aminoácidos:");
            for (int i = 0; i < positions.Length; i++)
            {
                if (positions[i].Count > 0)
                {
                    Console.WriteLine($"Posição {i + 1}: {string.Join(", ", positions[i])}");
                }
            }
        }

    }
}
