using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ResultParser
{
    public class IDResult
    {
        private string peptide;
        public int Id { get; set; } // Position Alignments
        public bool IsPSM { get; set; } = true;
        public bool IsTag { get; set; } = false;
        public string Source { get; set; } // "PSM" / "DeNovo"
        public short File { get; set; }
        public int ScanNumber { get; set; }
        public double RT { get; set; }
        public double Mz { get; set; }
        public int Z { get; set; }
        public double PepMass { get; set; }
        public double Err { get; set; }
        public double Score { get; set; }
        public List<int> AaScore { get; set; }

        public string Peptide
        {
            get => peptide;
            set
            {
                peptide = value;
                if (value != null)
                {
                    CleanPeptide = Regex.Replace(value, @"\([^)]*\)", "");  // Clean the peptide when it's set
                }
                else
                {
                    CleanPeptide = null;
                }
            }
        }

        public string CleanPeptide { get; private set; }  // CleanPeptide is now read-only
    }

}
