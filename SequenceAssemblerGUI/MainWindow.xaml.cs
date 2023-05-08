using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        NovorParser novorParser;
        Dictionary<string, List<PsmRegistry>> psmDictTemp;
        Dictionary<string, List<DeNovoRegistry>> deNovoDictTemp;
        private string peptide;
       

        //public ObservableCollection<DeNovoRegistry> SequencesDeNovo { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            //SequencesDeNovo = new ObservableCollection<DeNovoRegistry>();
          
            //Console.WriteLine(SequencesDeNovo);
            //SequencesDeNovo.Add(new DeNovoRegistry() { Peptide = "" });
            //DataGridDeNovo.ItemsSource = SequencesDeNovo;
        }


        private void MenuItemImportResults_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = true;

            if ((bool)folderBrowserDialog.ShowDialog())
            {
                DataTable dtDenovo = new DataTable();
                dtDenovo.Columns.Add("Sequences Peptides DeNovo", typeof(string));

                DataTable dtPsm = new DataTable();
                dtPsm.Columns.Add("Sequences Peptides PSM", typeof(string)); // Adicione as colunas necessárias para os registros de Psm

                foreach (string folderPath in folderBrowserDialog.SelectedPaths)
                {
                    novorParser = new();
                    novorParser.LoadNovorUniversal(new DirectoryInfo(folderPath));

                    foreach (var denovo in novorParser.DictDenovo.Values.SelectMany(x => x))
                    {
                        DataRow row = dtDenovo.NewRow();
                        row["Sequences Peptides DeNovo"] = denovo.Peptide;
                        dtDenovo.Rows.Add(row);
                    }

                    foreach (var psm in novorParser.DictPsm.Values.SelectMany(x => x))
                    {
                        DataRow row = dtPsm.NewRow();
                        row["Sequences Peptides PSM"] = psm.Peptide; // Preencha as colunas necessárias para os registros de Psm
                        dtPsm.Rows.Add(row);
                    }
                }

                DataView dvDenovo = new DataView(dtDenovo);
                DataGridDeNovo.ItemsSource = dvDenovo;

                DataView dvPsm = new DataView(dtPsm);
                DataGridPSM.ItemsSource = dvPsm;

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


            var categoryAxis1 = new CategoryAxis() { Key = "y" };
            var linearAxis = new LinearAxis() { Key = "x" };

            foreach (KeyValuePair<string, List<DeNovoRegistry>> kvp in novorParser.DictDenovo)
            {
                categoryAxis1.Labels.Add(kvp.Key);
                bsPSM.Items.Add(new BarItem(psmDictTemp[kvp.Key].Select(a => a.Peptide).Distinct().Count()));
                bsDeNovo.Items.Add(new BarItem(deNovoDictTemp[kvp.Key].Select(a => a.Peptide).Distinct().Count()));
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
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;

            //Reset the temporary Dictionary
            deNovoDictTemp = new Dictionary<string, List<DeNovoRegistry>>();
            psmDictTemp = new Dictionary<string, List<PsmRegistry>>();

            foreach (var kvp in novorParser.DictDenovo)
            {
                deNovoDictTemp.Add(kvp.Key, kvp.Value.Select(a => a).ToList());
            }

            foreach (var kvp in novorParser.DictPsm)
            {
                psmDictTemp.Add(kvp.Key, kvp.Value.Select(a => a).ToList());
            }
            //---------------------------------------------------------

            // Apply filters to the filtered dictionaries

           // int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
           // NovorParser.FilterDictMinLengthDeNovo(denovoMinSequeceLength, deNovoDictTemp);

            //int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
           // NovorParser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            NovorParser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            NovorParser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            int filterPsmSocore = (int)IntegerUpDownPSMScore.Value;
            NovorParser.FilterSequencesByScorePSM(filterPsmSocore, psmDictTemp);

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
        }

        
    }
       
}
        
        
      