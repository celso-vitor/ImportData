using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public class FASTA
    {
        public string ID { get; set; }
        public string Description { get; set; }
        public string Sequence { get; set; }
    }


    public static class FastaParser
    {
        public static List<FASTA> ParseFastaFile(string filePath)
        {
            List<FASTA> sequences = new List<FASTA>();
            FASTA currentSequence = null;

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith(">"))
                {
                    if (currentSequence != null)
                    {
                        sequences.Add(currentSequence);
                    }

                    var parts = line.Substring(1).Split(new[] { ' ' }, 2);  // Split the line on the first space
                    currentSequence = new FASTA
                    {
                        ID = parts[0],
                        Description = parts.Length > 1 ? parts[1] : string.Empty,
                        Sequence = string.Empty
                    };
                }
                else if (currentSequence != null)
                {
                    currentSequence.Sequence += line.Trim();
                }
            }

            // Add last sequence
            if (currentSequence != null)
            {
                sequences.Add(currentSequence);
            }

            return sequences;

        }
       
    }
}