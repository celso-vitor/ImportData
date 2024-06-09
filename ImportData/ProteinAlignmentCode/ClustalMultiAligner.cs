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
            string clustalOmegaPath = Path.Combine("..", "..", "..", "Clustal", "clustalo.exe"); //Path to the Clustal Omega executable
            //Save the Fasta Sequences to the work directory
            StreamWriter sw = new StreamWriter(inputFile);

            foreach (var fastaItem in FastaToAlign)
            {
                sw.WriteLine(">" + fastaItem.SequenceIdentifier);
                sw.WriteLine(fastaItem.Sequence);
            }

            sw.Close();


            //Configure the process
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = clustalOmegaPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            //Run the process

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                //Read process outputs
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                //Check if there were any errors
                if (process.ExitCode != 0)
                {
                    return (null, null);
                }
                else
                {
                    //Read and display file output
                    string result = File.ReadAllText(outputFile);
                    Console.WriteLine("Saída do Clustal Omega:");
                    Console.WriteLine(result);


                    //Capture all lines of output
                    var allLines = CaptureOutputLines(result);

                    //Process aligned sequences
                    var sequences = ReadSequencesFromOutput(result);
                    List<char>[] consensus = ConstructConsensus(sequences);

                    return (consensus, allLines);
                }
            }
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

        private List<char>[] ConstructConsensus(List<string> sequences)
        {
            int length = sequences[0].Length;
            var positions = new List<char>[length];

            //Initialize the positions array with empty lists
            for (int i = 0; i < length; i++)
            {
                positions[i] = new List<char>();
            }

            //Process each position in the sequences
            for (int i = 0; i < length; i++)
            {
                var aminoAcids = new HashSet<char>();

                foreach (var sequence in sequences)
                {
                    if (sequence[i] != '-')
                    {
                        aminoAcids.Add(sequence[i]); //Add amino acid to the set
                    }
                }

                //Convert the HashSet to a List and store it in the positions array
                positions[i].AddRange(aminoAcids);
            }

            return positions;
        }

        private List<FastaItem> CaptureOutputLines(string output)
        {
            List<FastaItem> fastaItems = new List<FastaItem>();
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            //Remove first and last lines
            lines.RemoveAt(0);
            lines.RemoveAt(lines.Count - 1);

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
