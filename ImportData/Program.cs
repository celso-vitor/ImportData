using SequenceAssemblerLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;

public class Program
{
    private static void Main()
    {
        List<FASTA> alignments = FastaParser.ParseFastaFile("output.txt");
        Console.WriteLine(alignments.Count + " alignments read");

        List<(string word, int startPos)> contigPos = new();
        
        if (alignments.Count > 1)
        {
            for (int i = 1; i < alignments.Count; i++)
            {

            }
        }
        

        Console.WriteLine("Done");



    }
}
    





    
       
       