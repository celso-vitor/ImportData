using System.Text.RegularExpressions;

namespace SequenceAssemblerLogic.ResultParser
{
    public class DeNovoRegistry 
    {
        public bool IsTag { get; set; } = false;
        public short File { get; set; }
        public int ScanNumber { get; set; }
        public double RT { get; set; }
        public double Mz { get; set; }
        public int Z { get; set; }
        public double PepMass { get; set; }
        public double Err { get; set; }
        public double Score { get; set; }
        public string Peptide { get; set; }
        public List<int> AaScore { get; set; }
     
        public string CleanPeptide
        {
            get
            {
                return Regex.Replace(Peptide, @"\([^)]*\)", "");
            }
        }
     

    }

}
