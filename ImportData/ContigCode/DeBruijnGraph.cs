using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ContigCode
{
    public class DeBruijnGraph
    {
        private Dictionary<string, List<string>> adjacencyList;

        public DeBruijnGraph(IEnumerable<string> kmers)
        {
            adjacencyList = new Dictionary<string, List<string>>();
            BuildGraph(kmers);
        }

        private void BuildGraph(IEnumerable<string> kmers)
        {
            foreach (var kmer in kmers)
            {
                var prefix = kmer.Substring(0, kmer.Length - 1);
                var suffix = kmer.Substring(1);

                if (!adjacencyList.ContainsKey(prefix))
                    adjacencyList[prefix] = new List<string>();

                adjacencyList[prefix].Add(suffix);
            }
        }

        public List<string> GetEulerianPath()
        {
            var stack = new Stack<string>();
            var path = new List<string>();
            var current = adjacencyList.Keys.First();

            while (stack.Count > 0 || (adjacencyList.ContainsKey(current) && adjacencyList[current].Count > 0))
            {
                if (!adjacencyList.ContainsKey(current) || adjacencyList[current].Count == 0)
                {
                    path.Add(current);
                    if (stack.Count > 0)
                    {
                        current = stack.Pop();
                    }
                }
                else
                {
                    stack.Push(current);
                    var next = adjacencyList[current].First();
                    adjacencyList[current].Remove(next);
                    current = next;
                }
            }

            path.Add(current);
            path.Reverse();
            return path;
        }
    }

}
