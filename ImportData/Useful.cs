using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic
{
    public static class Useful
    {
        public static List<(string ID, int Gain)> GenerateOrderedGains(Dictionary<string, List<string>> listOfLists)
        {
            // Identifica a lista com o maior número de sequências
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

            // Encontra as sequências que não estão presentes na maior lista
            List<(string ID, int Gain)> sequenceGains = new List<(string ID, int Gain)>();

            foreach (var entry in listOfLists)
            {
                if (entry.Key != nameOfLargestList)
                {
                    var complementary = entry.Value.Except(largestList).ToList();
                    sequenceGains.Add((entry.Key, complementary.Count));
                }
            }

            // Ordena a lista de ganhos
            sequenceGains.Sort((x, y) => y.Gain.CompareTo(x.Gain));

            return sequenceGains;
        }
    }
}
