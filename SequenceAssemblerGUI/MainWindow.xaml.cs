using Ookii.Dialogs.Wpf;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SequenceAssemblerLogic.ContigCode;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.ResultParser;
using SequenceAssemblerLogic.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
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


                // Tenta carregar o arquivo FASTA selecionado
                var loadedFasta = FastaFormat.LoadFasta(openFileDialog.FileName);

                // Verifica se o arquivo FASTA foi carregado corretamente
                if (loadedFasta == null || !loadedFasta.Any())
                {
                    MessageBox.Show("Failed to load FASTA file. The file is empty or not valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return; // Sai do método para evitar mais processamento
                }

                myFasta = loadedFasta;
                DataGridFasta.ItemsSource = myFasta;

                // Verifica se existe alguma sequência contig para processar antes de proceder
                if (myContigs != null && myContigs.Any())
                {
                    // Se houver contigs, proceda com o alinhamento e outras operações
                    int maxGaps = (int)IntegerUpDownMaximumGaps.Value;
                    int minIdentity = (int)NormalizedSimilarityUpDown.Value;
                    int minNormalizedSimilarity = (int)IdentityUpDown.Value;
                    SequenceAligner aligner = new SequenceAligner(maxGaps: maxGaps, gapPenalty: -2, ignoreILDifference: true);

                    myAlignment = myContigs.Select(a => aligner.AlignSequences(myFasta[0].Sequence, a.Sequence)).ToList();

                    // Chama o método para atualizar a grade de alinhamento com os parâmetros necessários
                    MyAlignmentViewer.AlignmentList = myAlignment;
                    MyAlignmentViewer.UpdateAlignmentGrid(minIdentity, minNormalizedSimilarity, myFasta);

                    // Chama o método para atualizar a grade de alinhamento com os parâmetros necessários
                    MyAssembly.AlignmentList = myAlignment;
                    MyAssembly.UpdateAlignmentGrid(minIdentity, minNormalizedSimilarity, myFasta);

                    ButtonUpdateResult.IsEnabled = true;
                    TabItemResultBrowser.IsSelected = true;

                }
                else
                {
                    MessageBox.Show("There are no contigs to line up. Please upload the contigs before attempting to process.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
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

            MyAlignmentViewer.UpdateAlignmentGrid(minIdentity, minNormalizedSimilarity, myFasta);
            MyAssembly.UpdateAlignmentGrid(minIdentity, minNormalizedSimilarity, myFasta);

            // Filtra a lista de alinhamentos com base nos critérios de identidade e similaridade
            var filteredAlignments = myAlignment.Where(a => a.Identity >= minIdentity && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();

            // Concatena as informações dos alinhamentos filtrados
            StringBuilder sb = new StringBuilder();
            foreach (var alignment in filteredAlignments)
            {
                // Aqui você concatena as propriedades de interesse do alinhamento
                // Exemplo: sb.AppendLine($"Identity: {alignment.Identity}, Similarity: {alignment.SimilarityScore}");
                // Adapte esta linha conforme necessário para incluir as informações relevantes
                sb.AppendLine($">{alignment.AlignedSmallSequence}");
            }

            // Atualiza o texto do TextBox com os valores dos alinhamentos
            MyAssembly.ContigsSequence.Text = sb.ToString();
            var referenceString = string.Join("\n", myFasta.Select(fasta => $">{fasta.Sequence}"));
            MyAssembly.ReferenceSequence.Text = referenceString;
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
        private void TabControlMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Implement as needed
        }
        private void MenuItemCompareSequences_Click(object sender, RoutedEventArgs e)
        {
            CompareSequences cs = new CompareSequences();
            cs.ShowDialog();

        }

        private void MenuItemAssembly_Click(object sender, RoutedEventArgs e)
        {

            //MyAssembly.MyReferenceSequences.Text = "";

            // Concatenar e atualizar com novos valores
            var referenceString = string.Join("\n", myFasta.Select(fasta => $">{fasta.Sequence}"));
            MyAssembly.ReferenceSequence.Text = referenceString;

            var contigsSting = string.Join("\n", myAlignment.Select(contigs => $">{contigs.AlignedSmallSequence}"));
            MyAssembly.ContigsSequence.Text = contigsSting;
        }
    }
}


