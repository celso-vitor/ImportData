using SequenceAssemblerLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

public class Program
{
    private static void Main()
    {
        Console.WriteLine("Por favor, insira o caminho para o arquivo FASTA:");

        string clustaloPath = System.IO.Path.GetTempPath() + "clustalo.exe";
        Console.WriteLine(System.IO.Path.GetTempPath());
        if (!File.Exists(clustaloPath))
        {
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "clustalo.exe", SequenceAssemblerLogic.Properties.Resources.clustalo);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "libgcc_s_sjlj-1.dll", SequenceAssemblerLogic.Properties.Resources.libgcc_s_sjlj_1);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "libgomp-1.dll", SequenceAssemblerLogic.Properties.Resources.libgomp_1);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "libstdc++-6.dll", SequenceAssemblerLogic.Properties.Resources.libstdc___6);
            File.WriteAllBytes(System.IO.Path.GetTempPath() + "pthreadGC2-w64.dll", SequenceAssemblerLogic.Properties.Resources.pthreadGC2_w64);
        }
        string input = Console.ReadLine();
        string output = "output.txt";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = clustaloPath,
            Arguments = $"-i \"{input}\" -o \"{output}\" --force",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            Process process = new Process { StartInfo = startInfo };
            process.Start();

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (string.IsNullOrEmpty(standardOutput) && string.IsNullOrEmpty(standardError))
            {
                Console.WriteLine("Nenhuma saída padrão ou erro foi gerado pelo processo.");
            }
            else
            {
                Console.WriteLine(standardOutput);
                if (!string.IsNullOrEmpty(standardError))
                {
                    Console.Error.WriteLine(standardError);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Uma exceção ocorreu: {ex.Message}");
        }

        if (File.Exists(output))
        {
            string outputFileContent = File.ReadAllText(output);
            Console.WriteLine("Conteúdo do arquivo de saída:");
            Console.WriteLine(outputFileContent);
        }
        else
        {
            Console.WriteLine("Arquivo de saída não encontrado.");
        }

    }
}
    





    
       
       