using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ProteinAlignmentCode
{
    public class Alignment
    {
        public int ID { get; set; }
        public double Length { get; set; }
        public string SourceOrigin { get; set; }
        public string SourceType { get; set; }
        public string SourceSeq { get; set; }
        public string TargetOrigin { get; set; }
        public double Identity { get; set; }
        public double NormalizedIdentityScore { get; set; }
        public double SimilarityScore { get; set; }
        public double NormalizedSimilarity { get; set; }
        public double AlignedAA { get; set; }
        public double NormalizedAlignedAA { get; set; }
        public int GapsUsed { get; set; }
        public List<int> StartPositions { get; set; }
        public string AlignedLargeSequence { get; set; }
        public string AlignedSmallSequence { get; set; }

        public string StartPositionsString
        {
            get
            {
                return string.Join(", ", StartPositions);
            }
        }



    }
}
