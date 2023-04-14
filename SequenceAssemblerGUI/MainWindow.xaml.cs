using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using static System.Windows.Forms.LinkLabel;


namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        NovorParser novorParser;
        Dictionary<string, List<PsmRegistry>> psmDictTemp;
        Dictionary<string, List<DeNovoRegistry>> deNovoDictTemp;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItemImportResults_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = false;

            if ((bool)folderBrowserDialog.ShowDialog())
            {

                novorParser = new ();
                novorParser.LoadNovorUniversal(new DirectoryInfo(folderBrowserDialog.SelectedPaths[0]));

                int totalPsmRegistries = novorParser.DictPsm.Values.Sum(list => list.Count);
                int totalDenovoRegistries = novorParser.DictDenovo.Values.Sum(list => list.Count);

                Console.WriteLine($"Total dos Registros de Psm: {totalPsmRegistries}");
                Console.WriteLine($"Total dos Registros de DeNovo: {totalDenovoRegistries}");

                LabelPSMCount.Content = totalPsmRegistries;
                LabelDeNovoCount.Content = totalDenovoRegistries;

                TabControlMain.IsEnabled = true;
                UpdateGeneral();
                
            }
            
           
        }
        private void UpdatePlot()
        {

            PlotModel plotModel1 = new()
            {
                Title = "Sequences"
            };
            BarSeries bsPSM = new() { XAxisKey = "x", YAxisKey = "y", Title = "PSM" };
            BarSeries bsDeNovo = new() { XAxisKey = "x", YAxisKey = "y", Title = "DeNovo" };
       

            var categoryAxis1 = new CategoryAxis() { Key="y"};
            var linearAxis = new LinearAxis() { Key = "x" };

            foreach (KeyValuePair<string, List<DeNovoRegistry>> kvp in novorParser.DictDenovo)
            {
                categoryAxis1.Labels.Add(kvp.Key);
                bsPSM.Items.Add(new BarItem(psmDictTemp[kvp.Key].Select(a => a.Peptide).Distinct().Count()));
                bsDeNovo.Items.Add(new BarItem(deNovoDictTemp[kvp.Key].Select(a => a.Peptide). Distinct().Count()));
            }

            foreach (KeyValuePair<string, List<PsmRegistry>> kvp in novorParser.DictPsm)
            {
                categoryAxis1.Labels.Add(kvp.Key);
                bsPSM.Items.Add(new BarItem(psmDictTemp[kvp.Key].Select(a => a.Peptide).Distinct().Count()));
                bsDeNovo.Items.Add(new BarItem(deNovoDictTemp[kvp.Key].Select(a => a.Peptide).Distinct().Count()));
            }



            plotModel1.Series.Add(bsPSM);
            plotModel1.Series.Add(bsDeNovo);
            plotModel1.Axes.Add(linearAxis);
            plotModel1.Axes.Add(categoryAxis1);
            PlotViewEnzymeEfficiency.Model = plotModel1;
            
    }

        private void UpdateGeneral()
        {
            //Reset the temporary Dictionary
            deNovoDictTemp = new Dictionary<string, List<DeNovoRegistry>>();
            psmDictTemp = new Dictionary<string, List<PsmRegistry>>();

            foreach (var kvp in novorParser.DictDenovo)
            {
                deNovoDictTemp.Add(kvp.Key, kvp.Value);
            }

            foreach (var kvp in novorParser.DictPsm)
            {
                psmDictTemp.Add( kvp.Key, kvp.Value );
            }
            //---------------------------------------------------------

            // Apply filters to the filtered dictionaries
            int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
            NovorParser.FilterDictMinLengthDeNovo(denovoMinSequeceLength, deNovoDictTemp);

            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            NovorParser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int filterDeNovoScore = (int)IntegerUpDownDeNovoScore.Value;
            NovorParser.FilterDeNovoScore(filterDeNovoScore, deNovoDictTemp); //vc quer acima de um determinado score, e nao abaixo.  Quanto maior o score, maior a qualidade, e outra coisa, vc esta filtrando o length com o score, nao faz sentido nenhum

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            NovorParser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            NovorParser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            int filterPsmScore = (int)IntegerUpDownPSMScore.Value;
            NovorParser.FilterPSMScore(filterPsmScore, psmDictTemp);


            // Update the plot with the filtered data
            UpdatePlot();

        }
        private void ButtonProcess_Click(object sender, RoutedEventArgs e)
        {
            TabItemResults.IsEnabled = true;

            TabControlMain.SelectedItem = TabItemResults;
           
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateGeneral();
            UpdatePlot(); 
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;
        }
    }

}
        
        
      