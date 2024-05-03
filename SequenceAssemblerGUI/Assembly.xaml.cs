using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Media;
using SequenceAssemblerLogic.ContigCode;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using static SequenceAssemblerGUI.Assembly;
using SequenceAssemblerLogic.Tools;
using SequenceAssemblerLogic.AssemblyTools;
using System.Windows.Controls;
using System.Data;
using System.Windows.Data; 
using System.Globalization;
using System.CodeDom.Compiler;
using System.Windows.Threading;
using SequenceAssemblerLogic.ResultParser;
using System.ComponentModel;


namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {

        private AssemblyParameters assemblyParameters;

        public List<Alignment> AlignmentList { get; set; }
        public List<Fasta> MyFasta { get; set; }
        public List<Contig> Contigs { get; set; }

        public Dictionary<string, List<string>> SequenceOrigin = new Dictionary<string, List<string>>();

        public Dictionary<string, List<string>> denovoValue = new Dictionary<string, List<string>>();

        public Dictionary<string, List<string>> psmValue = new Dictionary<string, List<string>>();

        public Assembly()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel();
            assemblyParameters = new AssemblyParameters();
            SequenceOrigin = new Dictionary<string, List<string>>();
            denovoValue = new Dictionary<string, List<string>>();
            psmValue = new Dictionary<string, List<string>>();

        }

        //// Método para acessar meu DataGrid
        ////---------------------------------------------------------------------------------------------------------
        public void UpdateAlignmentGrid(double minNormalizedIdentityScore, int minNormalizedSimilarity, List<Fasta> myFasta)
        {
            MyFasta = myFasta;


            // Apply filters on the data
            List<Alignment> filteredAlnResults = AlignmentList.Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();

            DataTable dataTable = new DataTable();

            // Define the DataTable columns with the appropriate data types
            dataTable.Columns.Add("Identity", typeof(int));
            dataTable.Columns.Add("NormalizedIdentityScore", typeof(double));
            dataTable.Columns.Add("SimilarityScore", typeof(int));
            dataTable.Columns.Add("NormalizedSimilarity", typeof(double));
            dataTable.Columns.Add("AlignedAA", typeof(int));
            dataTable.Columns.Add("NormalizedAlignedAA", typeof(double));
            dataTable.Columns.Add("GapsUsed", typeof(int));
            dataTable.Columns.Add("AlignedLargeSequence", typeof(string));
            dataTable.Columns.Add("AlignedSmallSequence", typeof(string));

            // Fill the DataTable with your data
            foreach (var alignment in filteredAlnResults)
            {

                DataRow newRow = dataTable.NewRow();
                newRow["Identity"] = alignment.Identity;
                newRow["NormalizedIdentityScore"] = alignment.NormalizedIdentityScore;
                newRow["SimilarityScore"] = alignment.SimilarityScore;
                newRow["NormalizedSimilarity"] = alignment.NormalizedSimilarity;
                newRow["AlignedAA"] = alignment.AlignedAA;
                newRow["NormalizedAlignedAA"] = alignment.NormalizedAlignedAA;
                newRow["GapsUsed"] = alignment.GapsUsed;
                newRow["AlignedLargeSequence"] = alignment.AlignedLargeSequence;
                newRow["AlignedSmallSequence"] = alignment.AlignedSmallSequence;


                dataTable.Rows.Add(newRow);
            }

            // Set the DataTable as the data source for your control 
            DataGridAlignments.ItemsSource = dataTable.DefaultView;
            DataGridFasta.ItemsSource = MyFasta;
        }



        //Visual/Cores 
        //---------------------------------------------------------------------------------------------------------
        public class VisualAlignment : INotifyPropertyChanged
        {
            private string _letra;
            private Brush _corDeFundo;

            public string Letra
            {
                get { return _letra; }
                set { _letra = value; OnPropertyChanged(); }
            }

            public Brush CorDeFundo
            {
                get { return _corDeFundo; }
                set { _corDeFundo = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        //Visual/Contigs
        //---------------------------------------------------------------------------------------------------------
        public class SequencesViewModel : INotifyPropertyChanged
        {
            public string Id { get; set; }

            private string _toolTipContent;
            public ObservableCollection<VisualAlignment> VisualAlignment { get; set; } = new ObservableCollection<VisualAlignment>();

            public string ToolTipContent
            {
                get { return _toolTipContent; }
                set
                {
                    if (_toolTipContent != value)
                    {
                        _toolTipContent = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }



        //Visual/Interface
        //---------------------------------------------------------------------------------------------------------
        public class SequenceViewModel : INotifyPropertyChanged
        {
            

            public ObservableCollection<VisualAlignment> ReferenceAlignments { get; set; } = new ObservableCollection<VisualAlignment>();
            public ObservableCollection<SequencesViewModel> Seq { get; set; } = new ObservableCollection<SequencesViewModel>();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }


        }


        //Alinhamento
        //---------------------------------------------------------------------------------------------------------
        public (string, string) PerformAlignmentUsingAlignmentClass(string alignedSmallSequence, string referenceSequence, List<(string sequence, string sourceOrigin, string peptide, string folder)> sequenceOrigins)
        {
            SequenceAligner aligner = new SequenceAligner();
            List<(string AlignedSmallSequence, string AlignedLargeSequence)> results = new List<(string, string)>();

            // Processa cada origem de sequência individualmente
            foreach (var origin in sequenceOrigins)
            {
                // Usando o campo sourceOrigin para alinhamento
                Alignment alignmentResult = aligner.AlignSequences(referenceSequence, alignedSmallSequence, origin.sourceOrigin);
                results.Add((alignmentResult.AlignedSmallSequence, alignmentResult.AlignedLargeSequence));
            }

            // Se você quer retornar apenas um resultado, precisa decidir qual retornar
            // Aqui, estou assumindo que você quer o primeiro, mas você pode ajustar conforme necessário
            return results.FirstOrDefault();
        }



        //Update interface
        //---------------------------------------------------------------------------------------------------------

        private void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<string> alignedSequences, List<int> startPositions, string referenceSequence, List<(string sequence, string sourceOrigin, string peptides, string folder)> sequenceOrigins)
        {
            viewModel.ReferenceAlignments.Clear();
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenceAlignments.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Seq.Clear();

            // Criar uma lista de contigs com suas posições iniciais e IDs correspondentes
            var seqeuncesWithPositions = startPositions
                .Select((start, index) => new { ID = $"{start}", Sequence = alignedSequences[index], StartPosition = start - 1 })
                .OrderBy(c => c.StartPosition)  // Ordenar pela posição de início
                .ToList();

            // Determinar o comprimento necessário para alinhar os identificadores das sequências
            int maxLabelWidth = seqeuncesWithPositions.Max(c => c.ID.Length);

            for (int i = 0; i < seqeuncesWithPositions.Count; i++)
            {
                var sequences = seqeuncesWithPositions[i];
                string sequencesId = sequences.ID.PadRight(maxLabelWidth);

                // Encontre a origem associada à sequência atual
                var sequenceOrigin = sequenceOrigins[i];
                string sourceOrigin = sequenceOrigin.sourceOrigin;
                

                var sequencesViewModel = new SequencesViewModel
                {
                    Id = sequencesId,
                    ToolTipContent = $"Position: {sequences.ID} - Source: {sourceOrigin}" // Adicionando a origem da sequência, Peptide e Folder ao tooltip
                };

                // Adicionar espaços vazios ou hífens até a posição de início da sequência
                for (int pos = 0; pos < sequences.StartPosition; pos++)
                {
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras das sequências com as cores correspondentes
                for (int j = 0; j < sequences.Sequence.Length; j++)
                {
                    Brush corDeFundo;
                    char seqChar = sequences.Sequence[j];
                    if (seqChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else
                    {
                        int refIndex = sequences.StartPosition + j;
                        if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = seqChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        }
                    }
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = seqChar.ToString(), CorDeFundo = corDeFundo });
                }

                // Completar o resto da sequência com hífens se necessário
                while (sequencesViewModel.VisualAlignment.Count < referenceSequence.Length)
                {
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                viewModel.Seq.Add(sequencesViewModel);
            }

            // Imprimir alinhamento para depuração 
            foreach (var sequencesViewModel in viewModel.Seq)
            {
                Console.WriteLine($"Position ID: {sequencesViewModel.Id}");
                foreach (var alignment in sequencesViewModel.VisualAlignment)
                {
                    Console.Write(alignment.Letra);
                }
                Console.WriteLine();
            }
        }






        //Botton Click/ Montagem dos alinhamentos
        //---------------------------------------------------------------------------------------------------------
        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridFasta.ItemsSource == null || DataGridAlignments.ItemsSource == null)
            {
                // Verifica se as fontes de itens estão definidas
                MessageBox.Show("Please load data into the DataGrids before attempting to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Mostrar a label "Loading..."
            loadingLabel.Visibility = Visibility.Visible;
            DoEvents();

            var referenceItems = DataGridFasta.ItemsSource as List<Fasta>;
            var sequencesItems = DataGridAlignments.ItemsSource as List<Alignment>;

            if (referenceItems == null || sequencesItems == null || !referenceItems.Any() || !sequencesItems.Any())
            {
                // Verifica se as listas de itens estão vazias ou nulas
                MessageBox.Show("No data found in the DataGrids. Please load data before attempting to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string referenceSequence = referenceItems.FirstOrDefault()?.Sequence;

            var viewModel = (SequenceViewModel)DataContext;

            viewModel.Seq.Clear();
            viewModel.ReferenceAlignments.Clear();

            List<string> alignedSequences = new List<string>();
            List<int> startPositions = new List<int>();
            List<(string sequence, string sourceOrigin, string peptide, string folder)> sequenceOrigins = new List<(string sequence, string sourceOrigin, string peptide, string folder)>();
            Console.WriteLine(sequenceOrigins);
            foreach (var sequenceItem in sequencesItems)
            {

                // Exemplo de como você poderia dividir a string SourceOrigin
                string[] parts = sequenceItem.SourceOrigin.Split(','); // Ajuste o delimitador conforme necessário
                string peptide = parts.Length > 0 ? parts[0] : "";
                string folder = parts.Length > 1 ? parts[1] : "";

                sequenceOrigins.Add((sequenceItem.AlignedSmallSequence, sequenceItem.SourceOrigin, peptide, folder));


                // Prossiga com os cálculos e ajustes de alinhamento
                (string alignmentSequences, string alignedReferenceSequence) = PerformAlignmentUsingAlignmentClass(sequenceItem.AlignedSmallSequence, referenceSequence, sequenceOrigins);
                alignedSequences.Add(alignmentSequences);

                int startPosition = assemblyParameters.GetCorrectStartPosition(alignedReferenceSequence, alignmentSequences, referenceSequence);
                startPositions.Add(startPosition);
            }
            // Ordene a lista de sequências e sourceOrigins com base na posição inicial das sequências em relação à referência
            sequenceOrigins = sequenceOrigins.OrderBy(seq => startPositions[alignedSequences.IndexOf(seq.sequence)]).ToList();

            UpdateUIWithAlignmentAndAssembly(viewModel, alignedSequences, startPositions, referenceSequence, sequenceOrigins);

            // Agora que os dados foram carregados, ocultar a label "Loading..."
            loadingLabel.Visibility = Visibility.Hidden;
        }

    }



}


