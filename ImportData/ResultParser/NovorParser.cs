using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

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

        public static void FilterDictMinLengthDeNovo(int peptideLength, Dictionary<string, List<DeNovoRegistry>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length < peptideLength);
            }
        }

        public static void FilterDictMaxLengthDeNovo(int peptideLength, Dictionary<string, List<DeNovoRegistry>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length > peptideLength);
            }
        }


        public static void FilterDictMinLengthPSM(int peptideLength, Dictionary<string, List<PsmRegistry>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length < peptideLength);
            }
        }

        public static void FilterDictMaxLengthPSM(int peptideLength, Dictionary<string, List<PsmRegistry>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length > peptideLength);
            }
        }

        public static void FilterSequencesByScorePSM(int minScore, Dictionary<string, List<PsmRegistry>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(seq => seq.Score < minScore);
            }

        }
        
       public static void FilterSequencesByPeptideDeNovo(string peptide, Dictionary<string, List<DeNovoRegistry>> theDict)
        {
            foreach (var kvp in theDict)
            {
                    //Iterates over all dictionary lists
                foreach (var item in kvp.Value)
                {
                    //Accesses the value of the Peptide property for each list item
                    string sequence = item.Peptide;

                    //Console.WriteLine(sequence);
                }
            }
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

        }


        public static List<string> GetSubSequences2(string peptide, List<int> scores, int cutoff, int minSize)
        {

            if (scores.Count != peptide.Length)
            {
                throw new Exception("Peptide and scores should have the same length");
            }

            List<string> results = new();
            StringBuilder subSequence = new StringBuilder();

            for (int i = 0; i < scores.Count; i++)
            {
                if (scores[i] >= cutoff)
                {
                    subSequence.Append(peptide[i]);

                    if (i == scores.Count - 1)
                    {
                        results.Add(subSequence.ToString());
                    }
                }
                else if (scores[i] < cutoff || i == scores.Count - 1)
                {
                    if (subSequence.Length > minSize)
                    {
                        results.Add(subSequence.ToString());
                    }

                    subSequence.Clear();
                }

            }

            return results;
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
                PsmRegistry psmRegistry = new PsmRegistry()
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
                myRegistries.Add(psmRegistry);
            }
            return myRegistries;
        }

        //Method for finding valid peptides sequences
        public static List<string> FindValidPeptides(string sequence, List<int> scores, int minScore)
        {

            List<string> validPeptides = new List<string>();
            string currentPeptide = "";

            for (int i = 0; i < sequence.Length; i++)
            {
                if (scores[i] >= minScore)
                {
                    currentPeptide += sequence[i];
                }
                else
                {
                    if (currentPeptide.Length > 0)
                    {
                        validPeptides.Add(currentPeptide);
                        currentPeptide = "";
                    }
                }
            }

            if (currentPeptide.Length > 0)
            {
                validPeptides.Add(currentPeptide);
            }

            return validPeptides;

        }

        public static List<DeNovoRegistry> FilterSequencesByPeptideDeNovo(string peptide)
        {
            throw new NotImplementedException();
        }

     

        // public static void FilterSequencesByScoreDeNovo(int filterDeNovoSocore, Dictionary<string, List<DeNovoRegistry>> deNovoDictTemp)

    }
}
