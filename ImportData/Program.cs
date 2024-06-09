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
            var file = Path.Combine("..", "..", "..", "Debug", "teste.fasta");
            fastaFileParser.ParseFile(new StreamReader(file), false);

            ClustalMultiAligner clustalMultiAligner = new ClustalMultiAligner();
            var result = clustalMultiAligner.AlignSequences(fastaFileParser.MyItems);


            ClustalMultiAligner.DisplayPositions(result.consensus);

            // Proxima missão :: Alinhar uma sequência contra o consenso...
            string sequence = "LFPSAYPG";
            SequenceAligner sa = new SequenceAligner();
            var alignment = sa.AlignSequences(result.consensus, sequence);

            sa.DisplayAlignment(alignment);
            Console.WriteLine("Done");

        }

    }
}
  
