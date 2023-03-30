using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ResultParser
{
    public class NovorParser
    {

        public Dictionary<string, List<DeNovoRegistry>> DictDenovo { get; private set; }
        public Dictionary<string, List<PsmRegistry>> DictPsm { get; private set; }
        

        public NovorParser()
        {
            DictDenovo = new();
            DictPsm = new();
        }
        public void LoadNovorUniversal(DirectoryInfo di)
        {

            foreach (DirectoryInfo di2 in di.GetDirectories())
            {
                string[] csvFiles = Directory.GetFiles(di2.FullName, "*.csv");
                Console.WriteLine(di2.Name);


                foreach (string fileName in csvFiles)
                {
                    string firstLine = File.ReadAllLines(fileName)[0];
                    if (firstLine.StartsWith("#id"))
                    {
                        if (DictPsm.ContainsKey(di2.Name))
                        {
                            DictPsm[di2.Name].AddRange(LoadNovorPsmRegistries(fileName));
                        }
                        else
                        {
                            DictPsm.Add(di2.Name, LoadNovorPsmRegistries(fileName));
                        }

                    }
                    else
                    {
                        if (DictDenovo.ContainsKey(di2.Name))
                        {
                            DictDenovo[di2.Name].AddRange(LoadNovorDeNovoRegistries(fileName));
                        }
                        else
                        {
                            DictDenovo.Add(di2.Name, LoadNovorDeNovoRegistries(fileName));
                        }

                    }
                }

            }

            List<string> GetSubsequences(string peptide, List<int> aaConfidence, int cutoff)
            {
                List<string> subsequences = new List<string>();
                int startIndex = -1;
                int endIndex = -1;
                for (int i = 0; i < aaConfidence.Count; i++)
                {
                    if (aaConfidence[i] > cutoff)
                    {
                        if (startIndex == -1)
                        {
                            startIndex = i;
                        }
                        endIndex = i;
                    }
                    else
                    {
                        if (startIndex != -1)
                        {
                            string subsequence = peptide.Substring(startIndex, endIndex - startIndex + 1);
                            subsequences.Add(subsequence);
                            startIndex = -1;
                            endIndex = -1;
                        }
                    }
                }
                if (startIndex != -1)
                {
                    string subsequence = peptide.Substring(startIndex, endIndex - startIndex + 1);
                    subsequences.Add(subsequence);
                }
                return subsequences;
            }

            string peptide = "MAGFATVLFQY";
            List<int> aaConfidence = new List<int> { 35, 28, 12, 40, 45, 38, 25, 15, 32, 31, 29 };
            int cutoff = 31;
            Console.WriteLine(peptide);
            List<string> subsequences = GetSubsequences(peptide, aaConfidence, cutoff);
            Console.WriteLine(subsequences);

        }

        private List<DeNovoRegistry> LoadNovorDeNovoRegistries(string denovofileName)
        {
            string[] lines = File.ReadAllLines(denovofileName);
            List<DeNovoRegistry> myRegistries = new();
            for (int i = 22; i < lines.Length; i++)
            {
                string[] cols = Regex.Split(lines[i], ",");
                DeNovoRegistry deNovoRegistry = new DeNovoRegistry()
                {
                    ScanNumber = int.Parse(cols[1]),
                    RT = double.Parse(cols[2]),
                    Mz = double.Parse(cols[3]),
                    Z = int.Parse(cols[4]),
                    PepMass = double.Parse(cols[5]),
                    Err = double.Parse(cols[6]),
                    Score = double.Parse(cols[8]),
                    Peptide = Regex.Replace(cols[9], " ", ""),
                    AaScore = Regex.Split(cols[10], "-").Select(a => int.Parse(a)).ToList()
                };
                myRegistries.Add(deNovoRegistry);
            }
            return myRegistries;
        }

        private List<PsmRegistry> LoadNovorPsmRegistries(string psmfileName)
        {
            string[] line = File.ReadAllLines(psmfileName);
            List<PsmRegistry> myRegistries = new();
            for (int i = 2; i < line.Length; i++)
            {
                string[] columns = Regex.Split(line[i], ",");
                PsmRegistry PSMRegistry = new PsmRegistry()
                {
                    ScanNumber = int.Parse(columns[2]),
                    RT = double.Parse(columns[3]),
                    Mz = double.Parse(columns[4]),
                    Z = int.Parse(columns[5]),
                    PepMass = double.Parse(columns[6]),
                    Err = double.Parse(columns[7]),
                    Score = double.Parse(columns[9]),
                    Peptide = Regex.Replace(columns[14], " ", ""),
                    AaScore = Regex.Split(columns[16], "-").Select(b => int.Parse(b)).ToList()
                };
                myRegistries.Add(PSMRegistry);
            }
            return myRegistries;
        }

    }
}
