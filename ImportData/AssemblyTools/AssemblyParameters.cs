using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SequenceAssemblerLogic.ProteinAlignmentCode;

namespace SequenceAssemblerLogic.AssemblyTools
{
    public class AssemblyParameters
    {

        public string GenerateAssemblyText(string referenceSequence, List<string> alignedContigs, List<int> startPositions)
        {
            // Cria uma sequência com o mesmo tamanho da sequência de referência, preenchida com hífens (representando gaps)
            StringBuilder assembly = new StringBuilder(new string('-', referenceSequence.Length));

            // Assume que o número de contigs alinhados é o mesmo que o número de posições de início
            for (int contigIndex = 0; contigIndex < alignedContigs.Count; contigIndex++)
            {
                string contigSequence = alignedContigs[contigIndex];

                // Encontra o primeiro índice não-gap na sequência do contig
                int firstNonGapIndex = contigSequence.IndexOf(contigSequence.TrimStart('-').First());

                // Calcula a posição de início ajustada
                int startPosition = startPositions[contigIndex] + firstNonGapIndex - 1; // Converte para índice base-0

                // Adiciona o contig na sequência de montagem na posição correta
                for (int i = 0; i < contigSequence.Length; i++)
                {
                    if (contigSequence[i] != '-')
                    {
                        if (startPosition + i < assembly.Length)
                        {
                            assembly[startPosition + i] = contigSequence[i];
                        }
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
                throw new InvalidOperationException("Could not find a valid start position for the contig in the reference sequence");
            }

            // Ajustar para a contagem começar em 1 se o seu sistema espera indexação base-1
            return startPosition + 1;
        }
    }
}
