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

namespace SequenceAssemblerGUI
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, (int PSMCount, int DeNovoCount)> folderCounts = new Dictionary<string, (int, int)>();
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
                    // Re-perform the appropriate alignment based on the selected alignment mode
                    if (lastOpenedFileNames != null && lastOpenedFileNames.Length > 0)
                    {
                        if (isMultipleAlignmentMode)
                        {
                            MultipleAlignment(lastOpenedFileNames); // Use the variable that stores the names of loaded FASTA files
                        }
                        else
                        {
                            LocalAlignment(lastOpenedFileNames); // Use the variable that stores the names of loaded FASTA files
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

                            // Hide the splash image after importing results
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

            // Reset the temporary dictionaries
            deNovoDictTemp = new Dictionary<string, List<IDResult>>();
            psmDictTemp = new Dictionary<string, List<IDResult>>();

            int totalPsmRegistries = newParser.psmDictTemp.Values.Sum(list => list.Count);
            int totalDenovoRegistries = newParser.deNovoDictTemp.Values.Sum(list => list.Count);

            // Pegar os valores dos filtros de comprimento e confiança local
            int denovoMinSequenceLength = (int)IntegerUpDownDeNovoMinLength.Value; // Comprimento mínimo
            int denovoMaxSequenceLength = (int)IntegerUpDownDeNovoMaxLength.Value; // Comprimento máximo
            int denovoMinConfidence = (int)IntegerUpDownDeNovoScore.Value;   // Confiança local mínima

            // Processa o dicionário de DeNovo
            foreach (var kvp in newParser.DictDenovo)
            {
                List<IDResult> listNovor = (from rg in kvp.Value
                                            from rg2 in DeNovoTagExtractor.DeNovoRegistryToTags(rg, denovoMinSequenceLength, denovoMaxSequenceLength, denovoMinConfidence)
                                            select rg2).ToList();

                deNovoDictTemp.Add(kvp.Key, listNovor);
            }

            // Processa o dicionário de PSM
            foreach (var kvp in newParser.DictPsm)
            {
                psmDictTemp.Add(kvp.Key, kvp.Value.ToList());
            }

            // Aplica filtros nos dicionários filtrados
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequenceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            Parser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            Parser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            double filterPsmScore = (double)IntegerUpDownPSMScore.Value;
            Parser.FilterSequencesByScorePSM(filterPsmScore, psmDictTemp);

            // Contagens para os PSM e DeNovo filtrados
            int filteredPsmCount = psmDictTemp.Values.Sum(list => list.Count);
            int filteredDeNovoCount = deNovoDictTemp.Values.Sum(list => list.Count);

            // Atualiza a lista de sequências filtradas
            filteredSequences = deNovoDictTemp.Values.SelectMany(v => v)
                                .Union(psmDictTemp.Values.SelectMany(v => v))
                                .Select(seq => seq.CleanPeptide)
                                .Distinct()
                                .ToList();

            // Recalcula os alinhamentos com base nas sequências filtradas
            myAlignment = filteredSequences.Select(seq => new Alignment()).ToList();

            // Atualiza a interface gráfica (GUI)
            Dispatcher.Invoke(() =>
            {
                UpdatePlot();
                UpdateDataView();

                // Atualiza as contagens exibidas na interface
                LabelPSMCount.Content = filteredPsmCount.ToString();
                LabelDeNovoCount.Content = filteredDeNovoCount.ToString();

                // Atualiza as informações detalhadas sobre alinhamento
                FastaInfoStackPanel.Children.Clear();

                var folderInfo = string.Join("\n", psmDictTemp.Keys.Union(deNovoDictTemp.Keys).Select(folder =>
                {
                    int psmCount = psmDictTemp.ContainsKey(folder) ? psmDictTemp[folder].Count : 0;
                    int deNovoCount = deNovoDictTemp.ContainsKey(folder) ? deNovoDictTemp[folder].Count : 0;
                    return $"{folder}: PSM = {psmCount}, DeNovo = {deNovoCount}";
                }));

                var alignmentInfo = new TextBlock
                {
                    Text = $"Filtered PSM Count: {filteredPsmCount}\n" +
                           $"Filtered DeNovo Count: {filteredDeNovoCount}\n" +
                           $"Folder Usage:\n{folderInfo}",
                    Margin = new Thickness(0, 10, 0, 10)
                };

                FastaInfoStackPanel.Children.Add(alignmentInfo);
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

        private string[] lastOpenedFileNames; // Variable to store the names of open files


        private async void ButtonProcess_Click(object sender, RoutedEventArgs e)
        {
            VistaOpenFileDialog openFileDialog = new VistaOpenFileDialog
            {
                Multiselect = true,
                Filter = "Fasta Files (*.fasta)|*.fasta"
            };

            // Clear or delete the log file before starting the new alignment
            string logFilePath = Path.Combine("..", "..", "..", "Debug", "local_consensus_log.txt");
            if (File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, string.Empty);
            }

            string logFilemsaPath = Path.Combine("..", "..", "..", "Debug", "msa_consensus_log.txt");
            if (File.Exists(logFilemsaPath))
            {
                File.WriteAllText(logFilemsaPath, string.Empty);
            }

            if (openFileDialog.ShowDialog() == true)
            {
                // Limpa apenas os valores visuais do painel `FastaInfoGroupBox`
                ClearFastaInfo();

                ShowLoadingIndicator();
                lastOpenedFileNames = openFileDialog.FileNames; // Stores the names of selected files
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
            // Carregando os arquivos FASTA
            var loadedFastaFiles = fileNames.Select(FastaFormat.LoadFasta).ToList();

            // Verificando se ao menos um arquivo FASTA foi carregado corretamente
            if (loadedFastaFiles == null || loadedFastaFiles.Any(fasta => fasta == null || !fasta.Any()))
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Falha ao carregar um ou mais arquivos FASTA. Alguns arquivos estão vazios ou não são válidos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            // Armazenando as sequências FASTA carregadas
            var allFastaSequences = loadedFastaFiles.SelectMany(fasta => fasta).ToList();

            // Verificando se há pelo menos duas sequências carregadas
            if (allFastaSequences.Count < 2)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Você deve selecionar ao menos duas sequências FASTA para o alinhamento múltiplo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
                return;
            }

            Console.WriteLine($"Carregadas {allFastaSequences.Count} sequências FASTA.");

            // Realizando o alinhamento múltiplo de sequências usando Clustal
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
            Console.WriteLine("Alinhamento múltiplo de sequências completo.");

            // Concatenando as sequências para o alinhamento
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

            // Verificando se há sequências filtradas
            if (filteredSequences != null && filteredSequences.Any())
            {
                // Obtendo as origens das sequências e a contagem de uso dos folders
                var (sourceOrigins, folderUsageCount) = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);

                // Parâmetros de filtro
                int minNormalizedIdentityScore = 0, minNormalizedSimilarity = 0, minLengthFilter = 0;

                Dispatcher.Invoke(() =>
                {
                    minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    minLengthFilter = (int)IntegerUpDownMinimumLength.Value;
                });

                // Filtrando sequências por comprimento normalizado
                filteredSequences = Utils.FilterSequencesByNormalizedLength(filteredSequences,
                    string.Join("", allFastaSequences.Select(f => f.Sequence)), minLengthFilter);

                SequenceAligner alignerMsa = new SequenceAligner();
                myAlignment = new List<Alignment>();

                // Realizando o alinhamento
                foreach (var sequence in alignedSequences)
                {
                    myAlignment.AddRange(filteredSequences.Select((seq, index) =>
                    {
                        var alignmentResult = alignerMsa.AlignerMSA(msaResult.consensus, seq,
                            $"Sequence: {sourceOrigins[index].sequence} " +
                            $"Origin: {sourceOrigins[index].folder} " +
                            $"Identification Method: {sourceOrigins[index].identificationMethod}");

                        alignmentResult.TargetOrigin = sequence.ID;
                        return alignmentResult;
                    }));
                }

       
                // Atualizando a interface
                Dispatcher.Invoke(() =>
                {
                    UpdateViewMultipleModel(alignedSequences, FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter));
                    UpdateMultiAlignmentTable();
                });
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Não há sequências para alinhar. Filtre as sequências antes de tentar processar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }


        private void UpdateMultiAlignmentTable()
        {
            Dispatcher.Invoke(() =>
            {
                // Limpando a interface antes de adicionar novas informações
                FastaInfoStackPanel.Children.Clear();

                // Obtendo os valores dos filtros aplicados
                int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
                int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
                int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;

                // Filtrando os alinhamentos com base nos filtros
                var filteredAlignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);

                // Contagem de PSMs e DeNovos usados após filtro
                int psmUsedCount = filteredAlignments.Count(a => a.SourceOrigin.Contains("PSM"));
                int deNovoUsedCount = filteredAlignments.Count(a => a.SourceOrigin.Contains("DeNovo"));

                // Inicializa um dicionário para calcular o total de PSMs e DeNovos por folder
                var folderCountsTotal = filteredAlignments
                    .GroupBy(a => a.TargetOrigin)
                    .ToDictionary(
                        group => group.Key,
                        group => (
                            PSMCount: group.Count(a => a.SourceOrigin.Contains("PSM")),
                            DeNovoCount: group.Count(a => a.SourceOrigin.Contains("DeNovo"))
                        )
                    );

                // Variáveis para o total geral de PSMs e DeNovos
                int totalPsmCount = folderCountsTotal.Sum(folder => folder.Value.PSMCount);
                int totalDeNovoCount = folderCountsTotal.Sum(folder => folder.Value.DeNovoCount);

                // Criando o bloco de texto para exibir as informações
                var alignmentInfo = new TextBlock();

                // Adiciona as informações principais
                alignmentInfo.Text = "\n" +
                                     $"Filtered PSM Count: {psmUsedCount}\n" +
                                     $"Filtered DeNovo Count: {deNovoUsedCount}\n\n";

                // Adiciona as sequências individualmente
                alignmentInfo.Text += "ID Sequences:\n";
                foreach (var sequence in filteredAlignments.Select(a => a.TargetOrigin).Distinct())
                {
                    alignmentInfo.Text += $"{sequence}\n";
                }

                // Adiciona o total de folders utilizados
                alignmentInfo.Text += "\nFolders used in total:\n" +
                                      $"PSM: {totalPsmCount}, DeNovo: {totalDeNovoCount}\n";

                // Exibe as informações na interface gráfica
                FastaInfoStackPanel.Children.Add(alignmentInfo);
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
                MyMultipleAlignment.ExecuteMultipleAssembly();
            });
        }

        //---------------------------------------------------------

        // Local Alignment
        private void LocalAlignment(string[] fileNames)
        {
            var loadedFastaFiles = fileNames.Select(FastaFormat.LoadFasta).ToList();

            if (loadedFastaFiles == null || loadedFastaFiles.Any(fasta => fasta == null || !fasta.Any()))
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Falha ao carregar um ou mais arquivos FASTA. Alguns arquivos estão vazios ou não são válidos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return;
            }

            allFastaSequences = loadedFastaFiles.SelectMany(fasta => fasta).ToList();
            Console.WriteLine($"Loaded {allFastaSequences.Count} fasta sequences.");

            if (filteredSequences != null && filteredSequences.Any())
            {
                int minNormalizedIdentityScore = 0;
                int minNormalizedSimilarity = 0;
                int minLengthFilter = 0;

                Dispatcher.Invoke(() =>
                {
                    minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    minLengthFilter = (int)IntegerUpDownMinimumLength.Value;
                });

                SequenceAligner aligner = new SequenceAligner();
                filteredSequences = Utils.FilterSequencesByNormalizedLength(filteredSequences, string.Join("", allFastaSequences.Select(f => f.Sequence)), minLengthFilter);

                int psmUsedCount = 0;
                int deNovoUsedCount = 0;
                //int folder = 0;
                var (sourceOrigins, folderUsageCount) = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);
                myAlignment = new List<Alignment>();

                foreach (var fastaSequence in allFastaSequences)
                {
                    Console.WriteLine($"Processing fasta sequence: {fastaSequence.ID}");

                    // Filtrando os alinhamentos para cada sequência FASTA
                    var alignments = filteredSequences.Select((seq, index) =>
                    {
                        var alignment = aligner.AlignerLocal(fastaSequence.Sequence, seq, "Sequence: " + sourceOrigins[index].sequence + " Origin: " + sourceOrigins[index].folder + " Identification Method: " + sourceOrigins[index].identificationMethod);
                        alignment.TargetOrigin = fastaSequence.ID; // Adiciona a origem alvo ao alinhamento

                        // Lógica de contagem para PSM e DeNovo
                        if (sourceOrigins[index].identificationMethod == "PSM")
                        {
                            psmUsedCount++;
                            alignment.SourceOrigin = sourceOrigins[index].folder; // Preserva a origem PSM
                            alignment.SourceType = "PSM"; // Define o tipo de origem como "PSM"

                            // Preserva a sequência de origem
                            alignment.SourceSeq = sourceOrigins[index].sequence;  // Preservando a sequência do PSM

                            // Atualiza a contagem de PSM para o folder específico
                            if (folderCounts.ContainsKey(sourceOrigins[index].folder))
                            {
                                var currentCounts = folderCounts[sourceOrigins[index].folder];
                                folderCounts[sourceOrigins[index].folder] = (currentCounts.PSMCount + 1, currentCounts.DeNovoCount);
                            }
                            else
                            {
                                folderCounts[sourceOrigins[index].folder] = (1, 0); // Inicializa o contador para PSM
                            }
                        }
                        else if (sourceOrigins[index].identificationMethod == "DeNovo")
                        {
                            deNovoUsedCount++;
                            alignment.SourceOrigin = sourceOrigins[index].folder; // Preserva a origem DeNovo
                            alignment.SourceType = "DeNovo"; // Define o tipo de origem como "DeNovo"

                            // Preserva a sequência de origem
                            alignment.SourceSeq = sourceOrigins[index].sequence;  // Preservando a sequência do DeNovo

                            // Atualiza a contagem de DeNovo para o folder específico
                            if (folderCounts.ContainsKey(sourceOrigins[index].folder))
                            {
                                var currentCounts = folderCounts[sourceOrigins[index].folder];
                                folderCounts[sourceOrigins[index].folder] = (currentCounts.PSMCount, currentCounts.DeNovoCount + 1);
                            }
                            else
                            {
                                folderCounts[sourceOrigins[index].folder] = (0, 1); // Inicializa o contador para DeNovo
                            }
                        }

                        return alignment;
                    }).ToList();

                    myAlignment.AddRange(alignments);
                }


                Console.WriteLine($"Generated {myAlignment.Count} alignments.");
                List<Alignment> filteredAlnResults = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);


                Dispatcher.Invoke(() =>
                {
                    MyAssembly.UpdateViewLocalModel(allFastaSequences, filteredAlnResults);
                    UpdateLocalTable();

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
                // Obtendo os valores dos filtros aplicados
                int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
                int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
                int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;

                // Filtrando os alinhamentos com base nos filtros
                var filteredAlignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);

                // Limpa o StackPanel antes de adicionar novos itens
                var fastaInfoStackPanel = this.FindName("FastaInfoStackPanel") as StackPanel;
                if (fastaInfoStackPanel != null)
                {
                    fastaInfoStackPanel.Children.Clear();
                }

                // Processamento por sequência FASTA
                foreach (var fastaSequence in allFastaSequences)
                {
                    // Inicializando contadores para cada sequência
                    int psmUsedCount = 0;
                    int deNovoUsedCount = 0;
                    var folderCounts = new Dictionary<string, (int PSMCount, int DeNovoCount)>();

                    // Filtrando os alinhamentos para a sequência FASTA atual
                    var sequenceAlignments = filteredAlignments.Where(a => a.TargetOrigin == fastaSequence.ID);

                    // Processando os alinhamentos filtrados para cada sequência FASTA
                    foreach (var alignment in sequenceAlignments)
                    {
                        var folder = alignment.SourceOrigin;

                        if (alignment.SourceType.Contains("PSM"))
                        {
                            psmUsedCount++;
                            if (!folderCounts.ContainsKey(folder))
                                folderCounts[folder] = (0, 0);

                            var currentCounts = folderCounts[folder];
                            folderCounts[folder] = (currentCounts.PSMCount + 1, currentCounts.DeNovoCount);
                        }
                        else if (alignment.SourceType.Contains("DeNovo"))
                        {
                            deNovoUsedCount++;
                            if (!folderCounts.ContainsKey(folder))
                                folderCounts[folder] = (0, 0);

                            var currentCounts = folderCounts[folder];
                            folderCounts[folder] = (currentCounts.PSMCount, currentCounts.DeNovoCount + 1);
                        }
                    }

                    // Atualiza os TextBlocks com os valores da sequência
                    if (fastaInfoStackPanel != null)
                    {
                        // Cria os TextBlocks para cada sequência
                        var fastaInfo = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Margin = new Thickness(0, 10, 0, 10)
                        };

                        var labelFastaID = new TextBlock
                        {
                            Text = $"Fasta ID: {fastaSequence.ID}"
                        };

                        var labelPSMUsedCount = new TextBlock
                        {
                            Text = $"PSMs Used in Alignment: {psmUsedCount}"
                        };

                        var labelDeNovoUsedCount = new TextBlock
                        {
                            Text = $"DeNovo Used in Alignment: {deNovoUsedCount}"
                        };

                        var folderInfo = string.Join("\n", folderCounts.Select(f => $"{f.Key}: PSM = {f.Value.PSMCount}, DeNovo = {f.Value.DeNovoCount}"));
                        var labelFoldersUsed = new TextBlock
                        {
                            Text = $"Folders Used:\n{folderInfo}"
                        };

                        // Adiciona os TextBlocks ao StackPanel
                        fastaInfo.Children.Add(labelFastaID);
                        fastaInfo.Children.Add(labelPSMUsedCount);
                        fastaInfo.Children.Add(labelDeNovoUsedCount);
                        fastaInfo.Children.Add(labelFoldersUsed);

                        // Adiciona ao StackPanel principal
                        fastaInfoStackPanel.Children.Add(fastaInfo);
                    }
                }

                // Atualiza o modelo local após os filtros
                UpdateLocalModel(allFastaSequences, filteredAlignments);

                // Log geral de PSM e DeNovo usados
                int psmUsedCountTotal = filteredAlignments.Count(a => a.SourceType.Contains("PSM"));
                int deNovoUsedCountTotal = filteredAlignments.Count(a => a.SourceType.Contains("DeNovo"));

                // Log dos valores para depuração
                Console.WriteLine($"PSM Used Count: {psmUsedCountTotal}");
                Console.WriteLine($"DeNovo Used Count: {deNovoUsedCountTotal}");

                // Log geral dos folders e suas contagens
                var folderCountsTotal = new Dictionary<string, (int PSMCount, int DeNovoCount)>();
                foreach (var alignment in filteredAlignments)
                {
                    var folder = alignment.SourceOrigin;

                    if (alignment.SourceType.Contains("PSM"))
                    {
                        if (!folderCountsTotal.ContainsKey(folder))
                            folderCountsTotal[folder] = (0, 0);

                        var currentCounts = folderCountsTotal[folder];
                        folderCountsTotal[folder] = (currentCounts.PSMCount + 1, currentCounts.DeNovoCount);
                    }
                    else if (alignment.SourceType.Contains("DeNovo"))
                    {
                        if (!folderCountsTotal.ContainsKey(folder))
                            folderCountsTotal[folder] = (0, 0);

                        var currentCounts = folderCountsTotal[folder];
                        folderCountsTotal[folder] = (currentCounts.PSMCount, currentCounts.DeNovoCount + 1);
                    }
                }

                // Atualizando a interface com os valores totais
                Console.WriteLine("Folders used in total:");
                foreach (var folder in folderCountsTotal)
                {
                    Console.WriteLine($"{folder.Key}: PSM = {folder.Value.PSMCount}, DeNovo = {folder.Value.DeNovoCount}");
                }
            });
        }

        private void UpdateLocalModel(List<Fasta> allFastaSequences, List<Alignment> alignments)
        {
            Dispatcher.Invoke(() =>
            {
                MyAssembly.UpdateViewLocalModel(allFastaSequences, alignments);
                TabItemResultBrowser.IsSelected = true;
                NormalizedSimilarityUpDown.IsEnabled = true;
                IdentityUpDown.IsEnabled = true;
                IntegerUpDownMinimumLength.IsEnabled = true;
                TabItemResultBrowser.IsEnabled = true;
                ButtonUpdateAssembly.IsEnabled = true;
                MyAssembly.ExecuteLocalAssembly();
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

        private void ClearFastaInfo()
        {
            Dispatcher.Invoke(() =>
            {
                // Limpa os TextBlocks específicos da interface
                LabelFastaID.Text = string.Empty;
                LabelPSMUsedCount.Text = string.Empty;
                LabelDeNovoUsedCount.Text = string.Empty;
                LabelFoldersUsed.Text = string.Empty;

                // Opcional: Limpa outras crianças do StackPanel se necessário
                FastaInfoStackPanel.Children.Clear();
            });
        }
        // Update Alignments
        // Update Alignments
        private void ButtonUpdate_Assembly(object sender, RoutedEventArgs e)
        {
            ShowLoadingIndicator();

            // Inicia uma tarefa assíncrona em um thread separado
            Task.Run(() =>
            {
                try
                {
                    // Verifica se há alinhamentos a filtrar
                    if (myAlignment == null || !myAlignment.Any())
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("There are no alignments to filter. Please align sequences first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                        return;
                    }

                    // Obter valores dos filtros da UI
                    int minNormalizedIdentityScore = 0;
                    int minNormalizedSimilarity = 0;
                    int minLengthFilter = 0;

                    // Usar o Dispatcher.Invoke para garantir que a UI tenha os valores mais recentes
                    Dispatcher.Invoke(() =>
                    {
                        minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                        minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                        minLengthFilter = (int)IntegerUpDownMinimumLength.Value;

                        // Debug: Verificar os valores dos filtros
                        Console.WriteLine($"minNormalizedIdentityScore: {minNormalizedIdentityScore}, minNormalizedSimilarity: {minNormalizedSimilarity}, minLengthFilter: {minLengthFilter}");
                    });

                    // Filtrar os alinhamentos com base nos valores dos filtros
                    var filteredAlignments = FilterAlignments(myAlignment, minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter);

                    // Contagem de PSMs e DeNovos usados
                    int psmUsedCount = filteredAlignments.Count(a => a.SourceType.Contains("PSM"));
                    int deNovoUsedCount = filteredAlignments.Count(a => a.SourceType.Contains("DeNovo"));

                    // Log para depuração
                    Console.WriteLine($"PSM Used Count: {psmUsedCount}");
                    Console.WriteLine($"DeNovo Used Count: {deNovoUsedCount}");


                    // Atualizando o modelo na interface
                    Dispatcher.Invoke(() =>
                    {
                        if (isMultipleAlignmentMode)
                        {
                            // Atualiza a interface para o modo de múltiplos alinhamentos
                            UpdateViewMultipleModel(alignedSequences, filteredAlignments);
                            UpdateMultiAlignmentTable();
                        }
                        else
                        {
                            // Atualiza a interface para o modo de alinhamento local
                            UpdateLocalModel(allFastaSequences, filteredAlignments);
                            UpdateLocalTable();
                        }
                    });
                }
                finally
                {
                    // Esconde o indicador de carregamento
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

