using SequenceAssemblerLogic;
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    public static void Main()
    {
        string clustaloPath = System.IO.Path.GetTempPath() + "clustalo.exe";
        Console.WriteLine(System.IO.Path.GetTempPath());
        if(!File.Exists(clustaloPath))
        {
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "clustalo.exe", SequenceAssemblerLogic.Properties.Resources.clustalo);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "libgcc_s_sjlj-1.dll", SequenceAssemblerLogic.Properties.Resources.libgcc_s_sjlj_1);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "libgomp-1.dll", SequenceAssemblerLogic.Properties.Resources.libgomp_1);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "libstdc++-6.dll", SequenceAssemblerLogic.Properties.Resources.libstdc___6);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "pthreadGC2-w64.dll", SequenceAssemblerLogic.Properties.Resources.pthreadGC2_w64);
        }
        string input = "sequence.fasta"; //Série de sequencias 
        string output = "output.txt";
        //como chamar o metodo envoque no c# para executar o arquivo de entrada e gerar o arquivo de saída
    }
}