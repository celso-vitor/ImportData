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
            dataTable.Columns.Add("Normalized Identity Score", typeof(double));
            dataTable.Columns.Add("Similarity Score", typeof(int));
            dataTable.Columns.Add("Normalized Similarity", typeof(double));
            dataTable.Columns.Add("AlignedAA", typeof(int));
            dataTable.Columns.Add("Normalized AlignedAA", typeof(double));
            dataTable.Columns.Add("Gaps Used", typeof(int));
            dataTable.Columns.Add("Aligned Large Sequence", typeof(string));
            dataTable.Columns.Add("Aligned Small Sequence", typeof(string));

            // Fill the DataTable with your data
            foreach (var alignment in filteredAlnResults)
            {

                DataRow newRow = dataTable.NewRow();
                newRow["Identity"] = alignment.Identity;
                newRow["Normalized Identity Score"] = alignment.NormalizedIdentityScore;
                newRow["Similarity Score"] = alignment.SimilarityScore;
                newRow["Normalized Similarity"] = alignment.NormalizedSimilarity;
                newRow["AlignedAA"] = alignment.AlignedAA;
                newRow["Normalized AlignedAA"] = alignment.NormalizedAlignedAA;
                newRow["Gaps Used"] = alignment.GapsUsed;
                newRow["Aligned Large Sequence"] = alignment.AlignedLargeSequence;
                newRow["Aligned Small Sequence"] = alignment.AlignedSmallSequence;


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
        (string alignedSequence, string alignedReferenceSequence) PerformAlignmentUsingAlignmentClass(string sequence, string referenceSequence, string sourceOrigin)
        {
            SequenceAligner aligner = new SequenceAligner(); // Crie uma instância de SequenceAligner
            Alignment alignmentResult = aligner.AlignSequences(referenceSequence, sequence, sourceOrigin); // Chame o método AlignSequences nessa instância

            return (alignmentResult.AlignedSmallSequence, alignmentResult.AlignedLargeSequence);
        }



        //Update interface
        //---------------------------------------------------------------------------------------------------------

        private void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<string> alignedSequences, List<int> startPositions, string referenceSequence, List<(string sequence, string sourceOrigin)> sequenceOrigins)
        {
            viewModel.ReferenceAlignments.Clear();
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenceAlignments.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Seq.Clear();

            // Criar uma lista de contigs com suas posições iniciais e IDs correspondentes
            var contigsWithPositions = startPositions
                .Select((start, index) => new { ID = $"Position {start}", Sequence = alignedSequences[index], StartPosition = start - 1 })
                .OrderBy(c => c.StartPosition)  // Ordenar pela posição de início
                .ToList();

            // Determinar o comprimento necessário para alinhar os identificadores de contig
            int maxLabelWidth = contigsWithPositions.Max(c => c.ID.Length);

            for (int i = 0; i < contigsWithPositions.Count; i++)
            {
                var sequences = contigsWithPositions[i];
                string sequencesId = sequences.ID.PadRight(maxLabelWidth);

                // Encontre o sourceOrigin associado à sequência atual
                string sourceOrigin = sequenceOrigins[i].sourceOrigin;

                var sequencesViewModel = new SequencesViewModel
                {
                    Id = sequencesId,
                    ToolTipContent = $"Position ID: {sequences.ID}, Source Origin: {sourceOrigin}" // Adicionando a origem da sequência ao tooltip
                };

                // Adicionar espaços vazios ou hífens até a posição de início do contig
                for (int pos = 0; pos < sequences.StartPosition; pos++)
                {
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras do contig com a cor correspondente
                for (int j = 0; j < sequences.Sequence.Length; j++)
                {
                    Brush corDeFundo;
                    char contigChar = sequences.Sequence[j];
                    if (contigChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else
                    {
                        int refIndex = sequences.StartPosition + j;
                        if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = contigChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        }
                    }
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = contigChar.ToString(), CorDeFundo = corDeFundo });
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
                foreach (var aligment in sequencesViewModel.VisualAlignment)
                {
                    Console.Write(aligment.Letra);
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
            List<(string sequence, string sourceOrigin)> sequenceOrigins = new List<(string sequence, string sourceOrigin)>();

            foreach (var sequenceItem in sequencesItems)
            {
                string sourceOrigin = sequenceItem.SourceOrigin;
                sequenceOrigins.Add((sequenceItem.AlignedSmallSequence, sourceOrigin)); // Adiciona a sequência e seu sourceOrigin associado à lista
                (string alignmentSequences, string alignedReferenceSequence) = PerformAlignmentUsingAlignmentClass(sequenceItem.AlignedSmallSequence, referenceSequence, sourceOrigin);
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


