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
            public ObservableCollection<VisualAlignment> VisualAligment { get; set; } = new ObservableCollection<VisualAlignment>();

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

            public ObservableCollection<VisualAlignment> ReferenciaAlinhamentoCelulas { get; set; } = new ObservableCollection<VisualAlignment>();
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

        private void AlignSequencesFromDataGrids()
        {
            // Suponha que você tem dois DataGrids: DataGridContigs e DataGridReferences
            // E ambos têm ItemsSource configurados para listas de objetos com propriedade Sequence
            var contigs = DataGridAlignments.ItemsSource as List<Alignment>;
            var references = DataGridFasta.ItemsSource as List<Fasta>; // Ajuste conforme sua implementação real

            foreach (var contig in contigs)
            {
                foreach (var reference in references)
                {
                    // Perform the alignment
                    var result = PerformAlignmentUsingAlignmentClass(contig.AlignedSmallSequence, reference.Sequence);

                    // Agora você pode fazer algo com o resultado
                    // Por exemplo, exibir em algum lugar ou armazenar para uso posterior
                    Console.WriteLine($"Aligned Contig Sequence: {result.alignedContigSequence}");
                    Console.WriteLine($"Aligned Reference Sequence: {result.alignedReferenceSequence}");
                }
            }
        }

        // Sua função de alinhamento existente
        (string alignedContigSequence, string alignedReferenceSequence) PerformAlignmentUsingAlignmentClass(string contigSequence, string referenceSequence)
        {
            SequenceAligner aligner = new SequenceAligner(); // Crie uma instância de SequenceAligner
            Alignment alignmentResult = aligner.AlignSequences(referenceSequence, contigSequence); // Chame o método AlignSequences nessa instância

            return (alignmentResult.AlignedSmallSequence, alignmentResult.AlignedLargeSequence);
        }



        //Update interface
        //---------------------------------------------------------------------------------------------------------

        private void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<string> alignedContigSequences, List<int> startPositions, string referenceSequence)
        {
            viewModel.ReferenciaAlinhamentoCelulas.Clear();
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenciaAlinhamentoCelulas.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Seq.Clear();

            // Criar uma lista de contigs com suas posições iniciais e IDs correspondentes
            var contigsWithPositions = startPositions
                .Select((start, index) => new { ID = $"Position {start}", Sequence = alignedContigSequences[index], StartPosition = start - 1 })
                .OrderBy(c => c.StartPosition)  // Ordenar pela posição de início
                .ToList();

            // Determinar o comprimento necessário para alinhar os identificadores de contig
            int maxLabelWidth = contigsWithPositions.Max(c => c.ID.Length);

            foreach (var contig in contigsWithPositions)
            {
                string contigId = contig.ID.PadRight(maxLabelWidth);
                var contigViewModel = new SequencesViewModel { Id = contigId };

                // Adicionar espaços vazios ou hífens até a posição de início do contig
                for (int pos = 0; pos < contig.StartPosition; pos++)
                {
                    contigViewModel.VisualAligment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras do contig com a cor correspondente
                for (int i = 0; i < contig.Sequence.Length; i++)
                {
                    Brush corDeFundo;
                    char contigChar = contig.Sequence[i];
                    if (contigChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else
                    {
                        int refIndex = contig.StartPosition + i;
                        if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = contigChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        }
                    }
                    contigViewModel.VisualAligment.Add(new VisualAlignment { Letra = contigChar.ToString(), CorDeFundo = corDeFundo });
                }

                // Completar o resto da sequência com hífens se necessário
                while (contigViewModel.VisualAligment.Count < referenceSequence.Length)
                {
                    contigViewModel.VisualAligment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                viewModel.Seq.Add(contigViewModel);
            }

            // Atualizar o DataGrid, se aplicável
            List<Alignment> contigDataList = new List<Alignment>();
            foreach (var contig in contigsWithPositions)
            {
                var contigWithoutGaps = new string(contig.Sequence.Where(c => c != '-').ToArray());
                contigDataList.Add(new Alignment { Id = contig.StartPosition, AlignedSmallSequence = contigWithoutGaps });
            }

            DataGridAlignments.ItemsSource = contigDataList;



            // Imprimir alinhamento para depuração 
            foreach (var contigViewModel in viewModel.Seq)
            {
                Console.WriteLine($"Position ID: {contigViewModel.Id}");
                foreach (var aligment in contigViewModel.VisualAligment)
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
            var contigItems = DataGridAlignments.ItemsSource as List<Alignment>;

            if (referenceItems == null || contigItems == null || !referenceItems.Any() || !contigItems.Any())
            {
                // Verifica se as listas de itens estão vazias ou nulas
                MessageBox.Show("No data found in the DataGrids. Please load data before attempting to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string referenceSequence = referenceItems.FirstOrDefault()?.Sequence;

            var viewModel = (SequenceViewModel)DataContext;

            viewModel.Seq.Clear();
            viewModel.ReferenciaAlinhamentoCelulas.Clear();

            List<string> alignedContigSequences = new List<string>();
            List<int> startPositions = new List<int>();

            foreach (var contigItem in contigItems)
            {
                (string alignedContigSequence, string alignedReferenceSequence) = PerformAlignmentUsingAlignmentClass(contigItem.AlignedSmallSequence, referenceSequence);
                alignedContigSequences.Add(alignedContigSequence);

                int startPosition = assemblyParameters.GetCorrectStartPosition(alignedReferenceSequence, alignedContigSequence, referenceSequence);
                startPositions.Add(startPosition);
            }

            viewModel.AssemblySequence = assemblyParameters.GenerateAssemblyText(referenceSequence, alignedContigSequences, startPositions);

            UpdateUIWithAlignmentAndAssembly(viewModel, alignedContigSequences, startPositions, referenceSequence);

            viewModel.IsReferenceSequenceAligned = true;
            viewModel.IsAssemblyVisible = true;

            // Agora que os dados foram carregados, ocultar a label "Loading..."
            loadingLabel.Visibility = Visibility.Hidden;
        }



    }



}


