using SequenceAssemblerLogic.ProteinAlignmentCode;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SequenceAssemblerGUI
{
    public partial class CompareSequence : Window
    {
        private MainWindow _mainWindow;

        public CompareSequence(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private async void GenerateConsensusButton_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar ProgressBar
            LoadingProgressBar.Visibility = Visibility.Visible;
            LoadingProgressBar.IsIndeterminate = true;

            // Gere a sequência consenso usando Clustal
            string alignedSequences = await GenerateConsensusAsync();

            // Defina a sequência consenso na TextBoxSequenceA
            TextBoxSequenceA.Text = alignedSequences;

            // Ocultar ProgressBar
            LoadingProgressBar.Visibility = Visibility.Collapsed;
            LoadingProgressBar.IsIndeterminate = false;
        }

        public static async Task<string> GenerateConsensusAsync()
        {
            string inputFilePath = Path.Combine("..", "..", "..", "Debug", "contigs.fasta");
            string outputFilePath = Path.Combine("..", "..", "..", "Debug", "aligned.fasta");

            // Verifique se o arquivo de entrada existe
            if (!File.Exists(inputFilePath))
            {
                Console.WriteLine("Error: Input file contigs.fasta not found.");
                return null;
            }

            // Execute o Clustal para alinhar os contigs
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Path.Combine("..", "..", "..", "Clustal", "clustalo.exe"),
                Arguments = $"-i \"{inputFilePath}\" -o \"{outputFilePath}\" --force",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Console.WriteLine("Running Clustal...");
            using (Process process = Process.Start(psi))
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(error);
                    return null;
                }
            }
            Console.WriteLine($"Alignment output file created: {outputFilePath}");

            // Verificar se o arquivo de saída foi criado
            if (!File.Exists(outputFilePath))
            {
                Console.WriteLine("Error: Alignment output file not found.");
                return null;
            }

            // Ler o arquivo de saída e gerar a sequência consenso
            var alignedSequences = ReadAlignedSequences(outputFilePath);
            string consensus = GenerateConsensus(alignedSequences);

            return consensus;
        }

        static List<string> ReadAlignedSequences(string filePath)
        {
            List<string> sequences = new List<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                StringBuilder currentSequence = new StringBuilder();
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith(">"))
                    {
                        if (currentSequence.Length > 0)
                        {
                            sequences.Add(currentSequence.ToString());
                            currentSequence.Clear();
                        }
                    }
                    else
                    {
                        currentSequence.Append(line.Trim());
                    }
                }
                if (currentSequence.Length > 0)
                {
                    sequences.Add(currentSequence.ToString());
                }
            }
            return sequences;
        }

        static string GenerateConsensus(List<string> alignedSequences)
        {
            if (alignedSequences == null || alignedSequences.Count == 0)
            {
                throw new ArgumentException("No aligned sequences provided.");
            }

            int sequenceLength = alignedSequences[0].Length;
            char[] consensus = new char[sequenceLength];

            for (int i = 0; i < sequenceLength; i++)
            {
                Dictionary<char, int> residueCounts = new Dictionary<char, int>();

                foreach (string sequence in alignedSequences)
                {
                    char residue = sequence[i];
                    if (residue != '-') // Ignorar gaps
                    {
                        if (!residueCounts.ContainsKey(residue))
                        {
                            residueCounts[residue] = 0;
                        }
                        residueCounts[residue]++;
                    }
                }

                char consensusResidue = '-';
                int maxCount = 0;

                foreach (var kvp in residueCounts)
                {
                    if (kvp.Value > maxCount)
                    {
                        maxCount = kvp.Value;
                        consensusResidue = kvp.Key;
                    }
                }

                consensus[i] = consensusResidue;
            }

            return new string(consensus);
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var sequenceA = TextBoxSequenceA.Text;
            var sequenceB = TextBoxSequenceB.Text;

            if (!int.TryParse(IntegerUpDownMaxGaps.Text, out int maxGaps) || maxGaps < 0)
            {
                MessageBox.Show("Max Gaps must be a positive integer.");
                return;
            }

            if (!int.TryParse(IntegerUpDowGapPenalty.Text, out int gapPenalty))
            {
                MessageBox.Show("Gap Penalty must be an integer.");
                return;
            }

            // Call the alignment method and calculate the total number of aligned bases
            (string alignedSeqA, string alignedSeqB, int totalAlignedBases, double identityPercentage) = SequenceAligner.AlignSequences(sequenceA, sequenceB, gapPenalty);

            // Display results in TextBoxResults
            TextBoxResults.Text = $"A:\n{alignedSeqA}\n\nB:\n{alignedSeqB}\n\nTotal Aligned Bases: {totalAlignedBases}\nIdentity Percentage: {identityPercentage:P2}";
        }

        private void LoadingProgressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }
}