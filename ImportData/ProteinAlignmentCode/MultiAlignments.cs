using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SequenceAssemblerLogic.ProteinAlignmentCode
{
    public class MultiAlignments
    {
        public static string PerformMultipleSequenceAlignment(Dictionary<string, string> inputSequences)
        {
            string clustalOmegaPath = @"C:\clustal-omega-1.2.2-win64\clustalo.exe";
            string inputFilePath = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(inputFilePath))
            {
                foreach (var kvp in inputSequences)
                {
                    writer.WriteLine($">{kvp.Key}");
                    writer.WriteLine(kvp.Value);
                }
            }

            string outputFilePath = Path.GetTempFileName();
            string arguments = $"-i \"{inputFilePath}\" -o \"{outputFilePath}\" --force";

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = clustalOmegaPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Erro ao executar Clustal Omega: {error}");
                    }
                    else
                    {
                        string alignmentOutput = File.ReadAllText(outputFilePath);
                        Console.WriteLine("Clustal Omega Output:");
                        Console.WriteLine(alignmentOutput);
                        return alignmentOutput;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ocorreu um erro: {ex.Message}", ex);
            }
            finally
            {
                if (File.Exists(inputFilePath))
                {
                    File.Delete(inputFilePath);
                }
                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }
            }
        }

        public static List<string> ReadSequencesFromOutput(string output)
        {
            var sequences = new List<string>();
            var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine("Processing Clustal Omega Output Lines:");
            foreach (var line in lines)
            {
                Console.WriteLine(line);
                if (!line.StartsWith(" ") && !line.StartsWith("CLUSTAL") && !line.Contains('*'))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        sequences.Add(parts[1]);
                    }
                }
            }

            Console.WriteLine("Extracted Aligned Sequences:");
            foreach (var seq in sequences)
            {
                Console.WriteLine(seq);
            }

            return sequences;
        }
        public static List<char>[] AnalyzePositions(List<string> sequences)
        {
            if (sequences == null || sequences.Count == 0)
            {
                throw new ArgumentException("A lista de sequências não pode estar vazia.");
            }

            int length = sequences[0].Length;
            foreach (var sequence in sequences)
            {
                if (sequence.Length != length)
                {
                    throw new ArgumentException("Todas as sequências devem ter o mesmo comprimento.");
                }
            }

            var positions = new List<char>[length];

            for (int i = 0; i < length; i++)
            {
                positions[i] = new List<char>();
            }

            for (int i = 0; i < length; i++)
            {
                bool hasGap = false;
                var aminoAcids = new HashSet<char>();
                foreach (var sequence in sequences)
                {
                    if (i < sequence.Length)
                    {
                        if (sequence[i] == '-')
                        {
                            hasGap = true;
                            break;
                        }
                        aminoAcids.Add(sequence[i]);
                    }
                }

                if (!hasGap)
                {
                    positions[i].AddRange(aminoAcids);
                }
            }

            return positions;
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
