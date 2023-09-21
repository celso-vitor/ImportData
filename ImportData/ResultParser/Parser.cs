using SequenceAssemblerLogic.ProteinAlignmentCode;
using System.Text;
using System.Text.RegularExpressions;

namespace SequenceAssemblerLogic.ResultParser
{
    public class Parser
    {

        public Dictionary<string, List<IDResult>> DictNovorDenovo { get; private set; }
        public Dictionary<string, List<IDResult>> DictNovorPsm { get; private set; }
        public Dictionary<string, List<IDResult>> DictPeaksDenovo { get; private set; }

        public Dictionary<short, string> FileDictionary { get; private set; }

        public Parser()
        {
            DictNovorDenovo = new();
            DictNovorPsm = new();
            DictPeaksDenovo = new();
        }

        public static void FilterDictMaxLengthDeNovo(int peptideLength, Dictionary<string, List<IDResult>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length > peptideLength);
            }
        }


        public static void FilterDictMinLengthPSM(int peptideLength, Dictionary<string, List<IDResult>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length < peptideLength);
            }
        }

        public static void FilterDictMaxLengthPSM(int peptideLength, Dictionary<string, List<IDResult>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(a => a.CleanPeptide.Length > peptideLength);
            }
        }

        public static void FilterSequencesByScorePSM(double minScore, Dictionary<string, List<IDResult>> theDict)
        {
            foreach (var kvp in theDict)
            {
                kvp.Value.RemoveAll(seq => seq.Score < minScore);
            }

        }

     
        public void LoadUniversal(DirectoryInfo di)
        {
            short fileCounter = 0;
            FileDictionary = new Dictionary<short, string>();

            foreach (DirectoryInfo di2 in di.GetDirectories())
            {
                string[] csvFiles = Directory.GetFiles(di2.FullName, "*.csv");
                Console.WriteLine(di2.Name);



                foreach (string fileName in csvFiles)
                {
                    FileDictionary.Add(++fileCounter, Path.GetFileName(fileName));
                    string firstLine = File.ReadAllLines(fileName)[0];


                    if (firstLine.StartsWith("#id"))
                    {
                        if (DictNovorPsm.ContainsKey(di2.Name))
                        {
                            DictNovorPsm[di2.Name].AddRange(LoadNovorPsmRegistries(fileName, fileCounter));
                        }
                        else
                        {
                            DictNovorPsm.Add(di2.Name, LoadNovorPsmRegistries(fileName, fileCounter));
                        }
                    }
                    else if (firstLine.StartsWith("Fraction"))
                    {
                        if (DictNovorDenovo.ContainsKey(di2.Name))
                        {
                            DictNovorDenovo[di2.Name].AddRange(LoadPeaksDeNovorRegistries(fileName, fileCounter));
                        }
                        else
                        {
                            DictNovorDenovo.Add(di2.Name, LoadPeaksDeNovorRegistries(fileName, fileCounter));
                        }
                    }
                    else
                    {
                        if (DictNovorDenovo.ContainsKey(di2.Name))
                        {
                            DictNovorDenovo[di2.Name].AddRange(LoadNovorDeNovoRegistries(fileName, fileCounter));
                        }
                        else
                        {
                            DictNovorDenovo.Add(di2.Name, LoadNovorDeNovoRegistries(fileName, fileCounter));
                        }
                    }
                }
            }
        }

        private List<IDResult> LoadNovorDeNovoRegistries(string denovofileName, short fileCounter)
        {
            string[] lines = File.ReadAllLines(denovofileName);
            List<IDResult> myRegistries = new();
            for (int i = 22; i < lines.Length; i++)
            {
                string[] cols = Regex.Split(lines[i], ",");
                IDResult deNovoRegistry = new IDResult()
                {
                    IsPSM = false,
                    ScanNumber = int.Parse(cols[1]),
                    File = fileCounter,
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

        private List<IDResult> LoadNovorPsmRegistries(string psmfileName, short fileCounter)
        {
            string[] line = File.ReadAllLines(psmfileName);
            List<IDResult> myRegistries = new();
            for (int i = 2; i < line.Length; i++)
            {
                string[] columns = Regex.Split(line[i], ",");
                IDResult psmRegistry = new IDResult()
                {
                    ScanNumber = int.Parse(columns[2]),
                    File = fileCounter,
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

        private List<IDResult> LoadPeaksDeNovorRegistries(string denovofileName, short fileCounter)
        {
            var lines = File.ReadAllLines(denovofileName);
            var myRegistries = new List<IDResult>();

            for (int i = 1; i < lines.Length; i++) 
            {
                var columns = Regex.Split(lines[i], ",");

                var deNovoRegistry = new IDResult
                {
                    ScanNumber = int.Parse(columns[4]),
                    File = fileCounter,
                    RT = double.Parse(columns[11]),
                    Mz = double.Parse(columns[9]),
                    Z = int.Parse(columns[10]),
                    PepMass = double.Parse(columns[14]),
                    Score = double.Parse(columns[6]),
                    Peptide = Regex.Replace(columns[3], " ", ""),
                    AaScore = columns[17].Split(' ').Select(b => int.Parse(b)).ToList()
                };

                myRegistries.Add(deNovoRegistry);
            }
            return myRegistries;
        }

     
    }
}
