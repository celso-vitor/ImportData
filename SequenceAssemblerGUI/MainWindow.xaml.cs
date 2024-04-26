using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic.ContigCode;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.ResultParser;
using SequenceAssemblerLogic.Tools;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
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
        List<Alignment> alignments = new List<Alignment>();
        List<IDResult> sequencesNovorDeNovo;
        List<IDResult> sequencesNovorPSM;




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


            UpdateSequencesFromDictionaries();
            //// Enabled the DataGridContig 
            //DataGridContig.IsEnabled = true;

            //// Make a loading label visible to inform the user that data is being loaded.
            //loadingLabel.Visibility = Visibility.Visible;

            //UpdateContig();
            //UpdateSequences();
        }

        public void UpdateSequencesFromDictionaries()
        {
            // From a dictionary of data psmDictTemp, select all the clean sequences (CleanPeptide) of PSMs, remove duplicates, and store them in a list named sequencesPSM.
            List<IDResult> sequencesNovorPSM =
            (from s in psmDictTemp.Values
             from psmID in s
             select new IDResult
             {
                 Peptide = psmID.CleanPeptide,
                 Source = "PSM"
             }).Distinct().ToList();

            // From a dictionary of data deNovoDictTemp, select all the clean sequences (CleanPeptide) of deNovo, remove duplicates, and store them in a list named sequencesDeNovo.
            List<IDResult> sequencesNovorDeNovo =
            (from s in deNovoDictTemp.Values
             from denovoID in s
             select new IDResult
             {
                 Peptide = denovoID.CleanPeptide,
                 Source = "DeNovo"
             }).Distinct().ToList();

            List<string> filteredSequences = sequencesNovorPSM.Concat(sequencesNovorDeNovo)
                          .Select(seq => seq.CleanPeptide).Distinct().ToList();

        }

        async void UpdateSequences()
        {
            int minOverlap = (int)IntegerUpDownAAOverlap.Value;
            try
            {
                List<IDResult> combinedResults = new List<IDResult>();

                // Adicionar sequências filtradas à lista combinada com sua origem identificada
                combinedResults.AddRange(filteredSequences.Select(seq => new IDResult { Peptide = seq, Source = "Filtered" }));

                // Aqui você pode adicionar suas sequências de PSM e de Novo se necessário
                // Por exemplo:
                combinedResults.AddRange(sequencesNovorPSM.Select(seq => new IDResult { Peptide = seq.CleanPeptide, Source = "PSM" }));
                combinedResults.AddRange(sequencesNovorDeNovo.Select(seq => new IDResult { Peptide = seq.CleanPeptide, Source = "DeNovo" }));

                // Se necessário, você pode adicionar mais processamento antes de montar os contigs
                // Por exemplo, alinhar sequências com uma sequência fasta.

                // Montar os contigs
                //myContigs = await Task.Run(() =>
                //{
                //    ContigAssembler ca = new ContigAssembler();
                //    return ca.AssembleContigSequences(combinedResults, minOverlap);
                //});

                //// Atualizar a exibição dos contigs na interface do usuário
                //DataGridContig.ItemsSource = myContigs.Select(contig => new
                //{
                //    Sequence = contig.Sequence,
                //    IDTotal = contig.IDs.Count,
                //    IDsFiltered = contig.IDs.Count(id => id.Source == "Filtered"),
                //    // Se quiser contar IDs de PSM e de Novo, descomente as linhas abaixo:
                //    IDsPSM = contig.IDs.Count(id => id.Source == "PSM"),
                //    IDsDeNovo = contig.IDs.Count(id => id.Source == "DeNovo")
                //}).ToList();

                //ButtonProcess.IsEnabled = true; // Assegurar que o botão é habilitado após a tarefa ser concluída
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to process sequences: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //finally
            //{
            //    loadingLabel.Visibility = Visibility.Hidden;
            //}
        }


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

                // Verifica se existem sequências de PSM e de Novo para processar antes de prosseguir
                if (filteredSequences != null && filteredSequences.Any())
                {
                    int maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    int minNormalizedIdentityScore = (int)IdentityUpDown.Value;
                    int minNormalizedSimilarity = (int)NormalizedSimilarityUpDown.Value;
                    SequenceAligner aligner = new SequenceAligner();

                    // Alinha as sequências de PSM e de Novo com as sequências do arquivo FASTA
                    myAlignment = filteredSequences.Select(seq => aligner.AlignSequences(myFasta[0].Sequence, seq)).ToList();

                    // Atualiza a visualização do alinhamento com os parâmetros necessários
                    MyAssembly.DataGridAlignments.ItemsSource = myAlignment;
                    MyAssembly.AlignmentList = myAlignment;
                    MyAssembly.UpdateAlignmentGrid(minNormalizedIdentityScore, minNormalizedSimilarity, myFasta);

                    ButtonUpdateResult.IsEnabled = true;
                    TabItemResultBrowser.IsSelected = true;
                    NormalizedSimilarityUpDown.IsEnabled = true;
                    IdentityUpDown.IsEnabled = true;
                    TabItemResultBrowser.IsEnabled = true;
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

        private void ButtonUpdateResult_Click(object sender, RoutedEventArgs e)
        {
            int minNormalizedIdentityScore = IdentityUpDown.Value ?? 0;
            int minNormalizedSimilarity = NormalizedSimilarityUpDown.Value ?? 0;

            //// Atualiza a grade de alinhamentos com os novos parâmetros de filtro
            MyAssembly.UpdateAlignmentGrid(minNormalizedIdentityScore, minNormalizedSimilarity, myFasta);

            // Filtra a lista de alinhamentos com base nos critérios de identidade e similaridade
            var filteredAlignments = myAlignment.Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();
            // Prepara a lista de dados para o DataGrid dos contigs
            //var seqDataList = filteredAlignments.Select(a => new SequencesData(a) { AlignedSmallSequence = a.AlignedSmallSequence }).ToList();
            MyAssembly.DataGridAlignments.ItemsSource = filteredAlignments;

            MyAssembly.DataGridFasta.ItemsSource = myFasta;
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

            //if (myContigs != null)
            //{
            //    UpdateContig();

            //}
            //else
            //{

            //    Console.WriteLine("Contigs is null");

            //}

        }
        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implement as needed
        }
        private void MenuItemCompareSequences_Click(object sender, RoutedEventArgs e)
        {
            CompareSequences cs = new CompareSequences();
            cs.ShowDialog();

        }

      
    }
}


