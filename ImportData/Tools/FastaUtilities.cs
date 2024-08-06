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
    }
}
