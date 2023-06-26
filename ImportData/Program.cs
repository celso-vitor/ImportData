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
            "bbJklLL",
            "AAVCASF"
        };

        // Chama o método AssembleContigs com a lista de sequências
        var contigs = AssembleContigs(sequences);

        // Imprime os contigs resultantes na saída do console
        Console.WriteLine("Contigs:");
        foreach (var contig in contigs)
        {
            Console.WriteLine(contig);
        }
    }

    // Método para montar contigs a partir de uma lista de sequências
    public static List<string> AssembleContigs(List<string> sequences)
    {
        // Lista para armazenar os contigs resultantes
        var contigs = new List<string>();

        // Continua enquanto houver sequências para processar
        while (sequences.Count > 0)
        {
            // Inicializa o contig atual com a primeira sequência da lista
            var currentContig = sequences[0];
            sequences.RemoveAt(0);

            // Loop infinito até encontrar uma sequência que não se sobreponha
            while (true)
            {
                var overlapIndex = -1;
                var overlapSize = 0;
                var overlapString = "";

                // Encontra a sequência com a maior sobreposição
                for (int i = 0; i < sequences.Count; i++)
                {
                    // Obtém o tamanho da sobreposição
                    var size = GetOverlapLength(currentContig, sequences[i]);
                    // Se a sobreposição for maior que a sobreposição encontrada anteriormente,
                    // atualiza o índice e tamanho da sobreposição
                    if (size > overlapSize)
                    {
                        overlapSize = size;
                        overlapIndex = i;
                        overlapString = sequences[i].Substring(size);
                    }
                }

                // Se uma sequência sobreposta foi encontrada, anexe-a ao contig atual
                if (overlapIndex != -1)
                {
                    currentContig += overlapString;
                    sequences.RemoveAt(overlapIndex);
                }
                else
                {
                    // Se não houver mais sobreposições, saia do loop
                    break;
                }
            }

            // Adiciona o contig atual à lista de contigs
            contigs.Add(currentContig);
        }

        // Retorna a lista de contigs montados
        return contigs;
    }

    // Método para calcular o tamanho da sobreposição entre duas sequências
    public static int GetOverlapLength(string seq1, string seq2)
    {
        // Converte as sequências para maiúsculas para tornar a comparação insensível a maiúsculas/minúsculas
        seq1 = seq1.ToUpper();
        seq2 = seq2.ToUpper();

        // Calcula o comprimento mínimo das duas sequências
        int length = Math.Min(seq1.Length, seq2.Length);

        // Procura a maior sobreposição do final de seq1 com o início de seq2
        for (int i = length; i > 0; i--)
        {
            if (seq1.EndsWith(seq2.Substring(0, i)))
            {
                return i;
            }
        }

        // Se não houver sobreposição, retorna 0
        return 0;
    }
}