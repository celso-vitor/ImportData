using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Formats.Asn1;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
                dtDenovo.Columns.Add("Folder", typeof(string)); // Adicione uma nova coluna para o nome da pasta
                dtDenovo.Columns.Add("Sequences Peptides DeNovo", typeof(string));
                dtDenovo.Columns.Add("Score Peptides DeNovo", typeof(double));
                dtDenovo.Columns.Add("ScanNumber Peptides DeNovo", typeof(int));

                DataTable dtPsm = new DataTable();
                dtPsm.Columns.Add("Folder", typeof(string)); // Adicione uma nova coluna para o nome da pasta
                dtPsm.Columns.Add("Sequences Peptides PSM", typeof(string));
                dtPsm.Columns.Add("Score Peptides PSM", typeof(double));
                dtPsm.Columns.Add("ScanNumber Peptides PSM", typeof(int));

                foreach(string folderPath in folderBrowserDialog.SelectedPaths)
                {
                    DirectoryInfo mainDir = new DirectoryInfo(folderPath);
                    string folderName = mainDir.Name; // Obter o nome da pasta selecionada

                    foreach (DirectoryInfo subDir in mainDir.GetDirectories())
                    {
                        folderName += $",{subDir.Name}"; // Adicione o nome da subpasta separado por vírgula
                    }

                    novorParser = new();
                    novorParser.LoadNovorUniversal(mainDir);

                    foreach (var denovo in novorParser.DictDenovo.Values.SelectMany(x => x))
                    {
                        DataRow row = dtDenovo.NewRow();
                        row["Folder"] = folderName; // Defina o valor da nova coluna para o nome da pasta
                        row["Sequences Peptides DeNovo"] = denovo.Peptide;
                        row["Score Peptides DeNovo"] = denovo.Score;
                        row["ScanNumber Peptides DeNovo"] = denovo.ScanNumber;
                        dtDenovo.Rows.Add(row);
                    }

                    foreach (var psm in novorParser.DictPsm.Values.SelectMany(x => x))
                    {
                        DataRow row = dtPsm.NewRow();
                        row["Folder"] = folderName; // Defina o valor da nova coluna para o nome da pasta
                        row["Sequences Peptides PSM"] = psm.Peptide;
                        row["Score Peptides PSM"] = psm.Score;
                        row["ScanNumber Peptides PSM"] = psm.ScanNumber;
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
        private void UpdateDataView(DataView dataView, Dictionary<string, List<DeNovoRegistry>> deNovoDict, Dictionary<string, List<PsmRegistry>> psmDict)
        {
            if (dataView == null) return;

            DataTable dataTable = dataView.Table;
            dataTable.Rows.Clear();

            if (deNovoDict != null)
            {
                foreach (var kvp in deNovoDict)
                {
                    string folderName = kvp.Key;

                    foreach (var denovo in kvp.Value)
                    {
                        DataRow row = dataTable.NewRow();
                        row["Folder"] = folderName;
                        row["Sequences Peptides DeNovo"] = denovo.Peptide;
                        row["Score Peptides DeNovo"] = denovo.Score;
                        row["ScanNumber Peptides DeNovo"] = denovo.ScanNumber;
                        dataTable.Rows.Add(row);
                    }
                }
            }

            if (psmDict != null)
            {
                foreach (var kvp in psmDict)
                {
                    string folderName = kvp.Key;

                    foreach (var psm in kvp.Value)
                    {
                        DataRow row = dataTable.NewRow();
                        row["Folder"] = folderName;
                        row["Sequences Peptides PSM"] = psm.Peptide;
                        row["Score Peptides PSM"] = psm.Score;
                        row["ScanNumber Peptides PSM"] = psm.ScanNumber;
                        dataTable.Rows.Add(row);
                    }
                }
            }
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

            int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
            NovorParser.FilterDictMinLengthDeNovo(denovoMinSequeceLength, deNovoDictTemp);

            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            NovorParser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            NovorParser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            NovorParser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            int filterPsmSocore = (int)IntegerUpDownPSMScore.Value;
            NovorParser.FilterSequencesByScorePSM(filterPsmSocore, psmDictTemp);

            UpdateDataView(DataGridDeNovo.ItemsSource as DataView, deNovoDictTemp, null);
            UpdateDataView(DataGridPSM.ItemsSource as DataView, null, psmDictTemp);

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
        
        
      