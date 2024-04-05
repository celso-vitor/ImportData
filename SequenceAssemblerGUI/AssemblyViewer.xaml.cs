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

            Label l1 = new Label() { Content = "Template" };
            MainGrid.Children.Add(l1);


            ////Obtain Alignments
            Dictionary<string, Alignment> DictNameAlignment = GenerateAlignments(contigs, template);


            MainGrid.RowDefinitions.Add( new RowDefinition() );
            MainGrid.ColumnDefinitions.Add( new ColumnDefinition() );





            //foreach (var c in template)
            //{
            //    MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            //}

            int rowCounter = 0;
            foreach (var kvp in DictNameAlignment)
            {

                MainGrid.RowDefinitions.Add(new RowDefinition() );
                Label l = new Label() { Content = kvp.Key };

                MainGrid.Children.Add(l);
                Grid.SetRow(l, ++rowCounter);
            }


        }

        private Dictionary<string, Alignment> GenerateAlignments(Dictionary<string, string> contigs, string template)
        {
            Dictionary<string, Alignment> alignments = new Dictionary<string, Alignment>();
            foreach (var kvp in contigs)
            {
                SequenceAligner aligner = new SequenceAligner(); // Crie uma instância de SequenceAligner
                Alignment alignmentResult = aligner.AlignSequences(template, kvp.Value); // Chame o método AlignSequences nessa instância
                
                alignments.Add(kvp.Key, alignmentResult);

            }

            return alignments;
        }
    }
}
