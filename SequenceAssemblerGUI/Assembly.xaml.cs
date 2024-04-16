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



namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {
        //public TextBox MyReferenceSequences
        //{
        //    get
        //    {
        //        return ReferenceSequence;
        //    }

        //    set
        //    {
        //        ReferenceSequence = value;
        //    }
        //}

        private AssemblyParameters assemblyParameters;
      

        public List<Alignment> AlignmentList { get; set; }
        public List<Fasta> MyFasta { get; set; }

        public List<Contig> Contigs { get; set; }

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
            DataGridAlignments.ItemsSource = null; // Clear previous items
            DataGridAlignments.ItemsSource = dataTable.DefaultView;

            DataGridFasta.ItemsSource = MyFasta;


        }


        //DataGrid Ornenar Contigs
        //---------------------------------------------------------------------------------------------------------
        public class ContigData
        {
            public int Id { get; set; }
            public string Contig { get; set; }
        }




        //Visual/Cores 
        //---------------------------------------------------------------------------------------------------------
        public class Aligment : INotifyPropertyChanged
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
        public class ContigViewModel : INotifyPropertyChanged
        {
            public string Id { get; set; }
            public ObservableCollection<Aligment> Aligments { get; set; } = new ObservableCollection<Aligment>();

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

            public ObservableCollection<Aligment> ReferenciaAlinhamentoCelulas { get; set; } = new ObservableCollection<Aligment>();
            public ObservableCollection<ContigViewModel> Contigs { get; set; } = new ObservableCollection<ContigViewModel>();

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
            var contigs = DataGridContigsAssembly.ItemsSource as List<Contig>;
            var references = DataGridFasta.ItemsSource as List<Fasta>; // Ajuste conforme sua implementação real

            foreach (var contig in contigs)
            {
                foreach (var reference in references)
                {
                    // Perform the alignment
                    var result = PerformAlignmentUsingAlignmentClass(contig.Sequence, reference.Sequence);

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
                var corDeFundo = Brushes.White;
                viewModel.ReferenciaAlinhamentoCelulas.Add(new Aligment { Letra = letra.ToString(), CorDeFundo = corDeFundo });
            }

            viewModel.Contigs.Clear();

            // Determinar o comprimento necessário para alinhar os identificadores de contig e o cabeçalho "Template"
            int maxLabelWidth = Math.Max("Template".Length, alignedContigSequences.Count.ToString().Length + "Contig ".Length);

            for (int contigIndex = 0; contigIndex < alignedContigSequences.Count; contigIndex++)
            {
                string contigSequence = alignedContigSequences[contigIndex];

                // PadRight garante que todos os identificadores de contig tenham o mesmo comprimento
                string contigId = ("Contig " + (contigIndex + 1)).PadRight(maxLabelWidth);
                var contigViewModel = new ContigViewModel { Id = contigId };
                int startPosition = startPositions[contigIndex] - 1; // Ajuste para índice base-0

                // Construir a linha para o contig atual
                // StringBuilder não é mais necessário porque o PadRight já alinha o texto
                contigViewModel.Id = contigId;

                // Adicionar espaços vazios ou hífens até a posição de início do contig
                for (int pos = 0; pos < startPosition; pos++)
                {
                    contigViewModel.Aligments.Add(new Aligment { Letra = "-", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras do contig com a cor correspondente
                for (int i = 0; i < contigSequence.Length; i++)
                {
                    Brush corDeFundo;
                    char contigChar = contigSequence[i];
                    // Checa se a posição é um gap
                    if (contigChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else
                    {
                        // Posição na sequência de referência
                        int refIndex = startPosition + i;
                        if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = contigChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        }
                    }

                    contigViewModel.Aligments.Add(new Aligment { Letra = contigChar.ToString(), CorDeFundo = corDeFundo });
                }

                // Completar o resto da sequência com hífens se necessário
                while (contigViewModel.Aligments.Count < referenceSequence.Length)
                {
                    contigViewModel.Aligments.Add(new Aligment { Letra = "-", CorDeFundo = Brushes.LightGray });
                }

                viewModel.Contigs.Add(contigViewModel);
            }




            // Criar uma lista para os dados do DataGrid
            List<ContigData> contigDataList = new List<ContigData>();

            foreach (var contigIndex in startPositions.Select((value, index) => new { value, index }))
            {
                // Pegar o par de contig correspondente à posição
                var contigPair = alignedContigSequences.ElementAt(contigIndex.index);

                // Filtrar os gaps do contig
                var contigWithoutGaps = new string(contigPair.Where(c => c != '-').ToArray());

                // Adicionar o contig à lista de dados, incluindo a posição inicial
                contigDataList.Add(new ContigData
                {
                    Id = contigIndex.value,
                    Contig = contigWithoutGaps
                });
            }

            DataGridContigID.ItemsSource = contigDataList;

            // Imprimir alinhamento para depuração 
            foreach (var contigViewModel in viewModel.Contigs)
            {
                Console.WriteLine($"Contig: {contigViewModel.Id}");
                foreach (var aligment in contigViewModel.Aligments)
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
            // Mostrar a label "Loading..."
            loadingLabel.Visibility = Visibility.Visible;
            DoEvents();

            var referenceItems = (List<Fasta>)DataGridFasta.ItemsSource;
            var contigItems = (List<ContigData>)DataGridContigsAssembly.ItemsSource;

            string referenceSequence = referenceItems.FirstOrDefault()?.Sequence;

            var viewModel = (SequenceViewModel)DataContext;

            viewModel.Contigs.Clear();
            viewModel.ReferenciaAlinhamentoCelulas.Clear();

            List<string> alignedContigSequences = new List<string>();
            List<int> startPositions = new List<int>();

            foreach (var contigItem in contigItems)
            {
                (string alignedContigSequence, string alignedReferenceSequence) = PerformAlignmentUsingAlignmentClass(contigItem.Contig, referenceSequence);
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


        //Montagem de Grid
        //---------------------------------------------------------------------------------------------------------

        //MyAssemblyViewer.Display(alignedContigs, referenceSequence)
    }



}


