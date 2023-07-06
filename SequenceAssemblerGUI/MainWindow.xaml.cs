using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using SequenceAssemblerLogic;
using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        NovorParser novorParser;
        Dictionary<string, List<PsmRegistry>> psmDictTemp;
        Dictionary<string, List<DeNovoRegistry>> deNovoDictTemp;
        private string peptide;

        DataTable dtDenovo = new DataTable
        {
            Columns =
            {
                new DataColumn("IsTag"),
                new DataColumn("Folder"),
                new DataColumn("File"),
                new DataColumn("Sequence"),
                new DataColumn("Score", typeof(double)),
                new DataColumn("AAScores"),
                new DataColumn("ScanNumber", typeof(int)) 
            }
        };

        DataTable dtPSM = new DataTable
        {
            Columns = 
            {
                new DataColumn("Folder"),
                new DataColumn("File"),
                new DataColumn("Sequence"),
                new DataColumn("Score", typeof(double)),
                new DataColumn("ScanNumber", typeof(int)) 
            }
        };

        public MainWindow()
        {
            InitializeComponent();

        }

        private void MenuItemImportResults_Click(object sender, RoutedEventArgs e)
        {

            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = true;

            if ((bool)folderBrowserDialog.ShowDialog())
            {
                dtDenovo.Clear();
                dtPSM.Clear();


                // Create a list to store the sequences for ContigAssembler
                List<string> sequencesForAssembly = new List<string>();

                foreach (string folderPath in folderBrowserDialog.SelectedPaths)
                {
                    DirectoryInfo mainDir = new DirectoryInfo(folderPath);
                    string folderName = mainDir.Name; // Get the name of the selected folder

                    foreach (DirectoryInfo subDir in mainDir.GetDirectories())
                    {
                        folderName += $",{subDir.Name}"; // Add subfolder name separated by comma
                    }

                    novorParser = new();
                    novorParser.LoadNovorUniversal(mainDir);


                    foreach (var denovo in novorParser.DictDenovo.Values.SelectMany(x => x))
                    {
                        DataRow row = dtDenovo.NewRow();

                        if (denovo.IsTag)
                        {
                            row["IsTag"] = "T";
                        }
                        else
                        {
                            row["IsTag"] = "F";
                        }

                        row["Folder"] = folderName;
                        row["File"] = novorParser.FileDictionary[denovo.File];
                        row["ScanNumber"] = denovo.ScanNumber;
                        row["Sequence"] = denovo.Peptide;
                        row["Score"] = denovo.Score;
                        row["AAScores"] = string.Join("-", denovo.AaScore);

                        dtDenovo.Rows.Add(row);
                       // dtContig.Rows.Add(denovo.Peptide);
                    }

                    foreach (var psm in novorParser.DictPsm.Values.SelectMany(x => x))
                    {
                        DataRow row = dtPSM.NewRow();
                        row["Folder"] = folderName;
                        row["File"] = novorParser.FileDictionary[psm.File];
                        row["ScanNumber"] = psm.ScanNumber;
                        row["Sequence"] = psm.Peptide;
                        row["Score"] = psm.Score;

                        dtPSM.Rows.Add(row);

                        sequencesForAssembly.Add(psm.Peptide);
                    }

                }


                DataView dvDenovo = new DataView(dtDenovo);
                DataGridDeNovo.ItemsSource = dvDenovo;

                DataView dvPsm = new DataView(dtPSM);
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
                Title = "Sequences",
                IsLegendVisible = true, // Ensure the legend is visible
            };


            BarSeries bsPSM = new() { XAxisKey = "x", YAxisKey = "y", Title = "PSM" };
            BarSeries bsDeNovo = new() { XAxisKey = "x", YAxisKey = "y", Title = "DeNovo" };


            var categoryAxis1 = new CategoryAxis() { Key = "y", Position = AxisPosition.Bottom };
            var linearAxis = new LinearAxis() { Key = "x", Position = AxisPosition.Left };

            // Add DictDenovo dictionary folders to category axis
            foreach (var kvp in novorParser.DictDenovo)
            {
                categoryAxis1.Labels.Add(kvp.Key);
            }

            // Add DictPsm dictionary folders to the category axis
            foreach (var kvp in novorParser.DictPsm)
            {
                if (!categoryAxis1.Labels.Contains(kvp.Key))
                {
                    categoryAxis1.Labels.Add(kvp.Key);
                }
            }

            // Add the values from the dictionaries to the corresponding BarSeries
            foreach (var kvp in deNovoDictTemp)
            {
                bsDeNovo.Items.Add(new BarItem { Value = kvp.Value.Select(a => a.Peptide).Distinct().Count() });
            }

            foreach (var kvp in psmDictTemp)
            {
                bsPSM.Items.Add(new BarItem { Value = kvp.Value.Select(a => a.Peptide).Distinct().Count() });
            }

            plotModel1.Series.Add(bsPSM);
            plotModel1.Series.Add(bsDeNovo);
            plotModel1.Axes.Add(linearAxis);
            plotModel1.Axes.Add(categoryAxis1);
            PlotViewEnzymeEfficiency.Model = plotModel1;

        }
        private async void UpdateDataView()
        {
            
            dtDenovo.Clear();

            if (deNovoDictTemp != null)
            {
                foreach (var kvp in deNovoDictTemp)
                {
                    string folderName = kvp.Key;

                    foreach (var denovo in kvp.Value)
                    {
                        DataRow row = dtDenovo.NewRow();

                        if (denovo.IsTag)
                        {
                            row["IsTag"] = "T";
                        }
                        else
                        {
                            row["IsTag"] = "F";
                        }

                        row["Folder"] = folderName;
                        row["File"] = novorParser.FileDictionary[denovo.File];
                        row["ScanNumber"] = denovo.ScanNumber;
                        row["Sequence"] = denovo.Peptide;
                        row["Score"] = denovo.Score;
                        row["AAScores"] = string.Join("-", denovo.AaScore);

                        dtDenovo.Rows.Add(row);
                    }
                }

                DataView dvDenovo = new DataView(dtDenovo);
                DataGridDeNovo.ItemsSource = dvDenovo;
            }

            dtPSM.Clear();
            if (psmDictTemp != null)
            {
                foreach (var kvp in psmDictTemp)
                {
                    string folderName = kvp.Key;

                    foreach (var psm in kvp.Value)
                    {
                        DataRow row = dtPSM.NewRow();
                        row["Folder"] = folderName;
                        row["File"] = novorParser.FileDictionary[psm.File];
                        row["ScanNumber"] = psm.ScanNumber;
                        row["Sequence"] = psm.Peptide;
                        row["Score"] = psm.Score;
                        
                        dtPSM.Rows.Add(row);
                    }
                }

                DataView dvPSM = new DataView(dtPSM);
                DataGridPSM.ItemsSource = dvPSM;
            }

            // Define how many amino acids should overlap for contigs (partially overlapping sequences).
            int overlapAAForContigs = (int)IntegerUpDownAAOverlap.Value;

            // From a dictionary of data psmDictTemp, select all the clean sequences (CleanPeptide) of PSMs, remove duplicates, and store them in a list named sequencesPSM.
            List<string> sequencesPSM =
                (from s in psmDictTemp.Values
                 from psmID in s
                 select psmID.CleanPeptide).Distinct().ToList();

            // From a dictionary of data deNovoDictTemp, select all the clean sequences (CleanPeptide) of deNovo, remove duplicates, and store them in a list named sequencesDeNovo.
            List<string> sequencesDeNovo =
                (from s in deNovoDictTemp.Values
                 from denovoID in s
                 select denovoID.CleanPeptide).Distinct().ToList();

            // Concatenate the PSM and deNovo sequence lists into a single list named filteredSequences.
            List<string> filteredSequences = sequencesPSM.Concat(sequencesDeNovo).ToList();

            // Disable the DataGridContig to prevent user interaction while data is being loaded.
            DataGridContig.IsEnabled = false;

            // Make a loading label visible to inform the user that data is being loaded.
            loadingLabel.Visibility = Visibility.Visible;

            // Execute the contig assembly process with the filtered sequences and the previously defined overlap value on a background task.
            var contigs = await Task.Run(
                () =>
                {
                    ContigAssembler ca = new ContigAssembler();
                    return ca.AssembleContigSequences(filteredSequences, overlapAAForContigs);
                }
                        );

            // Set the item source of DataGridContig to be an anonymous list containing the assembled contigs.
            DataGridContig.ItemsSource = contigs.Select(a => new { Contig = a });

            // Re-enable the DataGridContig to allow user interaction.
            DataGridContig.IsEnabled = true;

            // Hide the loading label as the data has now been loaded.
            loadingLabel.Visibility = Visibility.Hidden;

        }


        private void UpdateGeneral()
        {
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;

            //Reset the temporary Dictionary
            deNovoDictTemp = new Dictionary<string, List<DeNovoRegistry>>();
            psmDictTemp = new Dictionary<string, List<PsmRegistry>>();

            int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
            int denovoMinScore = (int)IntegerUpDownDeNovoScore.Value;

            foreach (var kvp in novorParser.DictDenovo)
            {

                List<DeNovoRegistry> list = (from rg in kvp.Value.Select(a => a).ToList()
                                             from rg2 in DeNovoTagExtractor.DeNovoRegistryToTags(rg, denovoMinScore, denovoMinSequeceLength)
                                             select rg2).ToList();

                deNovoDictTemp.Add(kvp.Key, list);
            }

            foreach (var kvp in novorParser.DictPsm)
            {
                psmDictTemp.Add(kvp.Key, kvp.Value.Select(a => a).ToList());
            }
            //---------------------------------------------------------

            // Apply filters to the filtered dictionaries

            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            NovorParser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            NovorParser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            NovorParser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            double filterPsmSocore = (int)IntegerUpDownPSMScore.Value;
            NovorParser.FilterSequencesByScorePSM(filterPsmSocore, psmDictTemp);


            //Update the GUI
            UpdatePlot();
            UpdateDataView();

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

        private void DataGridDeNovo_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridPSM_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }

}


