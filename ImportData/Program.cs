using PatternTools.FastaTools;
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

            //// Caminhos para os arquivos de entrada e saída


            // Comando para executar Clustal Omega

            // Configurar o processo

            FastaFileParser fastaFileParser = new FastaFileParser();
            string file = Path.Combine("..", "..", "..", "Debug", "albumin.fasta");
            fastaFileParser.ParseFile(new StreamReader(file), false);

            ClustalMultiAligner clustalMultiAligner = new ClustalMultiAligner();
            (List<char>[] consensus, List<FastaItem> alignments) result = clustalMultiAligner.AlignSequences(fastaFileParser.MyItems);

            ClustalMultiAligner.DisplayPositions(result.consensus);

            // Proxima missão :: Alinhar uma sequência contra o consenso...
            //    string sequence = "LLAFS";
            //    SequenceAligner sa = new SequenceAligner();
            //    string sourceOrigin = "teste";
            //    Console.WriteLine("Sequence to align: " + sequence);
            //    var alignment = sa.AlignerPCC(result.consensus, sequence, sourceOrigin);

            //    Console.WriteLine("Alignment Results:");
            //    Console.WriteLine("Identity: " + alignment.Identity);
            //    Console.WriteLine("Aligned Large Sequence: " + alignment.AlignedLargeSequence);
            //    Console.WriteLine("Aligned Small Sequence: " + alignment.AlignedSmallSequence);
            //    Console.WriteLine("Start Positions: " + string.Join(", ", alignment.StartPositions));
            //    Console.WriteLine("Normalized Identity Score: " + alignment.NormalizedIdentityScore);
            //    Console.WriteLine("Gaps Used: " + alignment.GapsUsed);
            //    Console.WriteLine("Similarity Score: " + alignment.SimilarityScore);
            //    Console.WriteLine("Normalized Similarity: " + alignment.NormalizedSimilarity);
            //    Console.WriteLine("Aligned AA: " + alignment.AlignedAA);
            //    Console.WriteLine("Normalized Aligned AA: " + alignment.NormalizedAlignedAA);

            //    DisplayConsensus(result.consensus);

            //    Console.WriteLine("Done");
            //}

            //static void DisplayConsensus(List<char>[] consensus)
            //{
            //    for (int i = 0; i < consensus.Length; i++)
            //    {
            //        Console.Write(i + "\t");

            //        for (int j = 0; j < consensus[i].Count; j++)
            //        {
            //            Console.Write(consensus[i][j]);
            //        }

            //        Console.WriteLine();
            //    }
            //}
        }
    }
}
    


