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

        public Assembly()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel(); 
            assemblyParameters = new AssemblyParameters();
        }

        //// Método para acessar meu DataGrid
        ////---------------------------------------------------------------------------------------------------------

        public void UpdateAlignmentGrid(int minIdentity, int minNormalizedSimilarity, List<Fasta> myFasta)
        {
            MyFasta = myFasta;

            // Apply filters on the data
            List<Alignment> filteredAlnResults = AlignmentList.Where(a => a.Identity >= minIdentity && a.NormalizedSimilarity >= minNormalizedSimilarity).ToList();


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

        (string alignedContigSequence, string alignedReferenceSequence) PerformAlignmentUsingAlignmentClass(string contigSequence, string referenceSequence)
        {
            SequenceAligner aligner = new SequenceAligner(); // Crie uma instância de SequenceAligner
            Alignment alignmentResult = aligner.AlignSequences(referenceSequence, contigSequence); // Chame o método AlignSequences nessa instância

            return (alignmentResult.AlignedSmallSequence, alignmentResult.AlignedLargeSequence);
        }

        //Uptade do Alinhamento com a Interface
        //---------------------------------------------------------------------------------------------------------

        private void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, Dictionary<string, string> alignedContigs, List<int> startPositions, string referenceSequence)
        {
            viewModel.ReferenciaAlinhamentoCelulas.Clear();
            foreach (char letra in referenceSequence)
            {
                var corDeFundo = Brushes.White;
                viewModel.ReferenciaAlinhamentoCelulas.Add(new Aligment { Letra = letra.ToString(), CorDeFundo = corDeFundo });
            }

            viewModel.Contigs.Clear();
            for (int contigIndex = 0; contigIndex < alignedContigs.Count; contigIndex++)
            {
                var contigPair = alignedContigs.ElementAt(contigIndex);
                var contigViewModel = new ContigViewModel { Id = contigPair.Key };
                int startPosition = startPositions[contigIndex] - 1; // Ajuste para índice base-0

                // Adicionar espaços vazios ou hífens até a posição de início do contig
                for (int pos = 0; pos < startPosition; pos++)
                {
                    contigViewModel.Aligments.Add(new Aligment { Letra = "-", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras do contig com a cor correspondente
                for (int i = 0; i < contigPair.Value.Length; i++)
                {
                    Brush corDeFundo;
                    char contigChar = contigPair.Value[i];

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

            // Atualizar a montagem na UI
            viewModel.AssemblySequence = assemblyParameters.GenerateAssemblyText(referenceSequence, alignedContigs, startPositions);

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
        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var referenceSequenceFasta = ReferenceSequence.Text;
            var contigsFasta = ContigsSequence.Text;

            string referenceSequence = FastaFormat.ReadFastaSequence(referenceSequenceFasta);
            var contigs = FastaFormat.ReadContigs(contigsFasta);
            var viewModel = (SequenceViewModel)DataContext;

            viewModel.Contigs.Clear();
            viewModel.ReferenciaAlinhamentoCelulas.Clear();

            Dictionary<string, string> alignedContigs = new Dictionary<string, string>();
            List<int> startPositions = new List<int>(); // Lista para armazenar as posições de início
            
            foreach (var contig in contigs)
            {
                (string alignedContigSequence, string alignedReferenceSequence) = PerformAlignmentUsingAlignmentClass(contig.Value, referenceSequence);
                alignedContigs.Add(contig.Key, alignedContigSequence);

                // Obter a posição correta de início com base no alinhamento
                int startPosition = assemblyParameters.GetCorrectStartPosition(alignedReferenceSequence, alignedContigSequence, referenceSequence);
                startPositions.Add(startPosition);
            }

            viewModel.AssemblySequence = assemblyParameters.GenerateAssemblyText(referenceSequence, alignedContigs, startPositions);

            // Atualiza a UI com o alinhamento e a montagem
            UpdateUIWithAlignmentAndAssembly(viewModel, alignedContigs, startPositions, referenceSequence);

            // Definir IsReferenceSequenceAligned como true para garantir a visibilidade do rótulo "Reference"
            viewModel.IsReferenceSequenceAligned = true;
            viewModel.IsAssemblyVisible = true;


            //Montagem de Grid
            //---------------------------------------------------------------------------------------------------------

            //MyAssemblyViewer.Display(alignedContigs, referenceSequence)
        }

       

    }
}

