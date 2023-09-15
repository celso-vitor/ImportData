using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ProteinAlignmentCode
{
    internal class Alignment
    {
            public string AlignedLargeSequence { get; set; }
            public string AlignedSmallSequence { get; set; }
            public List<int> StartPositions { get; set; }
            public double IdentityScore { get; set; }
            public int GapsUsed { get; set; }
            public int SimilarityScore { get; set; }
        
    }
}
