using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SequenceAssemblerLogic;
using SequenceAssemblerLogic.ProteinAlignmentCode;

namespace SequenceAssemblerGUI
{
    /// <summary>
    /// Interaction logic for AlignmentViewer.xaml
    /// </summary>
    public partial class AlignmentViewer : UserControl
    {

        public List<Alignment> AlignmentList { get; set; }
        public List<Fasta> MyFasta { get; set; }
        public AlignmentViewer()
        {
            InitializeComponent();
        }

        public void UpdateAlignmentGrid(int minIdentity, int minNormalizedSimilarity, List<Fasta> myFasta)
        {
            MyFasta = myFasta;

            // Apply filters on the data
            List<Alignment> filteredAlnResults = AlignmentList.Where(a => a.Identity >= minIdentity && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();


            DataTable dataTable = new DataTable();

            // Define the DataTable columns with the appropriate data types
            dataTable.Columns.Add("Identity", typeof(int));
            dataTable.Columns.Add("Normalized Identity Score", typeof(double));
            dataTable.Columns.Add("Similarity Score", typeof(int));
            dataTable.Columns.Add("Normalized Similarity", typeof(double));
            dataTable.Columns.Add("AlignedAA", typeof(int));
            dataTable.Columns.Add("Normalized AlignedAA", typeof(double));
            dataTable.Columns.Add("Gaps Used", typeof(int));
            dataTable.Columns.Add("Aligned Large Sequence", typeof(string));
            dataTable.Columns.Add("Aligned Small Sequence", typeof(string));

            // Fill the DataTable with your data
            foreach (var alignment in filteredAlnResults)
            {
                DataRow newRow = dataTable.NewRow();
                newRow[0] = alignment.Identity;
                newRow[1] = alignment.NormalizedIdentityScore;
                newRow[2] = alignment.SimilarityScore;
                newRow[3] = alignment.NormalizedSimilarity;
                newRow[4] = alignment.AlignedAA;
                newRow[5] = alignment.NormalizedAlignedAA;
                newRow[6] = alignment.GapsUsed;
                newRow[7] = alignment.AlignedLargeSequence;
                newRow[8] = alignment.AlignedSmallSequence;

                dataTable.Rows.Add(newRow);
            }

            // Set the DataTable as the data source for your control 
            DataGridAlignments.ItemsSource = null; // Clear previous items
            DataGridAlignments.ItemsSource = dataTable.DefaultView;

            DataGridFasta.ItemsSource = MyFasta;
        }
    }
}
