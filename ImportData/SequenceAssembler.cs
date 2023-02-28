using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImportData
{
    public class SequenceAssembler
    {
        public List<DeNovoRegistry> MyDeNovoRegistries { get; private set; }
        public List<PsmRegistry> MyPsmRegistries { get; private set; }

        public SequenceAssembler() 
        {
            MyDeNovoRegistries = new();
            MyPsmRegistries = new();
        }

        public void LoadDeNovoRegistries(string fileName)
        {
            string[] lines = File.ReadAllLines(fileName);

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
                MyDeNovoRegistries.Add(deNovoRegistry);
            }
        }

        public void LoadPsmRegistries(string psmfileName)
        {
            string[] line = File.ReadAllLines(psmfileName);

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
                MyPsmRegistries.Add(PSMRegistry);
            }
        }           
                  
    }
}
