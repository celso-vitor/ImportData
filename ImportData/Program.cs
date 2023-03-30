using SequenceAssemblerLogic.ResultParser;
using System;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

namespace SequenceAssemblerLogic
{
    class Program
    {
        static void Main(string[] args)
        {
            var denovoFile = @"C:\Users\Celso Vitor\OneDrive\Documentos\Projeto Mestrado\BSA\F_2 - 20200710_BSA_HCD\20200710_BSA_HCD.raw.denovo.csv";

            NovorParser sequenceAssembler = new ();

            var psmFile = @"C:\Users\Celso Vitor\OneDrive\Documentos\Projeto Mestrado\BSA\F_2 - 20200710_BSA_HCD\20200710_BSA_HCD.raw.psms.csv";
        }
    }
}