using SequenceAssemblerLogic.ProteinAlignmentCode;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.Tools
{
    public class Fasta
    {
        public string ID { get; set; }
        public string Description { get; set; }
        public string Sequence { get; set; }
       public List<Alignment> Alignments { get; set; }

    }


    public class FastaParser
    {
        public List<Fasta> MyItems { get; private set; }

        public FastaParser()
        {
            MyItems = new List<Fasta>();
        }

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

                    // Capture the entire line after '>' as both ID and Description
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

        public static void LoadFasta(FastaParser fastaFileParser, StreamReader reader)
        {
            List<Fasta> sequences = new List<Fasta>();
            Fasta currentSequence = null;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(">"))
                {
                    if (currentSequence != null)
                    {
                        sequences.Add(currentSequence);
                    }

                    // Capture the entire line after '>' as both ID and Description
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

            fastaFileParser.MyItems.AddRange(sequences); // Adiciona as sequências à propriedade MyItems
        }
    }



    public class FastaFormat
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

        // Índice formato Fasta - Assembly
        public static string ReadFastaSequence(string fastaSequence)
        {
            int startIndex = fastaSequence.IndexOf('>');
            string sequence = fastaSequence.Substring(startIndex + 1);
            sequence = sequence.Replace("\r", "").Replace("\n", "");
            return sequence;
        }

        // Leitura Fasta para contigs - Assembly
        public static Dictionary<string, string> ReadContigs(string fastaContigs)
        {
            string[] contigEntries = fastaContigs.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, string> contigs = new Dictionary<string, string>();
            int contigCount = 1;
            foreach (string contigEntry in contigEntries)
            {
                string contigId = $"Contig{contigCount}";
                StringBuilder sequence = new StringBuilder();
                string[] lines = contigEntry.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    sequence.Append(line.Trim());
                }
                if (sequence.Length > 0)
                {
                    contigs.Add(contigId, sequence.ToString());
                    contigCount++;
                }

            }

            return contigs;
        }
    }
}
