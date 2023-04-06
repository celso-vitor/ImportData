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

            plotModel1.Series.Add(bsPSM);
            plotModel1.Series.Add(bsDeNovo);
            plotModel1.Axes.Add(linearAxis);
            plotModel1.Axes.Add(categoryAxis1);
            PlotViewEnzymeEfficiency.Model = plotModel1;
            
    }
        private void UpdateGeneral()
        {
            int denovoSequeceLength = (int)IntegerUpDownDeNovoLength.Value;
            deNovoDictTemp = novorParser.FilterDictDeNovo(denovoSequeceLength);

            int psmSequenceLength = (int)IntegerUpDownPSMLength.Value;
            psmDictTemp = novorParser.FilterDictPSM(psmSequenceLength);


            var r = (from v in psmDictTemp.Values
                     from rr in v
                     select Math.Floor(rr.Score)).Distinct().ToList();

            r.Sort();



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
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;
        }
    }

}
        
        
      