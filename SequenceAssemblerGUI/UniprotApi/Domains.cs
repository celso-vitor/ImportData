using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SequenceAssemblerGUI.UniprotApi
{
    public class ProteinData
    {
        [JsonProperty("features")]
        public List<ProteinFeature> Features { get; set; }
    }

    public class ProteinFeature
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("location")]
        public FeatureLocation Location { get; set; }
    }

    public class FeatureLocation
    {
        [JsonProperty("start")]
        public FeaturePosition Start { get; set; }

        [JsonProperty("end")]
        public FeaturePosition End { get; set; }
    }

    public class FeaturePosition
    {
        [JsonProperty("value")]
        public int Value { get; set; }
    }

}
