using SequenceAssemblerLogic.ProteinAlignmentCode;
using System;
using System.Collections.Generic;

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
            //var result = sequenceAligner.AlignSequences(large, small, sourceOrigins) ;
            //Console.WriteLine(result);
            //Console.WriteLine($"Identidade: {result.Identity}");
            //Console.WriteLine($"Sequência Alinhada (Grande): {result.AlignedLargeSequence}");
            //Console.WriteLine($"Sequência Alinhada (Pequena): {result.AlignedSmallSequence}");
            //Console.WriteLine("Position: " + string.Join(", ", result.StartPositions));

            var plResult = SeproPckg2.ResultPackage.Load(@"C:\Users\Celso Vitor Calomeno\Source\Repos\ImportData\ImportData\Test\F1.sepr2");

            foreach (var psm in plResult.MyProteins.AllPSMs)
            {
                Console.WriteLine(psm.PeptideSequence);
                Console.WriteLine(psm.FileName);
            }
            

        }

    }

}
