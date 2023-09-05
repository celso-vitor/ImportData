using System.Text;
using System.Text.RegularExpressions;

namespace SequenceAssemblerLogic.ResultParser
{
    public class Parser
    {
        public Dictionary<string, List<IDResult>> DictNovorDenovo { get; private set; }
        public Dictionary<string, List<IDResult>> DictNovorPsm { get; private set; }
        public Dictionary<string, List<IDResult>> DictPeaksDenovo{ get; private set; }
        public Dictionary<short, string> FileDictionary { get; private set; }

        public Parser()
        {
            DictNovorDenovo = new Dictionary<string, List<IDResult>>();
            DictNovorPsm = new Dictionary<string, List<IDResult>>();
            DictPeaksDenovo = new Dictionary<string, List<IDResult>>();
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

            foreach (var di2 in di.GetDirectories())
            {
                string[] csvFiles = Directory.GetFiles(di2.FullName, "*.csv");
                Console.WriteLine(di2.Name);

                foreach (var fileName in csvFiles)
                {
                    FileDictionary.Add(++fileCounter, Path.GetFileName(fileName));
                    string firstLine = File.ReadAllLines(fileName)[0];

                    if (firstLine.StartsWith("#id"))
                    {
                        // Handle Novor PSM results
                        if (DictNovorPsm.ContainsKey(di2.Name))
                        {
                            DictNovorPsm[di2.Name].AddRange(LoadNovorPsmRegistries(fileName, fileCounter));
                        }
                        else
                        {
                            DictNovorPsm.Add(di2.Name, LoadNovorPsmRegistries(fileName, fileCounter));
                        }
                    }
                    else if (firstLine.StartsWith("#id")) 
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
                    else if (firstLine.StartsWith("Fraction"))

                        if (DictPeaksDenovo.ContainsKey(di2.Name))
                    {
                        DictPeaksDenovo[di2.Name].AddRange(LoadPeaksDeNovorRegistries(fileName, fileCounter));
                    }
                    else
                    {
                        DictPeaksDenovo.Add(di2.Name, LoadPeaksDeNovorRegistries(fileName, fileCounter));
                    }
                }
            }
        }
        private List<IDResult> LoadNovorDeNovoRegistries(string denovofileName, short fileCounter)
        {
            var lines = File.ReadAllLines(denovofileName);
            var myRegistries = new List<IDResult>();

            for (int i = 2; i < lines.Length; i++)
            {
                var columns = Regex.Split(lines[i], ",");
                var psmRegistry = new IDResult
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


        private List<IDResult> LoadNovorPsmRegistries(string psmfileName, short fileCounter)
        {
            var lines = File.ReadAllLines(psmfileName);
            var myRegistries = new List<IDResult>();

            for (int i = 2; i < lines.Length; i++)
            {
                var columns = Regex.Split(lines[i], ",");
                var psmRegistry = new IDResult
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

                for (int i = 2; i < lines.Length; i++)
                {
                    var columns = Regex.Split(lines[i], ",");
                    var deNovoRegistry = new IDResult
                    {
                        ScanNumber = int.Parse(columns[5]),
                        File = fileCounter,
                        RT = double.Parse(columns[12]),
                        Mz = double.Parse(columns[10]),
                        Z = int.Parse(columns[11]),
                        PepMass = double.Parse(columns[15]),
                        Score = double.Parse(columns[7]),
                        Peptide = Regex.Replace(columns[4], " ", ""),
                        AaScore = Regex.Split(columns[18], "-").Select(b => int.Parse(b)).ToList()
                    };
                    myRegistries.Add(deNovoRegistry);
                }

                return myRegistries;
        }
    }
}            


