using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic;
using SequenceAssemblerLogic.ContigCode;
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

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        Parser newParser;
        Dictionary<string, List<IDResult>> psmDictTemp;
        Dictionary<string, List<IDResult>> deNovoDictTemp;


        private DispatcherTimer updateTimer;
        List<Contig> myContigs;
        List<Fasta> allFastaSequences;
        List<(string ID, string Sequence)> alignedSequences;
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
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            updateTimer.Stop(); // Stop the timer
            UpdateGeneral(); // Call the update method
        }

        // Method to restart the timer
        private void RestartTimer()
        {
            updateTimer.Stop(); // Stop the timer if it's already running
            updateTimer.Start(); // Start the timer
        }
        private void MenuItemImportResults_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Multiselect = true;

            if ((bool)folderBrowserDialog.ShowDialog())
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

                UpdateGeneral();
                DeNovoAssembly.IsSelected = true;
                TabItemResultBrowser.IsEnabled = false;
                TabItemResultBrowser2.IsEnabled = false;
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

                ButtonProcess.IsEnabled = true;
            }

            // Retrieves PSM and DeNovo sequences
            Dictionary<string, string> sequencesNovorPSM =
                (from s in psmDictTemp.Values
                 from psmID in s
                 select new { psmID.CleanPeptide, Source = "PSM" })
                .Distinct()
                .ToDictionary(p => p.CleanPeptide, p => p.Source);

            Dictionary<string, string> sequencesNovorDeNovo =
                (from s in deNovoDictTemp.Values
                 from denovoID in s
                 select new { denovoID.CleanPeptide, Source = "DeNovo" })
                .Distinct()
                .ToDictionary(p => p.CleanPeptide, p => p.Source);

            // Creation of resultsPSM and resultsDenovo lists
            var resultsPSM = psmDictTemp.SelectMany(kvp => kvp.Value).ToList();
            var resultsDenovo = deNovoDictTemp.SelectMany(kvp => kvp.Value).ToList();

            LabelPSMCount.Content = resultsPSM.Count;
            LabelDeNovoCount.Content = resultsDenovo.Count;
            // Debug messages
            //Console.WriteLine("Sequences from PSM:");
            //foreach (var sequence in sequencesNovorPSM.Keys)
            //{
            //    Console.WriteLine(sequencesNovorPSM.Keys);
            //}

            //Console.WriteLine("Sequences from DeNovo:");
            //foreach (var sequence in sequencesNovorDeNovo.Keys)
            //{
            //    Console.WriteLine(sequencesNovorDeNovo.Keys);
            //}

            DataGridContig.IsEnabled = true;
            loadingLabel.Visibility = Visibility.Visible;

            await UpdateContig();

            loadingLabel.Visibility = Visibility.Hidden;
        }

        private async Task UpdateContig()
        {
            ContigAssembly.IsEnabled = true;
            IntegerUpDownAAOverlap.IsEnabled = true;
            int overlapAAForContigs = (int)IntegerUpDownAAOverlap.Value;
            int maxContigs = 10000;// Example of count limit
            int maxTimeMilliseconds = 10000; // Example of time limit in milliseconds (10 seconds)


            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                myContigs = await Task.Run(() =>
                {
                    ContigAssembler ca = new ContigAssembler();

                    // Creation of resultsPSM and resultsDenovo lists
                    var resultsPSM = psmDictTemp.SelectMany(kvp => kvp.Value).ToList();
                    var resultsDenovo = deNovoDictTemp.SelectMany(kvp => kvp.Value).ToList();

                    //Console.WriteLine("Initial Results PSM count: " + resultsPSM.Count);
                    //Console.WriteLine("Initial Results DeNovo count: " + resultsDenovo.Count);


                    // Combine and check for duplications
                    var combinedResults = resultsPSM.Concat(resultsDenovo).ToList();
                    Console.WriteLine("Combined results count before assembling: " + combinedResults.Count);
                    var contigs = ca.AssembleContigSequencesWithLimits(combinedResults, overlapAAForContigs, maxContigs, maxTimeMilliseconds);


                    string fastaFilePath = Path.Combine("..", "..", "..", "Debug", "contigs.fasta");
                    SaveContigsToFile(fastaFilePath, contigs).Wait(); // Call asynchronous method synchronously

                    return contigs;
                });


                stopwatch.Stop();
                Console.WriteLine($"Time taken to assemble contigs: {stopwatch.ElapsedMilliseconds} ms");

                if (myContigs == null)
                {
                    Console.WriteLine("Failed to assemble contigs.");
                    return;
                }

                Console.WriteLine("Assembled Contigs count: " + myContigs.Count);

                // Update UI
                IntegerUpDownMaximumGaps.IsEnabled = true;
                ButtonProcess.IsEnabled = true;
                DataGridContig.ItemsSource = myContigs.Select(a => new
                {
                    Sequence = a.Sequence,
                    IDTotal = a.IDs.Count(),
                    IDsDenovo = a.IDs.Count(id => !id.IsPSM),
                    IDsPSM = a.IDs.Count(id => id.IsPSM)
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in UpdateContig: " + ex.Message);
            }
        }
        private async Task SaveContigsToFile(string filePath, List<Contig> contigs)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                int contigNumber = 1;
                foreach (var contig in contigs)
                {
                    await writer.WriteLineAsync($">Contig{contigNumber}");
                    await writer.WriteLineAsync(contig.Sequence);
                    contigNumber++;
                }
            }
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

            LabelPSMCount.Content = totalPsmRegistries;
            LabelDeNovoCount.Content = totalDenovoRegistries;
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

            // Apply filters to the filtered dictionaries
            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            Parser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            Parser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            double filterPsmScore = (double)IntegerUpDownPSMScore.Value;
            Parser.FilterSequencesByScorePSM(filterPsmScore, psmDictTemp);

            // Updates the list of filtered sequences
            filteredSequences = deNovoDictTemp.Values.SelectMany(v => v)
                                    .Union(psmDictTemp.Values.SelectMany(v => v))
                                    .Select(seq => seq.CleanPeptide)
                                    .Distinct()
                                    .ToList();

            // Update misAlignment variable to reflect updated alignments
            myAlignment = filteredSequences.Select(seq => new Alignment()).ToList();

            // Update the GUI
            UpdatePlot();
            UpdateDataView();
        }


        //---------------------------------------------------------
        //Method is a open fasta file 
        // Variable to track selected alignment mode
        private bool isMultipleAlignmentMode = true;

        // Method fired when single alignment mode RadioButton is selected
        private void RadioButtonAlignmentMode_Checked(object sender, RoutedEventArgs e)
        {
            isMultipleAlignmentMode = false;
        }

        // Method fired when multi-alignment mode RadioButton is selected
        private void RadioButtonMultipleAlignmentMode_Checked(object sender, RoutedEventArgs e)
        {
            isMultipleAlignmentMode = true;
        }

        // Method triggered when the processing button is clicked
        private void ButtonProcess_Click(object sender, RoutedEventArgs e)
        {
            UpdateGeneral();

            if (isMultipleAlignmentMode)
            {
                ProcessMultipleAlignment();
            }
            else
            {
                ProcessSingleAlignment();
            }
        }


        private void ProcessMultipleAlignment()
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog
            {
                Multiselect = true,
                Filter = "Fasta Files (*.fasta)|*.fasta"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var loadedFastaFiles = openFileDialog.FileNames.Select(FastaFormat.LoadFasta).ToList();

                // Checks if at least one FASTA file was loaded correctly
                if (loadedFastaFiles == null || loadedFastaFiles.Any(fasta => fasta == null || !fasta.Any()))
                {
                    MessageBox.Show("Failed to load one or more FASTA files. Some files are empty or not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Stores loaded FASTA files
                var allFastaSequences = loadedFastaFiles.SelectMany(fasta => fasta).ToList();

                Console.WriteLine($"Loaded {allFastaSequences.Count} fasta sequences.");

                // Perform multi-sequence alignment using Clustal
                FastaFileParser fastaFileParser = new FastaFileParser();
                foreach (var file in openFileDialog.FileNames)
                {
                    using (StreamReader reader = new StreamReader(file))
                    {
                        fastaFileParser.ParseFile(reader, false);
                    }
                }

                // Print each sequence before alignment
                foreach (var item in fastaFileParser.MyItems)
                {
                    Console.WriteLine($"Before Alignment - SequenceIdentifier: {item.SequenceIdentifier}");
                    Console.WriteLine($"Before Alignment - Description: {item.Description}");
                    Console.WriteLine($"Before Alignment - Sequence: {item.Sequence}");
                }

                ClustalMultiAligner clustalMultiAligner = new ClustalMultiAligner();
                var msaResult = clustalMultiAligner.AlignSequences(fastaFileParser.MyItems);
                Console.WriteLine("Multi-sequence alignment complete.");

                // Print each sequence from the multi-sequence alignment and store them
                Console.WriteLine("Aligned Sequences:");

                Dictionary<string, (string Sequence, string Description)> concatenatedSequences = new();

                foreach (var seq in msaResult.alignments)
                {
                    // Print SequenceIdentifier, Description, and Sequence after alignment
                    Console.WriteLine($"SequenceIdentifier: {seq.SequenceIdentifier}");
                    Console.WriteLine($"Description: {seq.Description}");
                    Console.WriteLine($"Sequence: {seq.Sequence}");

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

                var alignedSequences = concatenatedSequences
                                          .Select(kvp => (kvp.Key, kvp.Value.Sequence, kvp.Value.Description))
                                          .OrderBy(tuple => tuple.Key)
                                          .ToList();

                // Print the final concatenated sequences with descriptions
                foreach (var (identifier, sequence, description) in alignedSequences)
                {
                    Console.WriteLine($"SequenceIdentifier: {identifier}");
                    Console.WriteLine($"Description: {description}");
                    Console.WriteLine($"Sequence: {sequence}");
                }


                if (filteredSequences != null && filteredSequences.Any())
                {
                    //Gets the origins of the filtered sequences
                    var sourceOrigins = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);

                    int maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    int minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    int minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    int minLengthFilter = (int)IntegerUpDownMinimumLength.Value;

                    SequenceAligner alignermsa = new SequenceAligner();
                    myAlignment = new List<Alignment>();

                    var x = filteredSequences.FindAll(a => a.Contains("VADE")).ToList();


                    //foreach ((string ID, string Sequence) in alignedSequences)
                    //{

                    //    var alignment = alignermsa.AlignMSA(msaResult.consensus, Sequence, "Sequence: " +  + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].identificationMethod);

                    //    var alignments = filteredSequences.Select((seq, index) =>
                    //    {
                    //        var alignment = alignermsa.AlignMSA(msaResult.consensus, seq, "Sequence: " + sourceOrigins[index].sequence + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].identificationMethod);
                    //        alignment.TargetOrigin = fastaSequence.ID; //Add SourceOrigin to alignment
                    //        return alignment;
                    //    }).ToList();
                    //    myAlignment.AddRange(alignments);
                    //}

                    //for (int i = 0; i < filteredSequences.Count; i++)
                    //{
                    //    var alignment = alignermsa.AlignMSA(msaResult.consensus, filteredSequences[i], "Sequence: " + filteredSequences[i]);
                    //    alignment.TargetOrigin = msaResult;
                    //    myAlignment.Add(alignment);
                    //}

                    foreach (var Sequence in alignedSequences)
                    {
                        var alignment = filteredSequences.Select((seq, index) =>
                        {
                            var alignment = alignermsa.AlignerPCC(msaResult.consensus, seq, "Sequence: " + sourceOrigins[index].sequence + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].identificationMethod);
                            alignment.TargetOrigin = Sequence.Item1; //Add SourceOrigin to alignment
                            return alignment;
                        }).ToList();
                        myAlignment.AddRange(alignment);
                    }


                    var alignment2 = alignermsa.AlignerPCC(msaResult.consensus, "LLAFS", "xx");


                    //Updates the alignment view with the necessary parameters
                    List<Alignment> alignments = myAlignment
                        .Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore &&
                                    a.NormalizedSimilarity >= minNormalizedSimilarity &&
                                    a.GapsUsed <= maxGaps &&
                                    a.AlignedSmallSequence.Length >= minLengthFilter)
                        .ToList();


                    Console.WriteLine(alignments);
                   // var filteredDuplicatesToAlign = Utils.EliminateDuplicatesAndSubsequences(alignments);
                    
                    //Console.WriteLine(filteredDuplicatesToAlign);

                   MyMultipleAlignment.UpdateViewMultipleModel(alignedSequences, alignments);

                    TabItemResultBrowser2.IsSelected = true;
                    NormalizedSimilarityUpDown.IsEnabled = true;
                    IdentityUpDown.IsEnabled = true;
                    IntegerUpDownMinimumLength.IsEnabled = true;
                    TabItemResultBrowser2.IsEnabled = true;

                    UpdateMultiAlignmentTable();
                    MyMultipleAlignment.ExecuteAssembly();
                    ButtonUpdateAssembly.IsEnabled = true;

                }
                else
                {
                    MessageBox.Show("There are no sequences to align. Please filter the sequences before attempting to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void UpdateMultiAlignmentTable()
        {
            ButtonUpdateAssembly.IsEnabled = true;

            int maxGaps = IntegerUpDownMaximumGaps.Value ?? 0;
            int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
            int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
            int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;

            // Filtra a lista de alinhamentos com base nos critérios definidos
            var filteredAlignments = myAlignment
                .Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore &&
                            a.NormalizedSimilarity >= minNormalizedSimilarity &&
                            a.GapsUsed <= maxGaps &&
                            a.AlignedSmallSequence.Length >= minLengthFilter)
                .ToList();

            // Remove duplicatas e subsequências dos alinhamentos filtrados
            //var filteredDuplicatesToAlign = Utils.EliminateDuplicatesAndSubsequences(filteredAlignments);

            // Atualiza a fonte de itens do DataGridAlignments com as sequências FASTA carregadas e os alinhamentos filtrados
            //MyMultipleAlignment.UpdateViewMultipleModel(alignedSequences, filteredAlignments);
        }

        private void ProcessSingleAlignment()
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog
            {
                Multiselect = true,
                Filter = "Fasta Files (*.fasta)|*.fasta"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var loadedFastaFiles = openFileDialog.FileNames.Select(FastaFormat.LoadFasta).ToList();

                // Checks if at least one FASTA file was loaded correctly
                if (loadedFastaFiles == null || loadedFastaFiles.Any(fasta => fasta == null || !fasta.Any()))
                {
                    MessageBox.Show("Failed to load one or more FASTA files. Some files are empty or not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Stores loaded FASTA files
                allFastaSequences = loadedFastaFiles.SelectMany(fasta => fasta).ToList();

                Console.WriteLine($"Loaded {allFastaSequences.Count} fasta sequences.");

                if (filteredSequences != null && filteredSequences.Any())
                {
                    // Gets the origins of the filtered sequences
                    List<(string folder, string sequence, string origin)> sourceOrigins = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);

                    int maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    int minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    int minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    int minLengthFilter = (int)IntegerUpDownMinimumLength.Value;

                    SequenceAligner aligner = new SequenceAligner();

                    // Align PSM and De Novo sequences with sequences from all FASTA files
                    myAlignment = new List<Alignment>();
                    foreach (var fastaSequence in allFastaSequences)
                    {
                        Console.WriteLine($"Processing fasta sequence: {fastaSequence.ID}");
                        var alignments = filteredSequences.Select((seq, index) =>
                        {
                            var alignment = aligner.AlignSequences(fastaSequence.Sequence, seq, "Sequence: " + sourceOrigins[index].sequence + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].origin);
                            alignment.TargetOrigin = fastaSequence.ID; // Adds the target origin to the alignment
                            return alignment;
                        }).ToList();
                        myAlignment.AddRange(alignments);
                    }


                    Console.WriteLine($"Generated {myAlignment.Count} alignments.");

                    // Filters alignment results based on defined criteria
                    List<Alignment> filteredAlnResults = myAlignment
                        .Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore &&
                                    a.NormalizedSimilarity >= minNormalizedSimilarity &&
                                    a.GapsUsed <= maxGaps &&
                                    a.AlignedSmallSequence.Length >= minLengthFilter)
                        .ToList();

                    // Gets the origins of the targets of the filtered alignments
                    var filteredTargetOrigin = filteredAlnResults.Select(a => a.TargetOrigin).Distinct().ToList();

                    // Update the ViewModel in the Assembly control
                    MyAssembly.UpdateViewLocalModel(allFastaSequences, filteredAlnResults);

                    TabItemResultBrowser.IsSelected = true;
                    NormalizedSimilarityUpDown.IsEnabled = true;
                    IdentityUpDown.IsEnabled = true;
                    IntegerUpDownMinimumLength.IsEnabled = true;
                    TabItemResultBrowser.IsEnabled = true;
                    UpdateTable();

                    MyAssembly.ExecuteAssembly();
                    ButtonUpdateAssembly.IsEnabled = true;
                }
                else
                {
                    MessageBox.Show("There are no sequences to align. Please filter the sequences before attempting to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        //private static List<int> GetAlignedPositions(string alignedSequence)
        //{
        //    List<int> positions = new List<int>();
        //    for (int i = 0; i < alignedSequence.Length; i++)
        //    {
        //        if (alignedSequence[i] != '-')
        //        {
        //            positions.Add(i + 1);
        //        }
        //    }
        //    return positions;
        //}

        private void UpdateTable()
        {
            ButtonUpdateAssembly.IsEnabled = true;
            int maxGaps = IntegerUpDownMaximumGaps.Value ?? 0;
            int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
            int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
            int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;

            // Filters the list of alignments based on defined criteria
            var filteredAlignments = myAlignment
                .Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore &&
                            a.NormalizedSimilarity >= minNormalizedSimilarity &&
                            a.GapsUsed <= maxGaps &&
                            a.AlignedSmallSequence.Length >= minLengthFilter)
                .ToList();

            // Updates the DataGridAlignments item source with the loaded FASTA sequences and filtered alignments
            MyAssembly.UpdateViewLocalModel(allFastaSequences, filteredAlignments);
        }

        private void ButtonUpdate_Assembly(object sender, RoutedEventArgs e)
        {
            UpdateGeneral();

            if (isMultipleAlignmentMode)
            {
                MyMultipleAlignment.ExecuteAssembly();
                UpdateMultiAlignmentTable();
            }
            else
            {
                MyAssembly.ExecuteAssembly();
                UpdateTable();
            }
        }


        private void DataGridDeNovo_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void DataGridPSM_LoadingRow(object sender, System.Windows.Controls.DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implement as needed
        }

        private void OpenCompareSequencesWindow_Click(object sender, RoutedEventArgs e)
        {
            CompareSequence compareWindow = new CompareSequence(this);
            compareWindow.Show();
        }


    }
}

