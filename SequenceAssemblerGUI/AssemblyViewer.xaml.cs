using System;
using System.Collections.Generic;
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
using SequenceAssemblerLogic.ProteinAlignmentCode;

namespace SequenceAssemblerGUI
{
    /// <summary>
    /// Interaction logic for AssemblyViewer.xaml
    /// </summary>
    public partial class AssemblyViewer : UserControl
    {
        public AssemblyViewer()
        {
            InitializeComponent();
        }

        public void Display(string template, Dictionary<string, string> contigs)
        {
            //Prepare the main grid
            MainGrid.Background = Brushes.WhiteSmoke;

            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();

            MainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Template row height
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) }); // Template column width

            Label templateLabel = new Label() { Content = "Template: " + template, Padding = new Thickness(5) };
            Grid.SetRow(templateLabel, 0);
            Grid.SetColumn(templateLabel, 0);
            MainGrid.Children.Add(templateLabel);

            ////Obtain Alignments
            Dictionary<string, Alignment> DictNameAlignment = GenerateAlignments(contigs, template);

            int rowCounter = 1; // Start at 1 to leave room for the template label
            foreach (var kvp in DictNameAlignment)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                Label contigLabel = new Label { Content = kvp.Key + ": " + kvp.Value.ToString(), Padding = new Thickness(5) };
                Grid.SetRow(contigLabel, rowCounter++);
                Grid.SetColumn(contigLabel, 0);
                MainGrid.Children.Add(contigLabel);
            }
        }

        private Dictionary<string, Alignment> GenerateAlignments(Dictionary<string, string> contigs, string template)
        {
            Dictionary<string, Alignment> alignments = new Dictionary<string, Alignment>();
            foreach (var kvp in contigs)
            {
                SequenceAligner aligner = new SequenceAligner(); // Create an instance of SequenceAligner
                Alignment alignmentResult = aligner.AlignSequences(template, kvp.Value); // Call the AlignSequences method on this instance

                alignments.Add(kvp.Key, alignmentResult);
            }

            return alignments;
        }
    }
}

