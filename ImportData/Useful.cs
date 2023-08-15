using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public static class Useful
    {
        //Add fasta file 
        public static List<FASTA> LoadFasta(string fileName)
        {
            List<FASTA> MyFasta = new List<FASTA>();

            string line;
            string id = null;
            string description = null;

            FASTA f = new FASTA();

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
                            MyFasta.Add(new FASTA { ID = id, Description = description });
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
        public static List<(string ID, int Gain)> GenerateOrderedGains(Dictionary<string, List<string>> listOfLists)
        {
            // Identifies the list with the highest number of sequences
            string nameOfLargestList = "";
            int maxCount = 0;

            foreach (var entry in listOfLists)
            {
                if (entry.Value.Count > maxCount)
                {
                    maxCount = entry.Value.Count;
                    nameOfLargestList = entry.Key;
                }
            }

            List<string> largestList = listOfLists[nameOfLargestList];

            // Find strings that are not present in the largest list
            List<(string ID, int Gain)> sequenceGains = new List<(string ID, int Gain)>();

            foreach (var entry in listOfLists)
            {
                if (entry.Key != nameOfLargestList)
                {
                    var complementary = entry.Value.Except(largestList).ToList();
                    sequenceGains.Add((entry.Key, complementary.Count));
                }
            }

            // Sort the gain list
            sequenceGains.Sort((x, y) => y.Gain.CompareTo(x.Gain));

            return sequenceGains;
        }

        public static string ContigsToFastaFormat(List<Contig> contigs)
        {
            StringBuilder fastaFormat = new StringBuilder();
            int counter = 1;

            foreach (Contig contig in contigs)
            {
                fastaFormat.AppendLine($">Contig_{counter}");
                fastaFormat.AppendLine(contig.Sequence);
                counter++;
            }

            return fastaFormat.ToString();
            string contigsInFastaFormat = ContigsToFastaFormat(contigs);
        }


    }
}
