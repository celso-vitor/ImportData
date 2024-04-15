using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerLogic.ContigCode
{
    public class Contig
    {
        public int Id { get; set; }
        public string Sequence { get; set; }
        public List<IDResult> IDs { get; set; }
       
    }
}

