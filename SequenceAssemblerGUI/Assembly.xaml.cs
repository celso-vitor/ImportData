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
using System.Diagnostics;
using SequenceAssemblerLogic;


namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {
        public List<Alignment> AlignmentList { get; set; }
        public List<Fasta> MyFasta { get; set; }

       
        public Assembly()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel();


        }

        //// Método para acessar meu DataGrid
        ////---------------------------------------------------------------------------------------------------------
        public void UpdateAlignmentGrid(double minNormalizedIdentityScore, int minNormalizedSimilarity, int minLengthFilter, List<Fasta> myFasta)
        {
            MyFasta = myFasta;


            // Apply filters on the data
            List<Alignment> filteredAlnResults = AlignmentList.Where(a => a.NormalizedIdentityScore >= minNormalizedIdentityScore && a.NormalizedSimilarity >= minNormalizedSimilarity && a.Length >= minLengthFilter).ToList();

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

            public string ToolTipContent { get; internal set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }

        public class ConsensusChar : INotifyPropertyChanged
        {
            private string _char;
            private SolidColorBrush _backgroundColor;

            public string Char
            {
                get => _char;
                set
                {
                    _char = value;
                    OnPropertyChanged();
                }
            }

            public SolidColorBrush BackgroundColor
            {
                get => _backgroundColor;
                set
                {
                    _backgroundColor = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        //Visual/Contigs
        //---------------------------------------------------------------------------------------------------------
        public class SequencesViewModel : INotifyPropertyChanged
        {

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
            private ObservableCollection<ConsensusChar> _consensusSequence;
            public ObservableCollection<ConsensusChar> ConsensusSequence
            {
                get => _consensusSequence;
                set
                {
                    _consensusSequence = value;
                    OnPropertyChanged(nameof(ConsensusSequence));
                }
            }

            private string _consensusText;
            public string ConsensusText
            {
                get => _consensusText;
                set
                {
                    _consensusText = value;
                    OnPropertyChanged(nameof(ConsensusText));
                }
            }

            public ObservableCollection<VisualAlignment> ReferenceAlignments { get; set; } = new ObservableCollection<VisualAlignment>();
            public ObservableCollection<SequencesViewModel> Seq { get; set; } = new ObservableCollection<SequencesViewModel>();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }


        }




        //Update interface
        //---------------------------------------------------------------------------------------------------------

        public static void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<Alignment> sequencesToAlign, string referenceSequence)
        {
            viewModel.ReferenceAlignments.Clear();

            // Adicionando a sequência de referência ao ViewModel
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenceAlignments.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Seq.Clear();

            // Ordenando sequências pelo início do alinhamento
            var sortedSequences = sequencesToAlign.OrderBy(seq => seq.StartPositions.Min()).ToList();

            // Mapa para rastrear as posições ocupadas em cada linha
            Dictionary<int, int> rowEndPositions = new Dictionary<int, int>();

            foreach (var sequence in sortedSequences)
            {
                string sequenceId = $"ID {sequence.ID}";
                var sequenceViewModel = new SequencesViewModel
                {
                    ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - Source: {sequence.SourceOrigin}"
                };

                int startPosition = sequence.StartPositions.Min() - 1;
                int rowIndex = FindAvailableRow(rowEndPositions, startPosition, sequence.AlignedSmallSequence.Length);

                // Preencher posições anteriores na linha encontrada
                for (int i = 0; i < startPosition; i++)
                {
                    if (i >= rowEndPositions[rowIndex])
                    {
                        sequenceViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                    }
                }

                // Atualizar o fim da linha ocupada
                rowEndPositions[rowIndex] = startPosition + sequence.AlignedSmallSequence.Length;

                // Adicionando as letras das sequências alinhadas
                foreach (char seqChar in sequence.AlignedSmallSequence)
                {
                    Brush corDeFundo;
                    int refIndex = startPosition++;
                    if (seqChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else if (refIndex < referenceSequence.Length)
                    {
                        corDeFundo = seqChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                    }
                    else
                    {
                        corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                    }
                    var visualAlignment = new VisualAlignment
                    {
                        Letra = seqChar.ToString(),
                        CorDeFundo = corDeFundo,
                        ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - Source: {sequence.SourceOrigin}"
                    };
                    sequenceViewModel.VisualAlignment.Add(visualAlignment);
                }
                // Adiciona o viewModel da sequência na linha correspondente
                while (viewModel.Seq.Count <= rowIndex)
                {
                    viewModel.Seq.Add(new SequencesViewModel());
                }
                foreach (var item in sequenceViewModel.VisualAlignment)
                {
                    viewModel.Seq[rowIndex].VisualAlignment.Add(item);
                }

            }

            // Imprimir alinhamento para depuração 
            foreach (var sequencesViewModel in viewModel.Seq)
            {
                Console.WriteLine($"Position ID: {sequencesViewModel}");
                foreach (var alignment in sequencesViewModel.VisualAlignment)
                {
                    Console.Write(alignment.Letra);
                }
                Console.WriteLine();
            }
        }

        private static int FindAvailableRow(Dictionary<int, int> rowEndPositions, int startPosition, int length)
        {
            foreach (var row in rowEndPositions)
            {
                if (row.Value <= startPosition)
                {
                    return row.Key;
                }
            }

            int newRow = rowEndPositions.Count;
            rowEndPositions[newRow] = 0;
            return newRow;
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
                MessageBox.Show("Please load data into the DataGrids before attempting to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            loadingLabel.Visibility = Visibility.Visible;
            DoEvents();

            var referenceItems = DataGridFasta.ItemsSource as List<Fasta>;
            var sequencesItems = DataGridAlignments.ItemsSource as List<Alignment>;

            if (referenceItems == null || sequencesItems == null || !referenceItems.Any() || !sequencesItems.Any())
            {
                MessageBox.Show("No data found in the DataGrids. Please load data before attempting to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string referenceSequence = referenceItems.FirstOrDefault()?.Sequence;

            var viewModel = (SequenceViewModel)DataContext;

            viewModel.Seq.Clear();
            viewModel.ReferenceAlignments.Clear();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            // Obter a lista de sequências a serem alinhadas
            List<Alignment> optSequencesToAlign = sequencesItems
                .Where(a => a.StartPositions != null && a.StartPositions.Count > 0)
                .OrderBy(a => a.StartPositions.Min())
                .ToList();

            // Aplicar a eliminação de duplicatas e subsequências às sequências a serem alinhadas
            var sequencesToAlign = Utils.EliminateDuplicatesAndSubsequences(optSequencesToAlign);

            UpdateUIWithAlignmentAndAssembly(viewModel, sequencesToAlign, referenceSequence);

            // Construir e exibir a sequência consenso
            var consensusSequence = BuildConsensus(sequencesToAlign, referenceSequence);
            viewModel.ConsensusSequence = new ObservableCollection<ConsensusChar>(consensusSequence);


            // Atualizar ConsensusText para refletir a nova sequência consenso
            viewModel.ConsensusText = String.Join("  ", viewModel.ConsensusSequence.Select(c => c.Char));

            sw.Stop();
            Console.WriteLine("Time for alignment " + sw.ElapsedMilliseconds * 1000);

            loadingLabel.Visibility = Visibility.Hidden;
        }
        public List<ConsensusChar> BuildConsensus(List<Alignment> sequencesToAlign, string referenceSequence)
        {
            int maxLength = sequencesToAlign.Max(seq => seq.StartPositions.Min() - 1 + seq.AlignedSmallSequence.Length);
            List<ConsensusChar> consensusSequence = new List<ConsensusChar>();
            int totalSequences = sequencesToAlign.Count;  // Total de sequências alinhadas para cálculo da cobertura
            List<double> coverageList = new List<double>();  // Lista para armazenar cobertura de cada posição

            for (int i = 0; i < maxLength; i++)
            {
                var column = new List<char>();
                int nonGapCount = 0;  // Contador de caracteres não-gap

                if (i < referenceSequence.Length)
                {
                    column.Add(referenceSequence[i]);
                }

                foreach (var seq in sequencesToAlign)
                {
                    int pos = i - (seq.StartPositions.Min() - 1);
                    if (pos >= 0 && pos < seq.AlignedSmallSequence.Length)
                    {
                        char charToAdd = seq.AlignedSmallSequence[pos];
                        if (charToAdd != '-')
                        {
                            column.Add(charToAdd);
                            nonGapCount++;
                        }
                    }
                }

                char consensusChar = column.GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
                SolidColorBrush color = column.All(c => c == consensusChar) ? new SolidColorBrush(Colors.LightGreen) : new SolidColorBrush(Colors.LightCoral);

                consensusSequence.Add(new ConsensusChar { Char = consensusChar.ToString(), BackgroundColor = color });

                double coverage = (double)nonGapCount / totalSequences * 100;
                coverageList.Add(coverage);  // Adiciona a cobertura da posição atual à lista
                Console.WriteLine($"Position {i}: Coverage = {coverage:F2}%");
            }

            // Calculando a cobertura total
            double totalCoverage = coverageList.Average();
            Console.WriteLine($"Total Coverage: {totalCoverage:F2}%");

            return consensusSequence;
        }




        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }



}


