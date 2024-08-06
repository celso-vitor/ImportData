using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.ResultParser;
using SequenceAssemblerLogic.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static SequenceAssemblerGUI.Assembly;
using SeproPckg2;
using PatternTools.FastaTools;
using System.Reflection;
using static PatternTools.SparseMatrixIndexParserV2;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Text;
using SequenceAssemblerLogic;

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        Parser newParser;
        Dictionary<string, List<IDResult>> psmDictTemp;
        Dictionary<string, List<IDResult>> deNovoDictTemp;

        private DispatcherTimer updateTimer;
        List<Fasta> allFastaSequences;
        List<(string ID, string Sequence, string Description)> alignedSequences;
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
            new DataColumn("ScanNumber", typeof(int)),
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
            new DataColumn("ScanNumber", typeof(int)),
        }
        };
        private List<string> filteredSequences;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the DispatcherTimer
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1); // Set the delay to 1 second
            updateTimer.Tick += UpdateTimer_Tick;

            // Attach event handlers
            IntegerUpDownDeNovoMinLength.ValueChanged += (s, e) => RestartTimer();
            IntegerUpDownDeNovoMaxLength.ValueChanged += (s, e) => RestartTimer();
            IntegerUpDownDeNovoScore.ValueChanged += (s, e) => RestartTimer();
            IntegerUpDownPSMMinLength.ValueChanged += (s, e) => RestartTimer();
            IntegerUpDownPSMMaxLength.ValueChanged += (s, e) => RestartTimer();
            IntegerUpDownPSMScore.ValueChanged += (s, e) => RestartTimer();
        }

        // Event handler for the DispatcherTimer
        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            updateTimer.Stop(); // Stop the timer
            UpdateGeneral(); // Call the update method

            ShowLoadingIndicator();

            await Task.Run(() =>
            {
                try
                {
                    // Reexecutar o alinhamento apropriado baseado no modo de alinhamento selecionado
                    if (lastOpenedFileNames != null && lastOpenedFileNames.Length > 0)
                    {
                        if (isMultipleAlignmentMode)
                        {
                            MultipleAlignment(lastOpenedFileNames); // Use a variável que armazena os nomes dos arquivos FASTA carregados
                        }
                        else
                        {
                            LocalAlignment(lastOpenedFileNames); // Use a variável que armazena os nomes dos arquivos FASTA carregados
                        }
                    }
                }
                finally
                {
                    HideLoadingIndicator();
                }
            });
        }



        // Method to restart the timer
        private void RestartTimer()
        {
            updateTimer.Stop(); // Stop the timer if it's already running
            updateTimer.Start(); // Start the timer
        }


        private async void MenuItemImportResults_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = true;

            if ((bool)folderBrowserDialog.ShowDialog())
            {
                ShowLoadingIndicator();

                await Task.Run(() =>
                {
                    try
                    {
                        dtDenovo.Clear();
                        dtPSM.Clear();
                        List<string> sequencesForAssembly = new();

                        foreach (string folderPath in folderBrowserDialog.SelectedPaths)
                        {
                            DirectoryInfo mainDir = new DirectoryInfo(folderPath);
                            string folderName = mainDir.Name;

                            foreach (DirectoryInfo subDir in mainDir.GetDirectories())
                            {
                                folderName += $",{subDir.Name}";
                            }

                            newParser = new();
                            newParser.LoadUniversal(mainDir);

                            foreach (var denovo in newParser.DictDenovo.Values.SelectMany(x => x))
                            {
                                DataRow row = dtDenovo.NewRow();
                                row["IsTag"] = denovo.IsTag ? "T" : "F";
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

                        Dispatcher.Invoke(() =>
                        {
                            DataView dvDenovo = new DataView(dtDenovo);
                            DataGridDeNovo.ItemsSource = dvDenovo;

                            DataView dvPsm = new DataView(dtPSM);
                            DataGridPSM.ItemsSource = dvPsm;

                            UpdateGeneral();

                            DeNovoAssembly.IsSelected = true;
                            TabItemResultBrowser.IsEnabled = false;
                            TabItemResultBrowser2.IsEnabled = false;

                            // Ocultar a imagem inicial após a importação dos resultados
                            BorderStart.Visibility = Visibility.Collapsed;
                        });
                    }
                    finally
                    {
                        Dispatcher.Invoke(HideLoadingIndicator);
                    }
                });
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

            // Add the values from the filtered dictionaries to the corresponding BarSeries
            foreach (var kvp in deNovoDictTemp)
            {
                bsDeNovo.Items.Add(new BarItem { Value = kvp.Value.Count });
            }

            foreach (var kvp in psmDictTemp)
            {
                bsPSM.Items.Add(new BarItem { Value = kvp.Value.Count });
            }

            plotModel1.Series.Add(bsPSM);
            plotModel1.Series.Add(bsDeNovo);
            plotModel1.Axes.Add(linearAxis);
            plotModel1.Axes.Add(categoryAxis1);
            PlotViewEnzymeEfficiency.Model = plotModel1;
        }

        public async void UpdateDataView()
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

            // Update the counts in the UI
            Dispatcher.Invoke(() =>
            {
                int filteredPsmCount = psmDictTemp.Values.Sum(list => list.Count);
                int filteredDeNovoCount = deNovoDictTemp.Values.Sum(list => list.Count);

                LabelPSMCount.Content = filteredPsmCount.ToString();
                LabelDeNovoCount.Content = filteredDeNovoCount.ToString();
            });
        }

        //---------------------------------------------------------

        // Update in Dicionarys DeNovo and Psm
        private void UpdateGeneral()
        {
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;

            // Reset the temporary Dictionary
            deNovoDictTemp = new Dictionary<string, List<IDResult>>();
            psmDictTemp = new Dictionary<string, List<IDResult>>();

            int totalPsmRegistries = newParser.psmDictTemp.Values.Sum(list => list.Count);
            int totalDenovoRegistries = newParser.deNovoDictTemp.Values.Sum(list => list.Count);

            Console.WriteLine($"Total dos Registros de Psm: {totalPsmRegistries}");
            Console.WriteLine($"Total dos Registros de DeNovo: {totalDenovoRegistries}");

            int denovoMinSequeceLength = (int)IntegerUpDownDeNovoMinLength.Value;
            int denovoMinScore = (int)IntegerUpDownDeNovoScore.Value;

            foreach (var kvp in newParser.DictDenovo)
            {
                List<IDResult> listNovor = (from rg in kvp.Value
                                            from rg2 in DeNovoTagExtractor.DeNovoRegistryToTags(rg, denovoMinScore, denovoMinSequeceLength)
                                            select rg2).ToList();

                deNovoDictTemp.Add(kvp.Key, listNovor);
            }

            foreach (var kvp in newParser.DictPsm)
            {
                psmDictTemp.Add(kvp.Key, kvp.Value.ToList());
            }

            // Apply filters to the filtered dictionaries
            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            Parser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            Parser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            double filterPsmScore = (double)IntegerUpDownPSMScore.Value;
            Parser.FilterSequencesByScorePSM(filterPsmScore, psmDictTemp);

            // Counts for filtered PSM and DeNovo
            int filteredPsmCount = psmDictTemp.Values.Sum(list => list.Count);
            int filteredDeNovoCount = deNovoDictTemp.Values.Sum(list => list.Count);

            // Updates the list of filtered sequences
            filteredSequences = deNovoDictTemp.Values.SelectMany(v => v)
                                .Union(psmDictTemp.Values.SelectMany(v => v))
                                .Select(seq => seq.CleanPeptide)
                                .Distinct()
                                .ToList();

            // Update misAlignment variable to reflect updated alignments
            myAlignment = filteredSequences.Select(seq => new Alignment()).ToList();

            // Update the GUI
            Dispatcher.Invoke(() =>
            {
                UpdatePlot();
                UpdateDataView();

                // Update the counts in the UI
                LabelPSMCount.Content = filteredPsmCount.ToString();
                LabelDeNovoCount.Content = filteredDeNovoCount.ToString();
            });
        }
        //---------------------------------------------------------
        //Method is a open fasta file 

        // Variable to track selected alignment mode
        private bool isMultipleAlignmentMode = true;

        // Method fired when single alignment mode RadioButton is selected
        private void RadioButtonAlignmentMode_Checked(object sender, RoutedEventArgs e)
        {
            isMultipleAlignmentMode = false;
            ButtonProcess.IsEnabled = true;

        }

        // Method fired when multi-alignment mode RadioButton is selected
        private void RadioButtonMultipleAlignmentMode_Checked(object sender, RoutedEventArgs e)
        {
            isMultipleAlignmentMode = true;
            ButtonProcess.IsEnabled = true;
        }

        private string[] lastOpenedFileNames; // Variável para armazenar os nomes dos arquivos abertos

        // Dentro do método ButtonProcess_Click, armazene os nomes dos arquivos:
        private async void ButtonProcess_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog
            {
                Multiselect = true,
                Filter = "Fasta Files (*.fasta)|*.fasta"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ShowLoadingIndicator();
                lastOpenedFileNames = openFileDialog.FileNames; // Armazena os nomes dos arquivos selecionados

                await Task.Run(() =>
                {
                    try
                    {
                        if (isMultipleAlignmentMode)
                        {
                            MultipleAlignment(lastOpenedFileNames);
                        }
                        else
                        {
                            LocalAlignment(lastOpenedFileNames);
                        }
                    }
                    finally
                    {
                        Dispatcher.Invoke(HideLoadingIndicator);
                    }
                });
            }
        }



        //---------------------------------------------------------

        // Multiple Alignment
        private void MultipleAlignment(string[] fileNames)
        {
            var loadedFastaFiles = fileNames.Select(FastaFormat.LoadFasta).ToList();

            // Checks if at least one FASTA file was loaded correctly
            if (loadedFastaFiles == null || loadedFastaFiles.Any(fasta => fasta == null || !fasta.Any()))
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Failed to load one or more FASTA files. Some files are empty or not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            // Stores loaded FASTA files
            var allFastaSequences = loadedFastaFiles.SelectMany(fasta => fasta).ToList();

            // Check if at least two sequences are loaded
            if (allFastaSequences.Count < 2)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("You must select at least two FASTA sequences for multiple alignment.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            Console.WriteLine($"Loaded {allFastaSequences.Count} fasta sequences.");

            // Perform multi-sequence alignment using Clustal
            FastaFileParser fastaFileParser = new FastaFileParser();
            foreach (var file in fileNames)
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    fastaFileParser.ParseFile(reader, false);
                }
            }

            ClustalMultiAligner clustalMultiAligner = new ClustalMultiAligner();
            var msaResult = clustalMultiAligner.AlignSequences(fastaFileParser.MyItems);
            Console.WriteLine("Multi-sequence alignment complete.");

            // Print each sequence from the multi-sequence alignment and store them
            Console.WriteLine("Aligned Sequences:");

            Dictionary<string, (string Sequence, string Description)> concatenatedSequences = new();

            foreach (var seq in msaResult.alignments)
            {
                if (concatenatedSequences.ContainsKey(seq.SequenceIdentifier))
                {
                    var existing = concatenatedSequences[seq.SequenceIdentifier];
                    concatenatedSequences[seq.SequenceIdentifier] = (existing.Sequence + seq.Sequence, existing.Description);
                }
                else
                {
                    concatenatedSequences.Add(seq.SequenceIdentifier, (seq.Sequence, seq.Description));
                }
            }

            alignedSequences = concatenatedSequences
                                   .Select(kvp => (kvp.Key, kvp.Value.Sequence, kvp.Value.Description))
                                   .OrderBy(tuple => tuple.Key)
                                   .ToList();

            if (filteredSequences != null && filteredSequences.Any())
            {
                // Gets the origins of the filtered sequences
                var sourceOrigins = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);

                int maxGaps = 0;
                int minNormalizedIdentityScore = 0;
                int minNormalizedSimilarity = 0;
                int minLengthFilter = 0;
                int minConsecutiveAminoAcids = 0;

                Dispatcher.Invoke(() =>
                {
                    //maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    minLengthFilter = (int)IntegerUpDownMinimumLength.Value;
                    
                });

                // Filter sequences by normalized length before alignment
                filteredSequences = Utils.FilterSequencesByNormalizedLength(filteredSequences, string.Join("", allFastaSequences.Select(f => f.Sequence)), minLengthFilter);

                SequenceAligner alignermsa = new SequenceAligner();
                myAlignment = new List<Alignment>();

                int psmUsedCount = 0;
                int deNovoUsedCount = 0;

                foreach (var sequence in alignedSequences)
                {
                    var alignment = filteredSequences
                        .Where(seq => seq.Length >= minLengthFilter) // Filter sequences based on minLengthFilter here
                        .Select((seq, index) =>
                        {
                            var alignment = alignermsa.AlignerMSA(msaResult.consensus, seq, "Sequence: " + sourceOrigins[index].sequence + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].identificationMethod);
                            alignment.TargetOrigin = sequence.ID; // Add SourceOrigin to alignment

                            // Update counts based on identification method
                            if (sourceOrigins[index].identificationMethod == "PSM")
                            {
                                psmUsedCount++;
                            }
                            else if (sourceOrigins[index].identificationMethod == "DeNovo")
                            {
                                deNovoUsedCount++;
                            }

                            return alignment;
                        }).Where(aln => aln.GapsUsed <= maxGaps).ToList(); // Filtrar por maxGaps aqui
                    myAlignment.AddRange(alignment);
                }

                // Updates the alignment view with the necessary parameters
                List<Alignment> alignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);

                Dispatcher.Invoke(() =>
                {
                    UpdateViewMultipleModel(alignedSequences, alignments);
                    UpdateMultiAlignmentTable();

                    // Update the UI with the counts
                    PSMUsedCount.Content = psmUsedCount.ToString();
                    DeNovoUsedCount.Content = deNovoUsedCount.ToString();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("There are no sequences to align. Please filter the sequences before attempting to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private void UpdateMultiAlignmentTable()
        {
            Dispatcher.Invoke(() =>
            {
                //int maxGaps = IntegerUpDownMaximumGaps.Value ?? 0;
                int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
                int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
                int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;

                var filteredAlignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);
                UpdateViewMultipleModel(alignedSequences, filteredAlignments);

                int psmUsedCount = filteredAlignments.Count(a => a.SourceOrigin.Contains("PSM"));
                int deNovoUsedCount = filteredAlignments.Count(a => a.SourceOrigin.Contains("DeNovo"));

                // Log dos valores para depuração
                Console.WriteLine($"PSM Used Count: {psmUsedCount}");
                Console.WriteLine($"DeNovo Used Count: {deNovoUsedCount}");

                PSMUsedCount.Content = psmUsedCount.ToString();
                DeNovoUsedCount.Content = deNovoUsedCount.ToString();
            });
        }

        private void UpdateViewMultipleModel(List<(string Key, string Sequence, string Description)> alignedSequences, List<Alignment> alignments)
        {
            Dispatcher.Invoke(() =>
            {
                MyMultipleAlignment.UpdateViewMultipleModel(alignedSequences, alignments);
                TabItemResultBrowser2.IsSelected = true;
                NormalizedSimilarityUpDown.IsEnabled = true;
                IdentityUpDown.IsEnabled = true;
                IntegerUpDownMinimumLength.IsEnabled = true;
                //IntegerUpDownMaximumGaps.IsEnabled = true;
                TabItemResultBrowser2.IsEnabled = true;
                ButtonUpdateAssembly.IsEnabled = true;
                MyMultipleAlignment.ExecuteAssembly();
            });
        }

        //---------------------------------------------------------

        // Local Alignment
        private void LocalAlignment(string[] fileNames)
        {
            var loadedFastaFiles = fileNames.Select(FastaFormat.LoadFasta).ToList();

            // Checks if at least one FASTA file was loaded correctly
            if (loadedFastaFiles == null || loadedFastaFiles.Any(fasta => fasta == null || !fasta.Any()))
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Failed to load one or more FASTA files. Some files are empty or not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            // Stores loaded FASTA files
            allFastaSequences = loadedFastaFiles.SelectMany(fasta => fasta).ToList();

            Console.WriteLine($"Loaded {allFastaSequences.Count} fasta sequences.");

            if (filteredSequences != null && filteredSequences.Any())
            {
                // Gets the origins of the filtered sequences
                List<(string folder, string sequence, string origin)> sourceOrigins = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);

                int maxGaps = 0;
                int minNormalizedIdentityScore = 0;
                int minNormalizedSimilarity = 0;
                int minLengthFilter = 0;
                int minConsecutiveAminoAcids = 0;

                Dispatcher.Invoke(() =>
                {
                    //maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    minLengthFilter = (int)IntegerUpDownMinimumLength.Value;
                });

                SequenceAligner aligner = new SequenceAligner();

                // Initialize counts for PSM and DeNovo used in alignments
                int psmUsedCount = 0;
                int deNovoUsedCount = 0;

                // Filter sequences by normalized length before alignment
                filteredSequences = Utils.FilterSequencesByNormalizedLength(filteredSequences, string.Join("", allFastaSequences.Select(f => f.Sequence)), minLengthFilter);

                // Align PSM and De Novo sequences with sequences from all FASTA files
                myAlignment = new List<Alignment>();
                foreach (var fastaSequence in allFastaSequences)
                {
                    Console.WriteLine($"Processing fasta sequence: {fastaSequence.ID}");
                    var alignments = filteredSequences
                        .Where(seq => seq.Length >= minLengthFilter) // Filter sequences based on minLengthFilter here
                        .Select((seq, index) =>
                        {
                            var alignment = aligner.AlignerLocal(fastaSequence.Sequence, seq, "Sequence: " + sourceOrigins[index].sequence + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].origin);
                            alignment.TargetOrigin = fastaSequence.ID; // Adds the target origin to the alignment

                            // Update counts based on identification method
                            if (sourceOrigins[index].origin == "PSM")
                            {
                                psmUsedCount++;
                            }
                            else if (sourceOrigins[index].origin == "DeNovo")
                            {
                                deNovoUsedCount++;
                            }

                            return alignment;
                        }).Where(aln => aln.GapsUsed <= maxGaps).ToList(); // Filtrar por maxGaps aqui
                    myAlignment.AddRange(alignments);
                }

                Console.WriteLine($"Generated {myAlignment.Count} alignments.");

                // Filters alignment results based on defined criteria
                List<Alignment> filteredAlnResults = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);

                Dispatcher.Invoke(() =>
                {
                    // Update the ViewModel in the Assembly control
                    MyAssembly.UpdateViewLocalModel(allFastaSequences, filteredAlnResults);
                    UpdateLocalTable();

                    // Log dos valores para depuração
                    Console.WriteLine($"PSM Used Count: {psmUsedCount}");
                    Console.WriteLine($"DeNovo Used Count: {deNovoUsedCount}");
                    // Update the UI with the counts
                    PSMUsedCount.Content = psmUsedCount.ToString();
                    DeNovoUsedCount.Content = deNovoUsedCount.ToString();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("There are no sequences to align. Please filter the sequences before attempting to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }


        private void UpdateLocalTable()
        {
            Dispatcher.Invoke(() =>
            {
                //int maxGaps = IntegerUpDownMaximumGaps.Value ?? 0;
                int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
                int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
                int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;

                var filteredAlignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);
                UpdateViewLocalModel(allFastaSequences, filteredAlignments);

                int psmUsedCount = filteredAlignments.Count(a => a.SourceOrigin.Contains("PSM"));
                int deNovoUsedCount = filteredAlignments.Count(a => a.SourceOrigin.Contains("DeNovo"));

                // Log dos valores para depuração
                Console.WriteLine($"PSM Used Count: {psmUsedCount}");
                Console.WriteLine($"DeNovo Used Count: {deNovoUsedCount}");

                PSMUsedCount.Content = psmUsedCount.ToString();
                DeNovoUsedCount.Content = deNovoUsedCount.ToString();
            });
        }




        private void UpdateViewLocalModel(List<Fasta> allFastaSequences, List<Alignment> alignments)
        {
            Dispatcher.Invoke(() =>
            {
                MyAssembly.UpdateViewLocalModel(allFastaSequences, alignments);
                TabItemResultBrowser.IsSelected = true;
                NormalizedSimilarityUpDown.IsEnabled = true;
                IdentityUpDown.IsEnabled = true;
                IntegerUpDownMinimumLength.IsEnabled = true;
                //IntegerUpDownMaximumGaps.IsEnabled = true;
                TabItemResultBrowser.IsEnabled = true;
                ButtonUpdateAssembly.IsEnabled = true;
                MyAssembly.ExecuteAssembly();
            });
        }
        private List<Alignment> FilterAlignments(List<Alignment> alignments, int minNormalizedIdentityScore, int minNormalizedSimilarity, int minLengthFilter)
        {
            return alignments
                .Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore &&
                            a.NormalizedSimilarity >= minNormalizedSimilarity &&           
                            a.AlignedSmallSequence.Length >= minLengthFilter)
                .ToList();
        }




        //---------------------------------------------------------

        // Update Alignments
        private void ButtonUpdate_Assembly(object sender, RoutedEventArgs e)
        {
            ShowLoadingIndicator();

            Task.Run(() =>
            {
                try
                {
                    if (myAlignment == null || !myAlignment.Any())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("There are no alignments to filter. Please align sequences first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        return;
                    }

                    // Obter valores dos filtros da UI
                    int maxGaps = 0;
                    int minNormalizedIdentityScore = 0;
                    int minNormalizedSimilarity = 0;
                    int minLengthFilter = 0;

                    Dispatcher.Invoke(() =>
                    {
                        //maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                        minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                        minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                        minLengthFilter = (int)IntegerUpDownMinimumLength.Value;
                    });

                    // Filtrar os alinhamentos existentes com base nos critérios definidos
                    var filteredAlignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);

                    Dispatcher.Invoke(() =>
                    {
                        if (isMultipleAlignmentMode)
                        {
                            UpdateViewMultipleModel(alignedSequences, filteredAlignments);
                            UpdateMultiAlignmentTable();
                        }
                        else
                        {
                            UpdateViewLocalModel(allFastaSequences, filteredAlignments);
                            UpdateLocalTable();
                        }
                    });
                }
                finally
                {
                    Dispatcher.Invoke(HideLoadingIndicator);
                }
            });
        }



        private void ShowLoadingIndicator()
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = Visibility.Visible;
            });
        }

        private void HideLoadingIndicator()
        {
            Dispatcher.Invoke(() =>
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            });
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

