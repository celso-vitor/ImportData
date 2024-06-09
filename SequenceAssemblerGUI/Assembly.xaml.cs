using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.Tools;
using System.Windows.Controls;
using System.Data;
using System.Diagnostics;
using SequenceAssemblerLogic;
using System.IO;
using Microsoft.Win32;
using SequenceAssemblerGUI;
using static SequenceAssemblerGUI.Assembly;


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

        public class ReferenceGroupViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<SequencesViewModel> _seq;
            private ObservableCollection<DataTableAlign> _alignmetns;
            private ObservableCollection<VisualAlignment> _referenceSequence;
            private ObservableCollection<ConsensusChar> _consensusSequence;
            private double _coverage;

            public string ReferenceHeader { get; set; }
            public string ID { get; set; }
            public string Description { get; set; }
            public ObservableCollection<SequencesViewModel> Seq
            {
                get => _seq;
                set
                {
                    _seq = value;
                    OnPropertyChanged();
                }
            }

            public ObservableCollection<DataTableAlign> Alignments
            {
                get => _alignmetns;
                set
                {
                    _alignmetns = value;
                    OnPropertyChanged();
                }
            }
            public ObservableCollection<VisualAlignment> ReferenceSequence
            {
                get => _referenceSequence;
                set
                {
                    _referenceSequence = value;
                    OnPropertyChanged();
                }
            }
            public ObservableCollection<ConsensusChar> ConsensusSequence
            {
                get => _consensusSequence;
                set
                {
                    _consensusSequence = value;
                    OnPropertyChanged();
                }
            }
            public double Coverage
            {
                get => _coverage;
                set
                {
                    _coverage = value;
                    OnPropertyChanged();
                }
            }

            public string DisplayHeader => $"{ReferenceHeader} (Coverage: {Coverage:F2}%)";

            public ReferenceGroupViewModel()
            {
                Alignments = new ObservableCollection<DataTableAlign>();
                Seq = new ObservableCollection<SequencesViewModel>();
                ReferenceSequence = new ObservableCollection<VisualAlignment>();
                ConsensusSequence = new ObservableCollection<ConsensusChar>();
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }



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
            public SolidColorBrush OriginalBackgroundColor { get; set; }

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

        public class SequencesViewModel : INotifyPropertyChanged
        {

            public ObservableCollection<VisualAlignment> VisualAlignment { get; set; } = new ObservableCollection<VisualAlignment>();
            public string ToolTipContent { get; internal set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }

        public class DataTableAlign : INotifyPropertyChanged
        {
            private string _startPositions;
            private int _identity;
            private double _normalizedIdentityScore;
            private int _similarityScore;
            private double _normalizedSimilarity;
            private int _alignedAA;
            private double _normalizedAlignedAA;
            private int _gapsUsed;
            private string _alignedLargeSequence;
            private string _alignedSmallSequence;
            private string _toolTipContent;
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

            public string StartPositions
            {
                get { return _startPositions; }
                set
                {
                    if (_startPositions != value)
                    {
                        _startPositions = value;
                        OnPropertyChanged();
                    }
                }
            }

            public int Identity
            {
                get { return _identity; }
                set
                {
                    if (_identity != value)
                    {
                        _identity = value;
                        OnPropertyChanged();
                    }
                }
            }

            public double NormalizedIdentityScore
            {
                get { return _normalizedIdentityScore; }
                set
                {
                    if (_normalizedIdentityScore != value)
                    {
                        _normalizedIdentityScore = value;
                        OnPropertyChanged();
                    }
                }
            }

            public int SimilarityScore
            {
                get { return _similarityScore; }
                set
                {
                    if (_similarityScore != value)
                    {
                        _similarityScore = value;
                        OnPropertyChanged();
                    }
                }
            }

            public double NormalizedSimilarity
            {
                get { return _normalizedSimilarity; }
                set
                {
                    if (_normalizedSimilarity != value)
                    {
                        _normalizedSimilarity = value;
                        OnPropertyChanged();
                    }
                }
            }

            public int AlignedAA
            {
                get { return _alignedAA; }
                set
                {
                    if (_alignedAA != value)
                    {
                        _alignedAA = value;
                        OnPropertyChanged();
                    }
                }
            }

            public double NormalizedAlignedAA
            {
                get { return _normalizedAlignedAA; }
                set
                {
                    if (_normalizedAlignedAA != value)
                    {
                        _normalizedAlignedAA = value;
                        OnPropertyChanged();
                    }
                }
            }

            public int GapsUsed
            {
                get { return _gapsUsed; }
                set
                {
                    if (_gapsUsed != value)
                    {
                        _gapsUsed = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string AlignedLargeSequence
            {
                get { return _alignedLargeSequence; }
                set
                {
                    if (_alignedLargeSequence != value)
                    {
                        _alignedLargeSequence = value;
                        OnPropertyChanged();
                    }
                }
            }

            public string AlignedSmallSequence
            {
                get { return _alignedSmallSequence; }
                set
                {
                    if (_alignedSmallSequence != value)
                    {
                        _alignedSmallSequence = value;
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

            public Dictionary<string, (List<ConsensusChar>, double)> ConsensusAndCoverage { get; set; }

            public ObservableCollection<ReferenceGroupViewModel> ReferenceGroups { get; set; } = new();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            public void UpdateConsensusColoring()
            {
                if (ReferenceGroups == null) return;

                foreach (var group in ReferenceGroups)
                {
                    if (group.ConsensusSequence == null || group.ConsensusSequence.Count == 0) continue;

                    for (int i = 0; i < group.ConsensusSequence.Count; i++)
                    {
                        var consensusChar = group.ConsensusSequence[i];
                        bool hasILinReference = false;
                        bool hasILinAligned = false;

                        //Checks if the reference string has 'I' or 'L' at this position
                        if (i < group.ReferenceSequence.Count)
                        {
                            var refChar = group.ReferenceSequence[i];
                            if (refChar.Letra == "I" || refChar.Letra == "L")
                            {
                                hasILinReference = true;
                            }
                        }

                        //Checks if any aligned string has 'I' or 'L' at this position
                        foreach (var seq in group.Seq)
                        {
                            if (i < seq.VisualAlignment.Count)
                            {
                                var alignmentChar = seq.VisualAlignment[i];
                                if (alignmentChar.Letra == "I" || alignmentChar.Letra == "L")
                                {
                                    hasILinAligned = true;
                                    break;
                                }
                            }
                        }

                        //Updates the background color of the consensus character based on the presence of 'I' or 'L'
                        if (ColorIL && hasILinReference && hasILinAligned)
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
        }


        public void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<Alignment> sequencesToAlign, List<(string ID, string Description, string Sequence)> referenceSequences)
        {
            foreach (var (id, description, referenceSequence) in referenceSequences)
            {
                var groupViewModel = new ReferenceGroupViewModel
                {
                    ReferenceHeader = $"{id} - {description}",
                    ID = id,
                    Description = description
                };

                foreach (char letra in referenceSequence)
                {
                    groupViewModel.ReferenceSequence.Add(new VisualAlignment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
                }

                var sortedSequences = sequencesToAlign.OrderBy(seq => seq.StartPositions.Min()).ToList();
                Dictionary<int, int> rowEndPositions = new Dictionary<int, int>();

                foreach (var sequence in sortedSequences)
                {
                    string sequenceId = $"ID {sequence.ID}";

                    var sequenceViewModel = new SequencesViewModel
                    {
                        ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - Source: {sequence.SourceOrigin}"
                    };
                    var dataTableViewModel = new DataTableAlign
                    {
                        ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - Source: {sequence.SourceOrigin}",
                        StartPositions = string.Join(",", sequence.StartPositions),
                        Identity = sequence.Identity,
                        NormalizedIdentityScore = sequence.NormalizedIdentityScore,
                        SimilarityScore = sequence.SimilarityScore,
                        NormalizedSimilarity = sequence.NormalizedSimilarity,
                        AlignedAA = sequence.AlignedAA,
                        NormalizedAlignedAA = sequence.NormalizedAlignedAA,
                        GapsUsed = sequence.GapsUsed,
                        AlignedLargeSequence = sequence.AlignedLargeSequence,
                        AlignedSmallSequence = sequence.AlignedSmallSequence
                    };

                    groupViewModel.Alignments.Add(dataTableViewModel);
                    int startPosition = sequence.StartPositions.Min() - 1;
                    int rowIndex = FindAvailableRow(rowEndPositions, startPosition, sequence.AlignedSmallSequence.Length);

                    for (int i = 0; i < startPosition; i++)
                    {
                        if (i >= rowEndPositions[rowIndex])
                        {
                            sequenceViewModel.VisualAlignment.Add(new VisualAlignment { Letra = " ", CorDeFundo = Brushes.LightGray });
                        }
                    }

                    rowEndPositions[rowIndex] = startPosition + sequence.AlignedSmallSequence.Length;

                    foreach (char seqChar in sequence.AlignedSmallSequence)
                    {
                        Brush corDeFundo;
                        int refIndex = startPosition++;
                        string letra;

                        if (seqChar == '-')
                        {
                            corDeFundo = Brushes.LightGray;
                            letra = " ";
                        }
                        else if (refIndex < referenceSequence.Length)
                        {
                            corDeFundo = seqChar == referenceSequence[refIndex] ? Brushes.LightGreen : Brushes.LightCoral;
                            letra = seqChar.ToString();
                        }
                        else
                        {
                            corDeFundo = Brushes.LightGray;
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

                    while (groupViewModel.Seq.Count <= rowIndex)
                    {
                        groupViewModel.Seq.Add(new SequencesViewModel());
                    }
                    foreach (var item in sequenceViewModel.VisualAlignment)
                    {
                        groupViewModel.Seq[rowIndex].VisualAlignment.Add(item);
                    }
                }

                foreach (var sequenceViewModel in groupViewModel.Seq)
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

                //Calculate and add consensus and individual coverage
                var (consensusChars, totalCoverage) = BuildConsensus(sequencesToAlign, referenceSequence);
                groupViewModel.ConsensusSequence = new ObservableCollection<ConsensusChar>(consensusChars);

                //Add coverage to reference group
                groupViewModel.Coverage = totalCoverage;
                viewModel.ReferenceGroups.Add(groupViewModel);
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


        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAssembly();
        }

        public void UpdateViewModel(List<Fasta> allFastaSequences, List<Alignment> alignments)
        {
            if (DataContext is SequenceViewModel viewModel)
            {
                Console.WriteLine("Updating ViewModel with fasta sequences and alignments.");
                viewModel.ReferenceGroups.Clear();

                foreach (var fasta in allFastaSequences)
                {
                    //Select alignments that have the TargetOrigin equal to the fasta sequence ID
                    var sequencesToAlign = alignments.Where(a => a.TargetOrigin == fasta.ID).ToList();
                    Console.WriteLine($"Fasta ID: {fasta.ID}, Description: {fasta.Description}, Alignments to process: {sequencesToAlign.Count}");

                    //Checks for alignments for the current fasta sequence
                    if (!sequencesToAlign.Any())
                    {
                        Console.WriteLine($"No alignments found for fasta ID: {fasta.ID}");
                        continue; //Ignore fasta sequences without alignments
                    }

                    //Eliminate duplicates and subsequences
                    var filteredSequencesToAlign = Utils.EliminateDuplicatesAndSubsequences(sequencesToAlign);

                    //Updates the interface with the alignments and assembly
                    UpdateUIWithAlignmentAndAssembly(viewModel, filteredSequencesToAlign, new List<(string ID, string Description, string Sequence)>
                    {
                        (fasta.ID, fasta.Description, fasta.Sequence)
                    });
                }
            }
            else
            {
                Console.WriteLine("DataContext is not of type SequenceViewModel.");
            }
        }



        public void ExecuteAssembly()
        {
            if (!(DataContext is SequenceViewModel viewModel))
            {
                MessageBox.Show("Failed to get the data context.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            viewModel.UpdateConsensusColoring();

            Console.WriteLine("Assembly executed successfully.");
        }

        public (List<ConsensusChar>, double) BuildConsensus(List<Alignment> sequencesToAlign, string referenceSequence)
        {
            if (sequencesToAlign == null || !sequencesToAlign.Any())
            {
                throw new InvalidOperationException("The list of sequences to align is empty.");
            }

            Console.WriteLine($"Building consensus for {sequencesToAlign.Count} sequences.");

            int maxLength = Math.Max(
                sequencesToAlign
                    .Where(seq => seq.StartPositions != null && seq.StartPositions.Any())
                    .Max(seq => seq.StartPositions.Min() - 1 + seq.AlignedSmallSequence.Length),
                referenceSequence.Length);

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
                    if (seq.StartPositions == null || !seq.StartPositions.Any())
                    {
                        continue;
                    }

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

            return (consensusSequence, overallCoverage);
        }

        //var mainWindow = Application.Current.MainWindow as MainWindow;
        //    if (mainWindow != null)
        //    {
        //        mainWindow.LabelCoverage.Content = $"{overallCoverage:F2}%";
        //    }

        //    return (consensusSequence, overallCoverage);
        //}


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

        //private void DownloadConsensusButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var viewModel = (SequenceViewModel)DataContext;

        //    if (viewModel.ConsensusSequence == null || !viewModel.ConsensusSequence.Any())
        //    {
        //        MessageBox.Show("No consensus sequence available. Please generate it before attempting to download.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    SaveConsensusSequenceToFile(viewModel.ConsensusSequence);
        //}

        //private void SaveConsensusSequenceToFile(ObservableCollection<ConsensusChar> consensusSequence)
        //{
        //    var dialog = new SaveFileDialog()
        //    {
        //        Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
        //        FileName = "ConsensusSequence.txt"
        //    };

        //    if (dialog.ShowDialog() == true)
        //    {
        //        using (var writer = new StreamWriter(dialog.FileName))
        //        {
        //            foreach (var item in consensusSequence)
        //            {
        //                writer.Write(item.Char);
        //            }
        //        }
        //        MessageBox.Show($"Consensus sequence saved to {dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}
    }

}
