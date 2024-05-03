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
        private string _sourceOrigin;
        public int ID { get; set; }
        public double NormalizedLength { get; set; }
        public string SourceOrigin
        {
            get => _sourceOrigin;
            set
            {
                _sourceOrigin = value;
                UpdatePeptideAndFolder();
            }
        }

        // Propriedade para armazenar a origem da sequência (0 para PSM, 1 para DENOVO)
        public int Identity { get; set; }
        public double NormalizedIdentityScore { get; set; }
        public int SimilarityScore { get; set; }
        public double NormalizedSimilarity { get; set; }
        public int AlignedAA { get; set; }
        public double NormalizedAlignedAA { get; set; }
        public int GapsUsed { get; set; }
        public List<int> StartPositions { get; set; }
        public string AlignedLargeSequence { get; set; }
        public string AlignedSmallSequence { get; set; }

        // Peptide property is read-only
        public string Peptide { get; private set; }

        // Folder property is read-only
        public string Folder { get; private set; }

        // Method to update Peptide and Folder properties
        private void UpdatePeptideAndFolder()
        {
            // Splitting by "- " instead of just ","
            string[] parts = SourceOrigin.Split(new string[] { "- " }, StringSplitOptions.RemoveEmptyEntries);

            // Finding peptide and folder from split parts
            string peptidePart = parts.FirstOrDefault(p => p.Trim().StartsWith("Peptide:"));
            string folderPart = parts.FirstOrDefault(p => p.Trim().StartsWith("Folder:"));

            // Parsing peptide value
            if (!string.IsNullOrEmpty(peptidePart))
            {
                // Trimming "Peptide:" and whitespace from the peptide part
                Peptide = peptidePart.Trim().Substring("Peptide:".Length).Trim();
            }
            else
            {
                Peptide = "";
            }

            // Parsing folder value
            if (!string.IsNullOrEmpty(folderPart))
            {
                // Trimming "Folder:" and whitespace from the folder part
                Folder = folderPart.Trim().Substring("Folder:".Length).Trim();
            }
            else
            {
                Folder = "";
            }
        }



    }
}

