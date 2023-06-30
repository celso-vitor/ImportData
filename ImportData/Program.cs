using SequenceAssemblerLogic;
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{


    public static void Main()
    {
        // Inicializa uma lista de sequências de strings
        List<string> sequences = new List<string>()
        {
            //"ABCDEFGHI",
            "GHIJKLMN",
            "MNOPQESTUVXZ",
            "ZZZ",
            "DEFGH",
            "FGHBBBJ",
            "AAVCASF",
            "IJK",
            "JKM",
            "TUVA",
            "CASFZZ"
        };


        ContigAssembler sa = new ContigAssembler();
        var contigs2 = sa.AssembleContigSequences(sequences, 2);

        // Imprime os contigs resultantes na saída do console
        Console.WriteLine("Contigs:");
        foreach (var contig in contigs2)
        {
            Console.WriteLine(contig);
        }
    }

    // Método para montar contigs a partir de uma lista de sequências
    
}