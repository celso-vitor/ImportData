using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public class Fasta
    {
        public string ID { get; set; }
        public string Description { get; set; }
        public string Sequence { get; set; }
    }


    public static class FastaParser
    {
        public static List<Fasta> ParseFastaFile(string filePath)
        {
            List<Fasta> sequences = new List<Fasta>();
            Fasta currentSequence = null;

            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith(">"))
                {
                    if (currentSequence != null)
                    {
                        sequences.Add(currentSequence);
                    }

                    var parts = line.Substring(1).Split(new[] { ' ' }, 2);  // Split the line on the first space
                    currentSequence = new Fasta
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

    public static class FastaFormat
    {
        //Add fasta file 
        public static List<Fasta> LoadFasta(string fileName)
        {
            List<Fasta> MyFasta = new List<Fasta>();

            string line;
            string id = null;
            string description = null;

            Fasta f = new Fasta();

            using (var reader = new StreamReader(fileName))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                    {
                        //do nothing
                    }
                    else if (line.StartsWith(">"))
                    {

                        string[] parts = line.Substring(1).Split(new[] { ' ' }, 2); // Remove '>' and split
                        if (parts.Length >= 2)
                        {

                            id = parts[0];
                            description = parts[1];
                            MyFasta.Add(new Fasta { ID = id, Description = description });
                        }

                    }
                    else
                    {
                        MyFasta.Last().Sequence += line;
                    }
                }
            }

            return MyFasta;

        }
    }
}
