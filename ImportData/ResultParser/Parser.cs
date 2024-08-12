using SequenceAssemblerLogic.ProteinAlignmentCode;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SequenceAssemblerLogic.ResultParser
{
    public class Parser
    {
        public Dictionary<string, List<IDResult>> DictDenovo { get; private set; }
        public Dictionary<string, List<IDResult>> DictPsm { get; private set; }
        public Dictionary<string, List<IDResult>> deNovoDictTemp { get; private set; }
        public Dictionary<string, List<IDResult>> psmDictTemp { get; private set; }



        public Dictionary<short, string> FileDictionary { get; private set; }

        public Parser()
        {
            DictDenovo = new();
            DictPsm = new();
            deNovoDictTemp = new();
            psmDictTemp = new();

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
                string[] sepr2Files = Directory.GetFiles(di2.FullName, "*.sepr2");
                Console.WriteLine($"Processing directory: {di2.Name}");

                foreach (string fileName in csvFiles)
                {
                    FileDictionary.Add(++fileCounter, Path.GetFileName(fileName));
                    string firstLine = File.ReadAllLines(fileName)[0];

                    if (firstLine.StartsWith("#id"))
                    {
                        var registries = LoadNovorPsmRegistries(fileName, fileCounter);
                        if (DictPsm.ContainsKey(di2.Name))
                        {
                            DictPsm[di2.Name].AddRange(registries);
                        }
                        else
                        {
                            DictPsm.Add(di2.Name, registries);
                        }
                    }
                    else if (firstLine.StartsWith("Fraction"))
                    {
                        var registries = LoadPeaksDeNovorRegistries(fileName, fileCounter);
                        if (DictDenovo.ContainsKey(di2.Name))
                        {
                            DictDenovo[di2.Name].AddRange(registries);
                        }
                        else
                        {
                            DictDenovo.Add(di2.Name, registries);
                        }
                    }
                    //else if (firstLine.StartsWith("Fraction"))
                    //{
                    //    var registries = LoadPeaksDeNovorRegistriesEdit(fileName, fileCounter);
                    //    Console.WriteLine($"Loaded {registries.Count} DeNovo registries from {fileName}");
                    //    if (DictDenovo.ContainsKey(di2.Name))
                    //    {
                    //        DictDenovo[di2.Name].AddRange(registries);
                    //    }
                    //    else
                    //    {
                    //        DictDenovo.Add(di2.Name, registries);
                    //    }
                    //}
                    else
                    {
                        var registries = LoadNovorDeNovoRegistries(fileName, fileCounter);
                        if (DictDenovo.ContainsKey(di2.Name))
                        {
                            DictDenovo[di2.Name].AddRange(registries);
                        }
                        else
                        {
                            DictDenovo.Add(di2.Name, registries);
                        }
                    }
                }

                foreach (string fileName in sepr2Files)
                {
                    FileDictionary.Add(++fileCounter, Path.GetFileName(fileName));
                    var registries = LoadSepr2Registries(fileName, fileCounter);
                    if (DictPsm.ContainsKey(di2.Name))
                    {
                        DictPsm[di2.Name].AddRange(registries);
                    }
                    else
                    {
                        DictPsm.Add(di2.Name, registries);
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
                    RT = double.Parse(cols[2], CultureInfo.InvariantCulture),
                    Mz = double.Parse(cols[3], CultureInfo.InvariantCulture),
                    Z = int.Parse(cols[4]),
                    PepMass = double.Parse(cols[5], CultureInfo.InvariantCulture),
                    Err = double.Parse(cols[6], CultureInfo.InvariantCulture),
                    Score = AdjustScore(double.Parse(cols[8], CultureInfo.InvariantCulture)),
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
                    RT = double.Parse(columns[3], CultureInfo.InvariantCulture),
                    Mz = double.Parse(columns[4], CultureInfo.InvariantCulture),
                    Z = int.Parse(columns[5]),
                    PepMass = double.Parse(columns[6], CultureInfo.InvariantCulture),
                    Err = double.Parse(columns[7], CultureInfo.InvariantCulture),
                    Score = AdjustScore(double.Parse(columns[9], CultureInfo.InvariantCulture)),
                    Peptide = Regex.Replace(columns[14], " ", ""),
                    AaScore = Regex.Split(columns[16], "-").Select(b => int.Parse(b)).ToList()
                };
                myRegistries.Add(psmRegistry);
            }
            return myRegistries;
        }


        private double AdjustScore(double score)
        {
            return score > 100 ? score / 10.0 : score; // Score Adjust 
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

        //private List<IDResult> LoadPeaksDeNovorRegistriesEdit(string denovofileName, short fileCounter)
        //{
        //    var lines = File.ReadAllLines(denovofileName);
        //    var myRegistries = new List<IDResult>();

        //    // Assuming the first line is the header, so starting from index 1
        //    for (int i = 1; i < lines.Length; i++)
        //    {
        //        // Use regex to split by comma, but handle commas inside quotes correctly
        //        var columns = Regex.Split(lines[i], ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

        //        var deNovoRegistry = new IDResult
        //        {
        //            ScanNumber = int.Parse(columns[4].Split(':')[1]), // Assuming Scan is F4:17259, we get 17259
        //            File = fileCounter,
        //            RT = double.Parse(columns[11]),
        //            Mz = double.Parse(columns[9]),
        //            Z = int.Parse(columns[10]),
        //            PepMass = double.Parse(columns[14]),
        //            Score = double.Parse(columns[6]),
        //            Peptide = Regex.Replace(columns[3], " ", ""), // Removing spaces from peptide
        //            AaScore = columns[17].Split(' ').Select(b => int.Parse(b)).ToList()
        //        };

        //        myRegistries.Add(deNovoRegistry);
        //    }
        //    return myRegistries;
        //}

        public List<IDResult> LoadSepr2Registries(string sepr2FileName, short fileCounter)
        {
            var plResult = SeproPckg2.ResultPackage.Load(sepr2FileName);
            var myRegistries = new List<IDResult>();

            foreach (var psm in plResult.MyProteins.AllPSMs)
            {
                var sepr2Registry = new IDResult
                {
                    ScanNumber = psm.ScanNumber,
                    File = fileCounter,
                    RT = psm.RetentionTime,
                    Mz = psm.MZ,
                    Z = psm.ChargeState,
                    PepMass = psm.MeasuredMH - 1.00728,
                    Score = psm.ClassificationScore,
                    Peptide = PatternTools.pTools.CleanPeptide(psm.PeptideSequence),
                    AaScore = PatternTools.pTools.CleanPeptide(psm.PeptideSequence).Select(b => (int)100).ToList()
                };

                myRegistries.Add(sepr2Registry);
            }

            return myRegistries;
        }

    }
}