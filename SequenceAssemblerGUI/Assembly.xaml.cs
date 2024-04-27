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



namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {
        

        private AssemblyParameters assemblyParameters;
      
        public List<Alignment> AlignmentList { get; set; }
        public List<Fasta> MyFasta { get; set; }
        public List<Contig> Contigs { get; set; }
        public List<IDResult> Results { get; set; }

        public Assembly()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel(); 
            assemblyParameters = new AssemblyParameters();
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
                newRow[0] = alignment.Identity;
                newRow[1] = alignment.NormalizedIdentityScore;
                newRow[2] = alignment.SimilarityScore;
                newRow[3] = alignment.NormalizedSimilarity;
                newRow[4] = alignment.AlignedAA;
                newRow[5] = alignment.NormalizedAlignedAA;
                newRow[6] = alignment.GapsUsed;
                newRow[7] = alignment.AlignedLargeSequence;
                newRow[8] = alignment.AlignedSmallSequence;
               

                dataTable.Rows.Add(newRow);
            }

            // Set the DataTable as the data source for your control 
            //DataGridAlignments.ItemsSource = null; // Clear previous items
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
            public ObservableCollection<VisualAlignment> VisualAlignment { get; set; } = new ObservableCollection<VisualAlignment>();

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
            private bool _isReferenceSequenceAligned;

            public ObservableCollection<VisualAlignment> ReferenceAlignments { get; set; } = new ObservableCollection<VisualAlignment>();
            public ObservableCollection<SequencesViewModel> Seq { get; set; } = new ObservableCollection<SequencesViewModel>();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public bool IsReferenceSequenceAligned
            {
                get => _isReferenceSequenceAligned;
                set
                {
                    if (_isReferenceSequenceAligned != value)
                    {
                        _isReferenceSequenceAligned = value;
                        OnPropertyChanged();
                    }
                }
            }

            private bool _isAssemblyVisible;
            public bool IsAssemblyVisible
            {
                get { return _isAssemblyVisible; }
                set
                {
                    if (_isAssemblyVisible != value)
                    {
                        _isAssemblyVisible = value;
                        OnPropertyChanged();
                    }
                }
            }

            private string _assemblySequence;
            public string AssemblySequence
            {
                get { return _assemblySequence; }
                set
                {
                    if (_assemblySequence != value)
                    {
                        _assemblySequence = value;
                        OnPropertyChanged();
                    }
                }
            }


        }


        //Alinhamento
        //---------------------------------------------------------------------------------------------------------
        (string alignedSequence, string alignedReferenceSequence) PerformAlignmentUsingAlignmentClass(string sequence, string referenceSequence)
        {
            SequenceAligner aligner = new SequenceAligner(); // Crie uma instância de SequenceAligner
            Alignment alignmentResult = aligner.AlignSequences(referenceSequence, sequence); // Chame o método AlignSequences nessa instância

            return (alignmentResult.AlignedSmallSequence, alignmentResult.AlignedLargeSequence);
        }



        //Update interface
        //---------------------------------------------------------------------------------------------------------

        private void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<string> alignedSequences, List<int> startPositions, string referenceSequence)
        {
            viewModel.ReferenceAlignments.Clear();
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenceAlignments.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Seq.Clear();

            // Criar uma lista de sequences com suas posições iniciais e IDs correspondentes
            var sequencesWithPositions = startPositions
                .Select((start, index) => new { ID = $"Position {start}", Sequence = alignedSequences[index], StartPosition = start - 1 })
                .OrderBy(c => c.StartPosition)  // Ordenar pela posição de início
                .ToList();

            // Determinar o comprimento necessário para alinhar os identificadores de contig
            int maxLabelWidth = sequencesWithPositions.Max(c => c.ID.Length);

            foreach (var sequences in sequencesWithPositions)
            {
                string sequencesId = sequences.ID.PadRight(maxLabelWidth);
                var sequencesViewModel = new SequencesViewModel { Id = sequencesId };

                // Adicionar espaços vazios ou hífens até a posição de início do contig
                for (int pos = 0; pos < sequences.StartPosition; pos++)
                {
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras do contig com a cor correspondente
                for (int i = 0; i < sequences.Sequence.Length; i++)
                {
                    Brush corDeFundo;
                    char sequencesChar = sequences.Sequence[i];
                    if (sequencesChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else
                    {
                        int refIndex = sequences.StartPosition + i;
                        if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = sequencesChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        }
                    }
                    sequencesViewModel.VisualAlignment.Add(new VisualAlignment { Letra = sequencesChar.ToString(), CorDeFundo = corDeFundo });
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

            foreach (var sequenceItem in sequencesItems)
            {
                (string alignmentSequences, string alignedReferenceSequence) = PerformAlignmentUsingAlignmentClass(sequenceItem.AlignedSmallSequence, referenceSequence);
                alignedSequences.Add(alignmentSequences);

                int startPosition = assemblyParameters.GetCorrectStartPosition(alignedReferenceSequence, alignmentSequences, referenceSequence);
                startPositions.Add(startPosition);
            }

            viewModel.AssemblySequence = assemblyParameters.GenerateAssemblyText(referenceSequence, alignedSequences, startPositions);

            UpdateUIWithAlignmentAndAssembly(viewModel, alignedSequences, startPositions, referenceSequence);

            viewModel.IsReferenceSequenceAligned = true;
            viewModel.IsAssemblyVisible = true;

            // Agora que os dados foram carregados, ocultar a label "Loading..."
            loadingLabel.Visibility = Visibility.Hidden;
        }



    }



}


