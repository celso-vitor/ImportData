using ImportData;
using System;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

class Program
{

    static void Main(string[] args)
    {
        var denovoFile = @"C:\Users\Celso Vitor\OneDrive\Documentos\Projeto Mestrado\BSA\F_2 - 20200710_BSA_HCD\20200710_BSA_HCD.raw.denovo.csv";

        SequenceAssembler sequenceAssembler = new SequenceAssembler();

        sequenceAssembler.LoadDeNovoRegistries(denovoFile);

        Console.WriteLine("DeNovoRestries: " + sequenceAssembler.MyDeNovoRegistries.Count);

        var psmFile = @"C:\Users\Celso Vitor\OneDrive\Documentos\Projeto Mestrado\BSA\F_2 - 20200710_BSA_HCD\20200710_BSA_HCD.raw.psms.csv";

        sequenceAssembler.LoadPsmRegistries(psmFile);

        Console.WriteLine("PsmRestries: " + sequenceAssembler.MyPsmRegistries.Count);

    }
}