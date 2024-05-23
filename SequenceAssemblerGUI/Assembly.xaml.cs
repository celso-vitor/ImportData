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
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;
using static SequenceAssemblerGUI.MainWindow;
using System.Collections;

namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {

        public List<Alignment> AlignmentList { get; set; }

        public Assembly()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel();
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
            public SolidColorBrush OriginalBackgroundColor { get; set; }  // Adicionar a cor original

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


        //Visual/Sequencias
        //---------------------------------------------------------------------------------------------------------
        public class SequencesViewModel : INotifyPropertyChanged
        {
            public string StartPositions { get; set; }

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
            private bool _colorIL;

            public ObservableCollection<ConsensusChar> ConsensusSequence
            {
                get => _consensusSequence;
                set
                {
                    _consensusSequence = value;
                    OnPropertyChanged(nameof(ConsensusSequence));
                }
            }

            public bool ColorIL
            {
                get => _colorIL;
                set
                {
                    _colorIL = value;
                    OnPropertyChanged(nameof(ColorIL));
                    UpdateConsensusColoring();
                }
            }

            public ObservableCollection<VisualAlignment> ReferenceAlignments { get; set; } = new ObservableCollection<VisualAlignment>();
            public ObservableCollection<SequencesViewModel> Seq { get; set; } = new ObservableCollection<SequencesViewModel>();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public void UpdateConsensusColoring()
            {
                if (ConsensusSequence == null || Seq == null) return;

                for (int i = 0; i < ConsensusSequence.Count; i++)
                {
                    var consensusChar = ConsensusSequence[i];
                    var columnChars = new List<char>();
                    bool hasRedIL = false;

                    foreach (var seq in Seq)
                    {
                        if (i < seq.VisualAlignment.Count)
                        {
                            var alignmentChar = seq.VisualAlignment[i];
                            if (alignmentChar.Letra != " " && (alignmentChar.Letra == "I" || alignmentChar.Letra == "L") && alignmentChar.CorDeFundo == Brushes.LightCoral)
                            {
                                hasRedIL = true;
                            }
                            if (alignmentChar.Letra != " ")
                            {
                                columnChars.Add(alignmentChar.Letra[0]);
                            }
                        }
                    }

                    if (ColorIL && hasRedIL)
                    {
                        consensusChar.BackgroundColor = new SolidColorBrush(Colors.LightGreen);
                    }
                    else
                    {
                        consensusChar.BackgroundColor = consensusChar.OriginalBackgroundColor;
                    }
                }
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
                    string letra;

                    if (seqChar == '-')
                    {
                        corDeFundo = Brushes.LightGray; // Cor cinza para gaps
                        letra = " "; // Adiciona um espaço em branco no lugar do gap
                    }
                    else if (refIndex < referenceSequence.Length)
                    {
                        corDeFundo = seqChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                        letra = seqChar.ToString();
                    }
                    else
                    {
                        corDeFundo = Brushes.LightGray; // Fora dos limites da referência
                        letra = seqChar.ToString();
                    }

                    var visualAlignment = new VisualAlignment
                    {
                        Letra = letra,
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

            // Preencher até o final da sequência de referência com espaços em branco apenas nas últimas sequências adicionadas
            foreach (var sequenceViewModel in viewModel.Seq)
            {
                int currentLength = sequenceViewModel.VisualAlignment.Count;
                if (currentLength < referenceSequence.Length)
                {
                    for (int i = currentLength; i < referenceSequence.Length; i++)
                    {
                        sequenceViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                    }
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
        //public static void DoEvents()
        //{
        //    Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        //}

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAssembly();
        }

        public void ExecuteAssembly()
        {
            if (DataGridFasta.ItemsSource == null || DataGridAlignments.ItemsSource == null)
            {
                MessageBox.Show("Please load data into the DataGrids before attempting to compare.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //loadingLabel.Visibility = Visibility.Visible;
            ////DoEvents();

            var referenceItems = DataGridFasta.ItemsSource as List<Fasta>;
            var sequencesItems = DataGridAlignments.ItemsSource as List<Alignment>;

            string referenceSequence = referenceItems.FirstOrDefault()?.Sequence;

            var viewModel = (SequenceViewModel)DataContext;

            viewModel.Seq.Clear();
            viewModel.ReferenceAlignments.Clear();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<Alignment> optSequencesToAlign = sequencesItems
                .Where(a => a.StartPositions != null && a.StartPositions.Count > 0)
                .OrderBy(a => a.StartPositions.Min())
                .ToList();

            var sequencesToAlign = Utils.EliminateDuplicatesAndSubsequences(optSequencesToAlign);

            UpdateUIWithAlignmentAndAssembly(viewModel, sequencesToAlign, referenceSequence);

            var (consensusChars, totalCoverage) = BuildConsensus(sequencesToAlign, referenceSequence);

            viewModel.ConsensusSequence = new ObservableCollection<ConsensusChar>(consensusChars);

            //viewModel.ConsensusText = String.Join("  ", viewModel.ConsensusSequence.Select(c => c.Char));
            viewModel.UpdateConsensusColoring();

            sw.Stop();
            Console.WriteLine("Time for alignment: " + sw.ElapsedMilliseconds * 1000 + " microseconds");

            //loadingLabel.Visibility = Visibility.Hidden;
            DownloadConsensus.IsEnabled = true;
            AssemblyConsensus.Visibility = Visibility.Visible;

            // Carrega os dados de alinhamento simultaneamente e mede o tempo
            Stopwatch swDataGrid = new Stopwatch();
            swDataGrid.Start();

            LoadAlignmentsData();

            swDataGrid.Stop();
            Console.WriteLine("Time for DataGrid assembly: " + swDataGrid.ElapsedMilliseconds * 1000 + " microseconds");
        }
        private void LoadAlignmentsData()
        {
            // Lógica para carregar os dados de alinhamentos
            var sequencesItems = DataGridAlignments.ItemsSource as List<Alignment>;
            if (sequencesItems != null)
            {
                var alignmentViewModel = new SequenceViewModel();
                foreach (var sequence in sequencesItems)
                {
                    alignmentViewModel.Seq.Add(new SequencesViewModel
                    {
                        VisualAlignment = new ObservableCollection<VisualAlignment>(
                            sequence.AlignedSmallSequence.Select(c => new VisualAlignment
                            {
                                Letra = c.ToString(),
                                CorDeFundo = c == '-' ? Brushes.LightGray : Brushes.White
                            })
                        )
                    });
                }
            }
        }

        public (List<ConsensusChar>, double) BuildConsensus(List<Alignment> sequencesToAlign, string referenceSequence)
        {
            int maxLength = Math.Max(sequencesToAlign.Max(seq => seq.StartPositions.Min() - 1 + seq.AlignedSmallSequence.Length), referenceSequence.Length);
            List<ConsensusChar> consensusSequence = new List<ConsensusChar>();
            int totalSequences = sequencesToAlign.Count;
            int coloredPositions = 0;

            for (int i = 0; i < maxLength; i++)
            {
                var column = new List<char>();
                bool fromReferenceOnly = false;

                if (i < referenceSequence.Length)
                {
                    column.Add(referenceSequence[i]);
                    fromReferenceOnly = true;
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
                            fromReferenceOnly = false;
                        }
                    }
                }

                char consensusChar = column.GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();
                SolidColorBrush color;

                if (fromReferenceOnly)
                {
                    color = new SolidColorBrush(Colors.White);
                }
                else if (column.All(c => c == consensusChar))
                {
                    color = new SolidColorBrush(Colors.LightGreen);
                    coloredPositions++;
                }
                else
                {
                    color = new SolidColorBrush(Colors.Orange);
                    coloredPositions++;
                }

                consensusSequence.Add(new ConsensusChar { Char = consensusChar.ToString(), BackgroundColor = color, OriginalBackgroundColor = color });
            }

            double overallCoverage = (double)coloredPositions / referenceSequence.Length * 100;
            Console.WriteLine($"Overall Coverage: {overallCoverage:F2}%");

            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.LabelCoverage.Content = $"{overallCoverage:F2}%";
            }

            return (consensusSequence, overallCoverage);
        }


        private void OnColorILChecked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SequenceViewModel;
            if (viewModel != null)
            {
                viewModel.ColorIL = true;
                viewModel.UpdateConsensusColoring();
            }
        }

        private void OnColorILUnchecked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SequenceViewModel;
            if (viewModel != null)
            {
                viewModel.ColorIL = false;
                viewModel.UpdateConsensusColoring();
            }
        }
       

        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }


        private void DownloadConsensusButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = (SequenceViewModel)DataContext;

            if (viewModel.ConsensusSequence == null || !viewModel.ConsensusSequence.Any())
            {
                MessageBox.Show("No consensus sequence available. Please generate it before attempting to download.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveConsensusSequenceToFile(viewModel.ConsensusSequence);
        }

        private void SaveConsensusSequenceToFile(ObservableCollection<ConsensusChar> consensusSequence)
        {
            var dialog = new SaveFileDialog()
            {
                Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "ConsensusSequence.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                using (var writer = new StreamWriter(dialog.FileName))
                {
                    foreach (var item in consensusSequence)
                    {
                        writer.Write(item.Char);  // Apenas escreve o caractere
                    }
                }
                MessageBox.Show($"Consensus sequence saved to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }



    }



}


