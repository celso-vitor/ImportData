using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic;
using SequenceAssemblerLogic.ContigCode;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.ResultParser;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        Parser newParser;
        Dictionary<string, List<IDResult>> psmDictTemp;
        Dictionary<string, List<IDResult>> deNovoDictTemp;

        List<Contig> myContigs;
        List<Fasta> myFasta;
        List<Alignment> myAlignment;

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
                List<string> sequencesForAssembly = new();

                foreach (string folderPath in folderBrowserDialog.SelectedPaths)
                {
                    DirectoryInfo mainDir = new DirectoryInfo(folderPath);
                    string folderName = mainDir.Name; // Get the name of the selected folder

                    foreach (DirectoryInfo subDir in mainDir.GetDirectories())
                    {
                        folderName += $",{subDir.Name}"; // Add subfolder name separated by comma
                    }

                    newParser = new();
                    newParser.LoadUniversal(mainDir);


                    foreach (var denovo in newParser.DictDenovo.Values.SelectMany(x => x))
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
                        row["File"] = newParser.FileDictionary[denovo.File];
                        row["ScanNumber"] = denovo.ScanNumber;
                        row["Sequence"] = denovo.Peptide;
                        row["Score"] = denovo.Score;
                        row["AAScores"] = string.Join("-", denovo.AaScore);

                        dtDenovo.Rows.Add(row);

                    }

                    foreach (var psm in newParser.DictPsm.Values.SelectMany(x => x))
                    {
                        DataRow row = dtPSM.NewRow();
                        row["Folder"] = folderName;
                        row["File"] = newParser.FileDictionary[psm.File];
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



                int totalPsmRegistries = newParser.DictPsm.Values.Sum(list => list.Count);
                int totalDenovoRegistries = newParser.DictDenovo.Values.Sum(list => list.Count);

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
            foreach (var kvp in newParser.DictDenovo)
            {
                categoryAxis1.Labels.Add(kvp.Key);
            }

            // Add DictPsm dictionary folders to the category axis
            foreach (var kvp in newParser.DictPsm)
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
                        row["File"] = newParser.FileDictionary[denovo.File];
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
                        row["File"] = newParser.FileDictionary[psm.File];
                        row["ScanNumber"] = psm.ScanNumber;
                        row["Sequence"] = psm.Peptide;
                        row["Score"] = psm.Score;

                        dtPSM.Rows.Add(row);
                    }
                }

                DataView dvPSM = new DataView(dtPSM);
                DataGridPSM.ItemsSource = dvPSM;
            }


            // From a dictionary of data psmDictTemp, select all the clean sequences (CleanPeptide) of PSMs, remove duplicates, and store them in a list named sequencesPSM.
            List<string> sequencesNovorPSM =
                (from s in psmDictTemp.Values
                 from psmID in s
                 select psmID.CleanPeptide).Distinct().ToList();

            // From a dictionary of data deNovoDictTemp, select all the clean sequences (CleanPeptide) of deNovo, remove duplicates, and store them in a list named sequencesDeNovo.
            List<string> sequencesNovorDeNovo =
                (from s in deNovoDictTemp.Values
                 from denovoID in s
                 select denovoID.CleanPeptide).Distinct().ToList();

            // Concatenate the PSM and deNovo sequence lists into a single list named filteredSequences.
            List<string> filteredSequences = sequencesNovorPSM.Concat(sequencesNovorDeNovo).ToList();


            // Disable the DataGridContig to prevent user interaction while data is being loaded.
            DataGridContig.IsEnabled = true;

            // Make a loading label visible to inform the user that data is being loaded.
            loadingLabel.Visibility = Visibility.Visible;

            UptadeContig();
        }

        async void UptadeContig()
        {

            // How many amino acids should overlap for contigs (partially overlapping sequences).
            int overlapAAForContigs = (int)IntegerUpDownAAOverlap.Value;

            // Execute the contig assembly process with the filtered sequences and the previously defined overlap value on a background task.
            myContigs = await Task.Run
            (
                () =>
                {
                    ContigAssembler ca = new ContigAssembler();
                    List<IDResult> results = new List<IDResult>();

                    var resultsPSM = (from kvp in psmDictTemp
                                      from r in kvp.Value
                                      select r).ToList();

                    var resultsDenovo = (from kvp in deNovoDictTemp
                                         from r in kvp.Value
                                         select r).ToList();


                    return ca.AssembleContigSequences(resultsPSM.Concat(resultsDenovo).ToList(), overlapAAForContigs);
                });

            ButtonProcess.IsEnabled = true;

            // Set the item source of DataGridContig to be an anonymous list containing the assembled contigs.
            DataGridContig.ItemsSource = myContigs.Select(a => new { Sequence = a.Sequence, IDTotal = a.IDs.Count(), IDsDenovo = a.IDs.Count(a => !a.IsPSM), IDsPSM = a.IDs.Count(a => a.IsPSM) });

            // Hide the loading label as the data has now been loaded.
            loadingLabel.Visibility = Visibility.Hidden;
        }

        //---------------------------------------------------------

        private void UpdateAlignmentGrid(int minIdentity, int minNormalizedSimilarity)
        {
            // Apply filters on the data
            List<Alignment> filteredAlnResults = myAlignment.Where(a => a.Identity >= minIdentity && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();


            DataTable dataTable = new DataTable();

            // Define the DataTable columns with the appropriate data types
            dataTable.Columns.Add("Identity", typeof(int));
            dataTable.Columns.Add("Normalized Identity Score", typeof(double));
            dataTable.Columns.Add("Similarity Score", typeof(int));
            dataTable.Columns.Add("Normalized Similarity", typeof(double));
            dataTable.Columns.Add("AlignedAA", typeof(int));
            dataTable.Columns.Add("Normalized AlignedAA", typeof(double));
            dataTable.Columns.Add("Gaps Used", typeof(int));
            dataTable.Columns.Add("Start Positions", typeof(string));
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
                newRow[7] = string.Join(",", alignment.StartPositions);
                newRow[8] = alignment.AlignedLargeSequence;
                newRow[9] = alignment.AlignedSmallSequence;

                dataTable.Rows.Add(newRow);
            }

            // Set the DataTable as the data source for your control 
            DataGridAlignments.ItemsSource = null; // Clear previous items
            DataGridAlignments.ItemsSource = dataTable.DefaultView;
        }

        //---------------------------------------------------------

        // Update in Dicionarys DeNovo and Psm
        private void UpdateGeneral()
        {
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;

            //Reset the temporary Dictionary
            deNovoDictTemp = new Dictionary<string, List<IDResult>>();
            psmDictTemp = new Dictionary<string, List<IDResult>>();

            int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
            int denovoMinScore = (int)IntegerUpDownDeNovoScore.Value;

            foreach (var kvp in newParser.DictDenovo)
            {

                List<IDResult> listNovor = (from rg in kvp.Value.Select(a => a).ToList()
                                            from rg2 in DeNovoTagExtractor.DeNovoRegistryToTags(rg, denovoMinScore, denovoMinSequeceLength)
                                            select rg2).ToList();

                deNovoDictTemp.Add(kvp.Key, listNovor);
            }

            foreach (var kvp in newParser.DictPsm)
            {
                psmDictTemp.Add(kvp.Key, kvp.Value.Select(a => a).ToList());
            }
            //---------------------------------------------------------

            // Apply filters to the filtered dictionaries

            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            Parser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            Parser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            double filterPsmSocore = (int)IntegerUpDownPSMScore.Value;
            Parser.FilterSequencesByScorePSM(filterPsmSocore, psmDictTemp);

            // Update the GUI
            UpdatePlot();
            UpdateDataView();
        }

        //---------------------------------------------------------


        //Method is a open fasta file 
        private void ButtonProcess_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Fasta Files (*.fasta)|*.fasta";

            if (openFileDialog.ShowDialog() == true)
            {
                myFasta = FastaFormat.LoadFasta(openFileDialog.FileName);
                DataGridFasta.ItemsSource = myFasta;

                // Assuming you have SequenceAlignment class and ProteinAlignment method
                int maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                int minIdentity = (int)NormalizedSimilarityUpDown.Value;
                int minNormalizedSimilarity = (int)IdentityUpDown.Value;
                SequenceAligner aligner = new SequenceAligner(maxGaps: maxGaps, gapPenalty: -2, ignoreILDifference: true);

                myAlignment = myContigs.Select(a => aligner.AlignSequences(myFasta[0].Sequence, a.Sequence)).ToList();


                // Call the UpdateAlignmentGrid method with the required parameters
                UpdateAlignmentGrid(minIdentity, minNormalizedSimilarity);

                // After processing, enable the "Results" 
                TabItemResults.IsEnabled = true;

                // Abra o TabItemResults
                TabControlMain.SelectedItem = TabItemResults;


            }
        }


        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateGeneral();
        }

        private void ButtonUpdateResult_Click(object sender, RoutedEventArgs e)
        {
            int minIdentity = IdentityUpDown.Value ?? 0;
            int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;

            UpdateAlignmentGrid(minIdentity, minNormalizedSimilarity);

        }


        private void DataGridDeNovo_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridPSM_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (myContigs != null)
            {
                UptadeContig();

            }
            else
            {

                Console.WriteLine("Contigs is null");

            }

        }

        //------------------------------------------------------------------------------------------------
        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void ButtonDownloadData_Click(object sender, RoutedEventArgs e)
        {
            // Use SaveFileDialog to allow the user to choose the location and name of the file
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "CSV Files (*.csv)|*.csv";
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.Title = "Save CSV File";

            if (saveFileDialog.ShowDialog() == true)
            {
                // Call a method to export data from DataGrids to CSV
                ExportDataGridsToCSV(DataGridFasta, DataGridAlignments, saveFileDialog.FileName);
            }
        }

        private void ExportDataGridsToCSV(DataGrid dataGridFasta, DataGrid dataGridAlignments, string filePath)
        {
            var lines = new List<string>();

            // Add the DataGridFasta header
            var headerFasta = new[] { "Description", "Template Sequence" };
            lines.Add(string.Join(",", headerFasta));

            // Add the DataGridFasta rows
            foreach (var item in (IEnumerable<SequenceAssemblerLogic.Fasta>)dataGridFasta.ItemsSource)
            {
                var column1 = item.Description;
                var column2 = item.Sequence;

                // Add lines to the final result
                lines.Add($"{column1},{column2}");
            }

            // Add a blank line between DataGrids
            lines.Add(string.Empty);

            // Add DataGridAlignments header
            var headerAlignments = new[] { " Assembling Sequences" };
            lines.Add(string.Join(",", headerAlignments));

            // Add the DataGridAlignments lines
            foreach (DataRowView dataItem in dataGridAlignments.ItemsSource)
            {
                var largeSequence = dataItem["Aligned Large Sequence"].ToString();
                var smallSequence = dataItem["Aligned Small Sequence"].ToString();

                // Add lines to the final result
                lines.Add($"Large Sequence: {largeSequence}");
                lines.Add($"Small Sequence: {smallSequence}");
                lines.Add(string.Empty); // Blank line
            }

            // Save to file
            System.IO.File.WriteAllLines(filePath, lines);
        }


        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implement as needed
        }
    }
}
