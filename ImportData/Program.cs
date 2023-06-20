using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;


namespace SequenceAssemblerLogic.ResultParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Cria uma lista de sequências de DNA
            List<string> sequences = new List<string>
        {
            "ACGTYRGTPLKLNPR",
            "ACGTYRGTPLMNPS",
            "TYRGTPLSLMNPS",
            "TYRGTPLMNPS",
            "TYRLMN"
        };

            // Chama o método IdentifyContigs passando as sequências como argumento
            IdentifyContigs(sequences);
        }

        static void IdentifyContigs(List<string> sequences)
        {
            // Cria uma lista vazia para armazenar os contigs
            List<string> contigs = new List<string>();

            // Cria uma string vazia para armazenar as regiões sobrepostas
            string overlapRegions = "";

            // Itera sobre cada sequência da lista de sequências
            foreach (string sequence in sequences)
            {
                // Adiciona a sequência à lista de contigs
                contigs.Add(sequence);
            }

            // Percorre os contigs para identificar as regiões sobrepostas
            for (int i = 0; i < contigs.Count - 1; i++)
            {
                // Cria uma string vazia para armazenar a sobreposição
                string overlap = "";

                // Percorre os contigs a partir do próximo contig atual
                for (int j = i + 1; j < contigs.Count; j++)
                {
                    // Verifica se o contig atual contém o contig seguinte
                    if (contigs[i].Contains(contigs[j]))
                    {
                        // O contig atual contém o contig seguinte, armazena o contig atual como sobreposição
                        overlap = contigs[i];
                        break;
                    }
                    // Verifica se o contig seguinte contém o contig atual
                    else if (contigs[j].Contains(contigs[i]))
                    {
                        // O contig seguinte contém o contig atual, armazena o contig seguinte como sobreposição
                        overlap = contigs[j];
                        break;
                    }
                }

                // Adiciona a sobreposição à string que armazena as regiões sobrepostas
                overlapRegions += overlap;
            }

            // Concatena todas as sequências de contigs e as regiões sobrepostas em uma única sequência final
            string finalSequence = string.Concat(contigs) + overlapRegions;

            // Imprime as sequências originais
            Console.WriteLine("Sequências:");
            foreach (string sequence in sequences)
            {
                Console.WriteLine(sequence);
            }

            // Imprime as regiões sobrepostas encontradas
            Console.WriteLine("\nRegiões sobrepostas:");
            Console.WriteLine(overlapRegions);
        }
    }
}