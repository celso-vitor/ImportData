using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ProteinAlignmentCode
{

    class Program
    {
        static void Main(string[] args)
        {
            //string small = "QEFVE";
            //string large = "MKWVTFISLLLLFSSAYSRGVFRRDTHKSEIAHRFKDLGEEHFKGLVLIAFSQYLQQCPFDEHVKLVNELTEFAKTCVADESHAGCEKSLHTLFGDELCKVASLRETYGDMADCCEKQEPERNECFLSHKDDSPDLPKLKPDPNTLCDEFKADEKKFWGKYLYEIARRHPYFYAPELLYYANKYNGVFQECCQAEDKGACLLPKIETMREKVLASSARQRLRCASIQKFGERALKAWSVARLSQKFPKAEFVEVTKLVTDLTKVHKECCHGDLLECADDRADLAKYICDNQDTISSKLKECCDKPLLEKSHCIAEVEKDAIPENLPPLTADFAEDKDVCKNYQEAKDAFLGSFLYEYSRRHPEYAVSVLLRLAKEYEATLEECCAKDDPHACYSTVFDKLKHLVDEPQNLIKQNCDQFEKLGEYGFQNALIVRYTRKVPQVSTPTLVEVSRSLGKVGTRCCTKPESERMPCTEDYLSLILNRLCVLHEKTPVSEKVTKCCTESLVNRRPCFSALTPDETYVPKAFDEKLFTFHADICTLPDTEKQIKKQTALVELLKHKPKATEEQLKTVMENFVAFVDKCCAADDKEACFAVEGPKLVVSTQTALA\r\n";
            //string sourceOrigins = "psm/denovo";
            //SequenceAligner sequenceAligner = new SequenceAligner();
            //var result = sequenceAligner.AlignSequences(large, small, sourceOrigins);
            //Console.WriteLine(result);
            //Console.WriteLine($"Identidade: {result.Identity}");
            //Console.WriteLine($"Sequência Alinhada (Grande): {result.AlignedLargeSequence}");
            //Console.WriteLine($"Sequência Alinhada (Pequena): {result.AlignedSmallSequence}");
            //Console.WriteLine("Position: " + string.Join(", ", result.StartPositions));

            //var plResult = SeproPckg2.ResultPackage.Load(@"C:\Users\celso\source\repos\ImportData\ImportData\Test\F1.sepr2");

            //foreach (var psm in plResult.MyProteins.AllPSMs)
            //{
            //    Console.WriteLine(psm.PeptideSequence);
            //    Console.WriteLine(psm.FileName);
            //}

            // Caminhos para os arquivos de entrada e saída
            string inputFile = "C:\\Users\\Celso Vitor Calomeno\\OneDrive - FIOCRUZ\\Projeto Mestrado\\ANALISES\\BSA\\SimpleTest\\teste.fasta";
            string outputFile = "C:\\Users\\Celso Vitor Calomeno\\OneDrive - FIOCRUZ\\Projeto Mestrado\\ANALISES\\BSA\\SimpleTest\\output.aln";

            // Comando para executar Clustal Omega
            string clustalOmegaPath = "C:\\Users\\Celso Vitor Calomeno\\OneDrive - FIOCRUZ\\Projeto Mestrado\\ANALISES\\clustal-omega-1.2.2-win64\\clustalo.exe"; // Caminho para o executável do Clustal Omega
            string arguments = $"-i \"{inputFile}\" -o \"{outputFile}\" --outfmt=clu --force";
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
            try
            {
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
                        Console.WriteLine($"Erro ao executar Clustal Omega: {error}");
                    }
                    else
                    {
                        // Ler e exibir a saída do arquivo
                        string result = File.ReadAllText(outputFile);
                        Console.WriteLine("Saída do Clustal Omega:");
                        Console.WriteLine(result);

                        // Processar as sequências alinhadas
                        var sequences = ReadSequencesFromOutput(result);
                        var positions = AnalyzePositions(sequences);

                        // Exibir as possíveis variações de aminoácidos por posição sem gaps e com diferenças
                        DisplayPositions(positions);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocorreu um erro: {ex.Message}");
            }
        }

        static List<string> ReadSequencesFromOutput(string output)
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

        static List<char>[] AnalyzePositions(List<string> sequences)
        {
            int length = sequences[0].Length;
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
                    if (sequence.Length > i)
                    {
                        if (sequence[i] == '-')
                        {
                            hasGap = true;
                            break;
                        }
                        aminoAcids.Add(sequence[i]);
                    }
                }

                if (!hasGap && aminoAcids.Count > 1)
                {
                    positions[i].AddRange(aminoAcids);
                }
            }

            return positions;
        }

        static void DisplayPositions(List<char>[] positions)
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