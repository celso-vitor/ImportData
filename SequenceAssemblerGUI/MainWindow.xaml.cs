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
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static SequenceAssemblerGUI.Assembly;

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
                
                ButtonUpdate.IsEnabled = true;
                ButtonProcess.IsEnabled = true;


            }



            //// From a dictionary of data psmDictTemp, select all the clean sequences (CleanPeptide) of PSMs, store them in a dictionary with their source.
            //Dictionary<string, string> sequencesNovorPSM =
            //    (from s in psmDictTemp.Values
            //     from psmID in s
            //     select new { psmID.CleanPeptide, Source = "PSM" })
            //    .Distinct()
            //    .ToDictionary(p => p.CleanPeptide, p => p.Source);

            //// From a dictionary of data deNovoDictTemp, select all the clean sequences (CleanPeptide) of deNovo, store them in a dictionary with their source.
            //Dictionary<string, string> sequencesNovorDeNovo =
            //    (from s in deNovoDictTemp.Values
            //     from denovoID in s
            //     select new { denovoID.CleanPeptide, Source = "DeNovo" })
            //    .Distinct()
            //    .ToDictionary(p => p.CleanPeptide, p => p.Source);

            //// Optionally, merge the dictionaries while preserving sources.
            //var allSequencesWithSources = new Dictionary<string, string>(sequencesNovorPSM);
            //foreach (var item in sequencesNovorDeNovo)
            //{
            //    if (!allSequencesWithSources.ContainsKey(item.Key))
            //    {
            //        allSequencesWithSources[item.Key] = item.Value;
            //    }
            //}

            //// Convert to list if needed for further processing
            //List<string> filteredSequences = allSequencesWithSources.Keys.ToList();

            //// Assign the dictionary to MyAssembly.cleanValues
            //MyAssembly.denovoValue.Add("DeNovo", sequencesNovorDeNovo.Keys.ToList());
            //MyAssembly.psmValue.Add("PSM", sequencesNovorPSM.Keys.ToList());


            //// Disable the DataGridContig to prevent user interaction while data is being loaded.
            //DataGridContig.IsEnabled = true;

            //// Make a loading label visible to inform the user that data is being loaded.
            //loadingLabel.Visibility = Visibility.Visible;

            //UptadeContig();
        }

        //async void UptadeContig()
        //{

        //    // How many amino acids should overlap for contigs (partially overlapping sequences).
        //    int overlapAAForContigs = (int)IntegerUpDownAAOverlap.Value;

        //    // Execute the contig assembly process with the filtered sequences and the previously defined overlap value on a background task.
        //    myContigs = await Task.Run
        //    (
        //        () =>
        //        {
        //            ContigAssembler ca = new ContigAssembler();
        //            List<IDResult> results = new List<IDResult>();

        //            var resultsPSM = (from kvp in psmDictTemp
        //                              from r in kvp.Value
        //                              select r).ToList();

        //            var resultsDenovo = (from kvp in deNovoDictTemp
        //                                 from r in kvp.Value
        //                                 select r).ToList();


        //            return ca.AssembleContigSequences(resultsPSM.Concat(resultsDenovo).ToList(), overlapAAForContigs);
        //        });

        //ButtonProcess.IsEnabled = true;

        //// Set the item source of DataGridContig to be an anonymous list containing the assembled contigs.
        //DataGridContig.ItemsSource = myContigs.Select(a => new { Sequence = a.Sequence, IDTotal = a.IDs.Count(), IDsDenovo = a.IDs.Count(a => !a.IsPSM), IDsPSM = a.IDs.Count(a => a.IsPSM) });

        //// Hide the loading label as the data has now been loaded.
        //loadingLabel.Visibility = Visibility.Hidden;
        //}


        //---------------------------------------------------------

        // Update in Dicionarys DeNovo and Psm
        private void UpdateGeneral()
        {
            PlotViewEnzymeEfficiency.Visibility = Visibility.Visible;

            // Reset the temporary Dictionary
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

            // Apply filters to the filtered dictionaries
            int denovoMaxSequeceLength = (int)IntegerUpDownDeNovoMaxLength.Value;
            Parser.FilterDictMaxLengthDeNovo(denovoMaxSequeceLength, deNovoDictTemp);

            int psmMinSequenceLength = (int)IntegerUpDownPSMMinLength.Value;
            Parser.FilterDictMinLengthPSM(psmMinSequenceLength, psmDictTemp);

            int psmMaxSequenceLength = (int)IntegerUpDownPSMMaxLength.Value;
            Parser.FilterDictMaxLengthPSM(psmMaxSequenceLength, psmDictTemp);

            double filterPsmSocore = (int)IntegerUpDownPSMScore.Value;
            Parser.FilterSequencesByScorePSM(filterPsmSocore, psmDictTemp);
            // Atualiza a lista de sequências filtradas
            filteredSequences = deNovoDictTemp.Values.SelectMany(v => v)
                                    .Union(psmDictTemp.Values.SelectMany(v => v))
                                    .Select(seq => seq.CleanPeptide)
                                    .Distinct()
                                    .ToList();

            // Atualiza a variável myAlignment para refletir os alinhamentos atualizados
            myAlignment = filteredSequences.Select(seq => new Alignment()).ToList();
       

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
                // Tenta carregar o arquivo FASTA selecionado
                var loadedFasta = FastaFormat.LoadFasta(openFileDialog.FileName);

                // Verifica se o arquivo FASTA foi carregado corretamente
                if (loadedFasta == null || !loadedFasta.Any())
                {
                    MessageBox.Show("Failed to load FASTA file. The file is empty or not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Sai do método para evitar mais processamento
                }

                // Armazena o arquivo FASTA carregado
                myFasta = loadedFasta;


                // Exemplo: Atualizando o título da janela com o ID e a descrição da primeira sequência
                var firstSequence = myFasta.First();
                this.Title = $"Protein Sequence Assembler - {firstSequence.ID} - {firstSequence.Description}";



                // Declare myAlignment antes deste bloco de código se ainda não tiver sido declarado

                if (filteredSequences != null && filteredSequences.Any())
                {
                    // Obtém as origens das sequências filtradas
                    List<string> sourceOrigins = Utils.GetSourceOrigins(filteredSequences, deNovoDictTemp, psmDictTemp);
                

                    int maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    int minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    int minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    int minLengthFilter = (int)IntegerUpDownMinimumLength.Value;

                    

                    SequenceAligner aligner = new SequenceAligner();


                    // Alinha as sequências de PSM e de Novo com as sequências do arquivo FASTA
                    myAlignment = filteredSequences.Select((seq, index) => aligner.AlignSequences(myFasta[0].Sequence, seq, sourceOrigins[index])).ToList();

                    // Atualiza a visualização do alinhamento com os parâmetros necessários
                    MyAssembly.DataGridAlignments.ItemsSource = myAlignment;
                    MyAssembly.AlignmentList = myAlignment;
                    MyAssembly.UpdateAlignmentGrid(minNormalizedIdentityScore, minNormalizedSimilarity, minLengthFilter, myFasta);

                    TabItemResultBrowser.IsSelected = true;
                    NormalizedSimilarityUpDown.IsEnabled = true;
                    IdentityUpDown.IsEnabled = true;
                    IntegerUpDownMinimumLength.IsEnabled = true;
                    TabItemResultBrowser.IsEnabled = true;

                    UpdateTable();

                    string allSequences = string.Join("\n", loadedFasta.Select(fasta => fasta.Sequence));
                    var (consensusChars, totalCoverage) = MyAssembly.BuildConsensus(myAlignment, allSequences);
                    LabelCoverage.Content = totalCoverage;

                }
                else
                {
                    MessageBox.Show("There are no sequences to align. Please filter the sequences before attempting to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateGeneral();
        }

        private void UpdateTable()
        {
            ButtonUpdateAssembly.IsEnabled = true;
            int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
            int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;
            int minLengthFilter = IntegerUpDownMinimumLength.Value ?? 0;


            // Filtra a lista de alinhamentos completa com base nos critérios de identidade e similaridade
            var filteredAlignments = myAlignment.Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();

            // Atualiza a fonte de itens do DataGridAlignments com os alinhamentos filtrados
            MyAssembly.DataGridAlignments.ItemsSource = filteredAlignments;
        }

       
        private void ButtonUpdate_Assembly(object sender, RoutedEventArgs e)
        {
            UpdateTable();
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
        private void MenuItemCompareSequences_Click(object sender, RoutedEventArgs e)
        {
            CompareSequences cs = new CompareSequences();
            cs.Show();

        }

        
    }
}


