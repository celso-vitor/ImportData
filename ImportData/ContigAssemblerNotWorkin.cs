using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public class gContigAssemblerNotWorkin
    {
        int minOverlap;
        public gContigAssemblerNotWorkin(int MinOverlap) 
        {
            minOverlap = MinOverlap;
        }

        public List<string> AssembleContigs(List<string> sequences)
        {

            sequences = sequences.Select(a => a.ToUpper()).OrderByDescending(a => a.Length).ToList();
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
                        var overlap = GetOverlapLength(currentContig, sequences[i]);
                        // Se a sobreposição for maior que a sobreposição encontrada anteriormente,
                        // atualiza o índice e tamanho da sobreposição
                        if (overlap > overlapSize)
                        {
                            overlapSize = overlap;
                            overlapIndex = i;
                            overlapString = sequences[i].Substring(overlap);
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
        private int GetOverlapLength(string seq1, string seq2)
        {
            int length = 0;

            // loop through the first sequence
            for (int i = 0; i < seq1.Length; i++)
            {
                // loop through the second sequence
                for (int j = 0; j < seq2.Length; j++)
                {
                    int k = 0;

                    // count the number of overlapping characters
                    while (i + k < seq1.Length && j + k < seq2.Length && seq1[i + k] == seq2[j + k])
                    {
                        k++;
                    }

                    // if the current overlap is greater than the previous one, update the length
                    if (k > length)
                    {
                        length = k;
                    }
                }
            }

            return length;
        }

    }
}
