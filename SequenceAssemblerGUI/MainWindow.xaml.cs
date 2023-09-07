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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        Parser newParser;
        Dictionary<string, List<IDResult>> psmNovorDictTemp;
        Dictionary<string, List<IDResult>> deNovoNovorDictTemp;
        Dictionary<string, List<IDResult>> deNovoPeaksDictTemp;
        private string peptide;
        List<Contig> contigs;
        List<FASTA> MyFasta;

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

                    newParser = new();
                    newParser.LoadUniversal(mainDir);


                    foreach (var denovo in newParser.DictNovorDenovo.Values.SelectMany(x => x))
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

                    foreach (var psm in newParser.DictNovorPsm.Values.SelectMany(x => x))
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

                    foreach (var denovo in newParser.DictPeaksDenovo.Values.SelectMany(x => x))
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

                DataView dvPsm = new DataView(dtPSM);
                DataGridPSM.ItemsSource = dvPsm;

               

                int totalPsmRegistries = newParser.DictNovorPsm.Values.Sum(list => list.Count);
                int totalDenovoRegistries = newParser.DictNovorDenovo.Values.Sum(list => list.Count);
                int totalPeaksDenovoRegistries = newParser.DictPeaksDenovo.Values.Sum(list => list.Count);

                Console.WriteLine($"Total dos Registros de Psm: {totalPsmRegistries}");
                Console.WriteLine($"Total dos Registros de DeNovo: {totalDenovoRegistries}");

                LabelPSMCount.Content = totalPsmRegistries;
                LabelDeNovoCount.Content = totalDenovoRegistries;
                LabelDeNovoCount.Content = totalPeaksDenovoRegistries;

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
            foreach (var kvp in newParser.DictNovorDenovo)
            {
                categoryAxis1.Labels.Add(kvp.Key);
            }

            // Add DictPsm dictionary folders to the category axis
            foreach (var kvp in newParser.DictNovorPsm)
            {
                if (!categoryAxis1.Labels.Contains(kvp.Key))
                {
                    categoryAxis1.Labels.Add(kvp.Key);
                }
            }

            // Add the values from the dictionaries to the corresponding BarSeries
            foreach (var kvp in deNovoNovorDictTemp)
            {
                bsDeNovo.Items.Add(new BarItem { Value = kvp.Value.Select(a => a.Peptide).Distinct().Count() });
            }

            foreach (var kvp in psmNovorDictTemp)
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

            if (deNovoNovorDictTemp != null)
            {
                foreach (var kvp in deNovoNovorDictTemp)
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
            if (psmNovorDictTemp != null)
            {
                foreach (var kvp in psmNovorDictTemp)
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

            if (deNovoPeaksDictTemp != null)
            {
                foreach (var kvp in deNovoPeaksDictTemp)
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
            }
           

            // How many amino acids should overlap for contigs (partially overlapping sequences).
            int overlapAAForContigs = (int)IntegerUpDownAAOverlap.Value;

            // From a dictionary of data psmDictTemp, select all the clean sequences (CleanPeptide) of PSMs, remove duplicates, and store them in a list named sequencesPSM.
            List<string> sequencesPSM =
                (from s in psmNovorDictTemp.Values
                 from psmID in s
                 select psmID.CleanPeptide).Distinct().ToList();

            // From a dictionary of data deNovoDictTemp, select all the clean sequences (CleanPeptide) of deNovo, remove duplicates, and store them in a list named sequencesDeNovo.
            List<string> sequencesDeNovo =
                (from s in deNovoNovorDictTemp.Values
                 from denovoID in s
                 select denovoID.CleanPeptide).Distinct().ToList();

            // From a dictionary of data deNovoPeaksDictTemp, select all the clean sequences (CleanPeptide) of deNovo, remove duplicates, and store them in a list named sequencesDeNovo.
            List<string> sequencesPeaksDeNovo =
               (from s in deNovoPeaksDictTemp.Values
                from denovoID in s
                select denovoID.CleanPeptide).Distinct().ToList();

            // Concatenate the PSM and deNovo sequence lists into a single list named filteredSequences.
            List<string> filteredSequences = sequencesPSM.Concat(sequencesDeNovo).ToList();

            // Concatenate the PSM and deNovo sequencePeaks lists into a single list named filteredSequences.
            List<string> filteredSequencesPeaks = sequencesPeaksDeNovo.ToList();

            // Disable the DataGridContig to prevent user interaction while data is being loaded.
            DataGridContig.IsEnabled = false;

            // Make a loading label visible to inform the user that data is being loaded.
            loadingLabel.Visibility = Visibility.Visible;

            // Execute the contig assembly process with the filtered sequences and the previously defined overlap value on a background task.
            contigs = await Task.Run
            (
                () =>
                {
                    ContigAssembler ca = new ContigAssembler();
                    List<IDResult> results = new List<IDResult>();

                    var resultsPSM = (from kvp in psmNovorDictTemp
                                      from r in kvp.Value
                                      select r).ToList();

                    var resultsDenovo = (from kvp in deNovoNovorDictTemp
                                         from r in kvp.Value
                                         select r).ToList();

                    var resultsDenovoPeaks = (from kvp in deNovoPeaksDictTemp
                                              from r in kvp.Value
                                              select r).ToList();

                    return ca.AssembleContigSequences(resultsPSM.Concat(resultsDenovo).Concat(resultsDenovoPeaks).ToList(), overlapAAForContigs);
                }
            );

            ButtonProcess.IsEnabled = true;

            // Set the item source of DataGridContig to be an anonymous list containing the assembled contigs.
            DataGridContig.ItemsSource = contigs.Select(a => new { Sequence = a.Sequence, IDTotal = a.IDs.Count(), IDsDenovo = a.IDs.Count(a => !a.IsPSM), IDsPSM = a.IDs.Count(a => a.IsPSM) });

            // Re-enable the DataGridContig to allow user interaction.
            DataGridContig.IsEnabled = true;

            // Hide the loading label as the data has now been loaded.
            loadingLabel.Visibility = Visibility.Hidden;

        }
       
        private void UpdateGeneral()
        {
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;

            //Reset the temporary Dictionary
            deNovoNovorDictTemp = new Dictionary<string, List<IDResult>>();
            psmNovorDictTemp = new Dictionary<string, List<IDResult>>();
            deNovoPeaksDictTemp = new Dictionary<string, List<IDResult>>();

            int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
            int denovoMinScore = (int)IntegerUpDownDeNovoScore.Value;

            foreach (var kvp in newParser.DictNovorDenovo)
            {
                // Lógica para Novor DeNovo
                List<IDResult> listNovor = (from rg in kvp.Value.Select(a => a).ToList()
                                            from rg2 in DeNovoTagExtractor.DeNovoRegistryToTags(rg, denovoMinScore, denovoMinSequeceLength)
                                            select rg2).ToList();

                deNovoNovorDictTemp.Add(kvp.Key, listNovor);

                // Se DictPeaksDenovo não for nulo e contiver uma chave correspondente
                if (newParser.DictPeaksDenovo != null && newParser.DictPeaksDenovo.ContainsKey(kvp.Key))
                {
                    // Lógica para PEAKS DeNovo (usando o mesmo método de filtragem)
                    List<IDResult> listPeaks = (from rg in newParser.DictPeaksDenovo[kvp.Key].Select(a => a).ToList()
                                                from rg2 in DeNovoTagExtractor.DeNovoRegistryToTags(rg, denovoMinScore, denovoMinSequeceLength)
                                                select rg2).ToList();

                    deNovoPeaksDictTemp.Add(kvp.Key, listPeaks);
                }
            }

            foreach (var kvp in newParser.DictNovorPsm)
                {
                psmNovorDictTemp.Add(kvp.Key, kvp.Value.Select(a => a).ToList());
            }
            //---------------------------------------------------------

            // Apply filters to the filtered dictionaries

            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoNovorDictTemp);
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoPeaksDictTemp); 

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            Parser.FilterDictMinLengthPSM(psmMinSequenceLength, psmNovorDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            Parser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmNovorDictTemp);

            double filterPsmSocore = (int)IntegerUpDownPSMScore.Value;
            Parser.FilterSequencesByScorePSM(filterPsmSocore, psmNovorDictTemp);

            // Update the GUI
            UpdatePlot();
            UpdateDataView();
        }

        //Method is a open fasta file 
        private void ButtonProcess_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "FASTA Files (*.fasta)|*.fasta";

            if (openFileDialog.ShowDialog() == true)
            {
                MyFasta = Useful.LoadFasta(openFileDialog.FileName);
                DataGridFasta.ItemsSource = MyFasta;

                // Initialize the StringBuilder to combine the contents
                StringBuilder combinedContent = new StringBuilder();

                // Adds the contents of the selected FASTA file
                combinedContent.AppendLine(File.ReadAllText(openFileDialog.FileName));

                // Add the contigs to the content in FASTA format
                combinedContent.AppendLine(SequenceAssemblerLogic.Useful.ContigsToFastaFormat(contigs)); 

                // Define the path where the combined file will be saved
                string savePath = Path.Combine(Path.GetDirectoryName(openFileDialog.FileName), "combinedOutput.txt");

                // Save the combined content in a new .txt file
                File.WriteAllText(savePath, combinedContent.ToString());

                // Parse the combined file and store the strings in a list
                List<FASTA> fastaSequences = SequenceAssemblerLogic.FastaParser.ParseFastaFile(savePath);

                Console.WriteLine($"File generated at {savePath}");

                if (fastaSequences.Count > 0)
                {
                    Console.WriteLine($"Number of contigs: {fastaSequences.Count}\nSequence Fasta: {fastaSequences[0].ID}");

                    SequenceAligner aligner = new SequenceAligner();
                    List<FASTA> alignments = aligner.AlignSequences(fastaSequences);

                    List<(string word, int position)> aaPos = new();

                    if (alignments.Count > 1) 
                    {

                    }

                    Console.WriteLine(string.Join("\n", fastaSequences.Select(f => $"{f.ID}: {f.Sequence}")));
                }
                else
                {
                    Console.WriteLine("No valid sequences found in the file.");
                }
            }
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (contigs != null)
            {
                foreach (Contig c in contigs)
                {
                    int total = c.IDs.Count;
                    int denovo = c.IDs.Count(a => !a.IsPSM);
                    int psm = c.IDs.Count(a => a.IsPSM);

                    if (total == denovo + psm)
                    {
                        Console.WriteLine("Sequence {0}\nTotal {1}, DeNovo {2}, PSMs {3}", c.Sequence, total, denovo, psm);
                    }
                    Console.Write("");
                }
            }
            else
            {
                Console.WriteLine("contigs is null");
            }
        }
    }

} 



