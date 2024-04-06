using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.AssemblyTools
{
    public class AssemblyParameters
    {
        public string GenerateAssemblyText(string referenceSequence, Dictionary<string, string> alignedContigs, List<int> startPositions)
        {
            // Cria uma sequência com o mesmo tamanho da sequência de referência, preenchida com hífens (representando gaps)
            StringBuilder assembly = new StringBuilder(new string('-', referenceSequence.Length));

            // Itera sobre cada contig alinhado
            foreach (var contigPair in alignedContigs)
            {
                string contigKey = contigPair.Key;
                string contigSequence = contigPair.Value;

                // Obter a posição de início do contig atual (presumindo que a chave do contig corresponde ao índice na lista startPositions)
                int startPosition = startPositions[int.Parse(contigKey.Replace("Contig", "")) - 1];

                // Adiciona o contig na sequência de montagem na posição correta
                int positionIndex = startPosition - 1; // Convertendo para índice base-0

                for (int i = 0; i < contigSequence.Length; i++)
                {
                    // Apenas adiciona o contig se a posição atual não for um gap
                    if (contigSequence[i] != '-')
                    {
                        assembly[positionIndex + i] = contigSequence[i];
                    }
                }
            }
            // Retorna a sequência de montagem como uma string
            return assembly.ToString();


        }
        //Ajusta as posições reconhecendo os gaps
        public int GetCorrectStartPosition(string alignedRef, string alignedContig, string fullRef)
        {
            int firstNonGapIndex = alignedContig.IndexOf(alignedContig.TrimStart('-').First());

            string nonGapContigStart = alignedContig.Substring(firstNonGapIndex).Replace("-", "");

            int startPosition = -1;

            for (int i = 0; i <= fullRef.Length - nonGapContigStart.Length; i++)
            {
                bool matchFound = true;
                for (int j = 0; j < nonGapContigStart.Length; j++)
                {
                    if (alignedRef[firstNonGapIndex + j] != '-')
                    {
                        if (fullRef[i + j] != alignedRef[firstNonGapIndex + j])
                        {
                            matchFound = false;
                            break;
                        }
                    }
                }

                if (matchFound)
                {
                    startPosition = i - firstNonGapIndex;  // Corrigido para subtrair os índices dos gaps iniciais
                    break;
                }
            }

            if (startPosition == -1)
            {
                throw new InvalidOperationException("Não foi possível encontrar uma posição de início válida para o contig na sequência de referência.");
            }

            // Ajustar para a contagem começar em 1 se o seu sistema espera indexação base-1
            return startPosition + 1;
        }
    }
}
