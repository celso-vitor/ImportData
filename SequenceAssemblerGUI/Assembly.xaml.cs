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




        //Update interface
        //---------------------------------------------------------------------------------------------------------

        public static void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<Alignment> sequences, string referenceSequence)
        {
            
            
            viewModel.ReferenceAlignments.Clear();

            // Adicionando a sequência de referência ao ViewModel
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenceAlignments.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Seq.Clear();

            // Ordenando sequências pelo início do alinhamento
            var sortedSequences = sequences.OrderBy(seq => seq.StartPositions.Min()).ToList();

            foreach (var sequence in sortedSequences)
            {
                string sequenceId = $"ID {sequence.ID}";
                var sequenceViewModel = new SequencesViewModel
                {
                    ToolTipContent = $"Start Pos: {sequence.StartPositions.Min()}, Source: {sequence.SourceOrigin}"
                };

                // Adicionar espaços até a posição de início - ajustado para começar uma casa para trás
                int startPosition = sequence.StartPositions.Min() - 1;
                for (int i = 0; i < startPosition; i++)
                {
                    sequenceViewModel.VisualAlignment.Add(new VisualAlignment { Letra = "-", CorDeFundo = Brushes.LightGray });
                }

                // Adicionando as letras das sequências alinhadas
                foreach (char seqChar in sequence.AlignedSmallSequence)
                {
                    Brush corDeFundo;
                    if (seqChar == '-')
                    {
                        corDeFundo = Brushes.Orange; // Cor laranja para gaps
                    }
                    else
                    {
                        int refIndex = startPosition++;
                        if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = seqChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        }
                    }
                    sequenceViewModel.VisualAlignment.Add(new VisualAlignment { Letra = seqChar.ToString(), CorDeFundo = corDeFundo });
                }

                // Completar o resto da sequência com hífens se necessário
                while (sequenceViewModel.VisualAlignment.Count < referenceSequence.Length)
                {
                    sequenceViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                }

                viewModel.Seq.Add(sequenceViewModel);
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

            Stopwatch sw = new Stopwatch();
            sw.Start();


            var sequencesToAlign = sequencesItems.Select(a => a.AlignedSmallSequence).ToList();
            var optSequencesToAlign = Utils.EliminateDuplicatesAndSubsequences(sequencesToAlign);

            List<string> sourceOrigins = sequencesItems.Select(a => a.SourceOrigin).ToList();

            // Ordena os itens de sequência com base na menor posição de início do alinhamento, ajustando para começar uma casa para trás
            sequencesItems = sequencesItems
                .Where(a => a.StartPositions != null && a.StartPositions.Count > 0)
                .OrderBy(a => a.StartPositions.Min()) 
                .ToList();

            // Ordena os itens de sequência com base na posição de início do alinhamento
            sequencesItems = sequencesItems.OrderBy(a => a.StartPositions.Min()).ToList();

            foreach (Alignment sequenceItem in sequencesItems)
            {
                // Obtém as sequências alinhadas
                (string alignmentSequences, string alignedReferenceSequence) = (sequenceItem.AlignedSmallSequence, referenceSequence);

            }


            // Após concluir o loop, atualiza a interface de usuário
            UpdateUIWithAlignmentAndAssembly(viewModel, sequencesItems, referenceSequence);

            sw.Stop();
            Console.WriteLine("Time for alignment " + sw.ElapsedMilliseconds * 1000);


            // Agora que os dados foram carregados, ocultar a label "Loading..."
            loadingLabel.Visibility = Visibility.Hidden;
        }

        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }



}


