using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SequenceAssemblerLogic;
using SequenceAssemblerLogic.AssemblyTools;
using SequenceAssemblerLogic.ProteinAlignmentCode;
using SequenceAssemblerLogic.Tools;

namespace SequenceAssemblerGUI
{
    public partial class AlignmentViewer : UserControl
    {
        private bool colorIL = true;
        private List<(string ID, string Sequence, string Description)> currentAlignedSequences;
        private List<Alignment> currentAlignments;

        public void OnColorILChecked(object sender, RoutedEventArgs e)
        {
            colorIL = true;
            if (currentAlignedSequences != null && currentAlignments != null)
            {
                UpdateUIWithMSAAlignmentAndAssembly(currentAlignedSequences, currentAlignments);
            }
        }

        public void OnColorILUnchecked(object sender, RoutedEventArgs e)
        {
            colorIL = false;
            if (currentAlignedSequences != null && currentAlignments != null)
            {
                UpdateUIWithMSAAlignmentAndAssembly(currentAlignedSequences, currentAlignments);
            }
        }

        public AlignmentViewer()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel();
        }

        public class ReferenceGroupViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<SequencesViewModel> _seq;
            private ObservableCollection<DataTableAlign> _alignmetns;
            private ObservableCollection<VisualAlignment> _referenceSequence;

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

            public ReferenceGroupViewModel()
            {
                Alignments = new ObservableCollection<DataTableAlign>();
                Seq = new ObservableCollection<SequencesViewModel>();
                ReferenceSequence = new ObservableCollection<VisualAlignment>();
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
            private double _identity;
            private double _normalizedIdentityScore;
            private double _similarityScore;
            private double _normalizedSimilarity;
            private double _alignedAA;
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
            public double Identity
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

            public double SimilarityScore
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

            public double AlignedAA
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
            public ObservableCollection<DataTableAlign> GlobalAlignments { get; set; } = new ObservableCollection<DataTableAlign>();

            public ObservableCollection<ReferenceGroupViewModel> ReferenceGroups { get; set; } = new();
            public ObservableCollection<SequencesViewModel> AllAlignments { get; set; } = new ObservableCollection<SequencesViewModel>();
            public ObservableCollection<VisualAlignment> ConsensusSequence { get; set; } = new ObservableCollection<VisualAlignment>();

            public void UpdateAllAlignments()
            {
                AllAlignments.Clear();
                foreach (var group in ReferenceGroups)
                {
                    foreach (var alignment in group.Seq)
                    {
                        AllAlignments.Add(alignment);
                    }
                }
            }
            private string _coverage;
            public string Coverage
            {
                get { return _coverage; }
                set
                {
                    _coverage = value;
                    OnPropertyChanged(nameof(Coverage));
                }
            }
            public void RefreshData()
            {
                UpdateAllAlignments();
                OnPropertyChanged(nameof(ConsensusSequence));
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void UpdateUIWithMSAAlignmentAndAssembly(List<(string ID, string Sequence, string Description)> alignedSequences, List<Alignment> alignments)
        {
            if (alignedSequences == null || alignments == null)
            {
                throw new ArgumentNullException("alignedSequences or alignments", "Aligned sequences and alignments cannot be null.");
            }

            currentAlignedSequences = alignedSequences;
            currentAlignments = alignments;

            if (DataContext is SequenceViewModel viewModel)
            {
                viewModel.ReferenceGroups.Clear();
                viewModel.GlobalAlignments.Clear();

                Brush correctAlignmentColor = Brushes.LightGreen;
                Brush incorrectAlignmentColor = Brushes.LightCoral;
                Brush consensusAdditionColor = Brushes.LightGreen;
                Brush gapColor = Brushes.White;
                Brush differentConsensusColor = Brushes.Orange;

                double coverage = AssemblyParameters.CalculateCoverage(alignedSequences, alignments);
                viewModel.Coverage = $"{coverage:F2}%";

                var sortedSequences = alignments.OrderBy(seq => seq.StartPositions.Max()).ToList();
                var processedAlignments = new HashSet<string>();
                int maxLength = alignedSequences.Max(s => s.Sequence.Length);

                Dictionary<int, List<char>> positionChars = new Dictionary<int, List<char>>();

                foreach (var fasta in alignedSequences)
                {
                    var groupViewModel = new ReferenceGroupViewModel
                    {
                        ReferenceHeader = $"{fasta.ID} - {fasta.Description}",
                    };

                    for (int i = 0; i < fasta.Sequence.Length; i++)
                    {
                        char letter = fasta.Sequence[i];
                        groupViewModel.ReferenceSequence.Add(new VisualAlignment
                        {
                            Letra = letter.ToString(),
                            CorDeFundo = Brushes.WhiteSmoke,
                            ToolTipContent = $"Position: {i + 1}, Letter: {letter}, ID: {fasta.ID}, Description: {fasta.Description}"
                        });
                    }

                    int refLength = groupViewModel.ReferenceSequence.Count;
                    if (refLength < maxLength)
                    {
                        for (int i = refLength; i < maxLength; i++)
                        {
                            groupViewModel.ReferenceSequence.Add(new VisualAlignment
                            {
                                Letra = " ",
                                CorDeFundo = Brushes.WhiteSmoke,
                                ToolTipContent = $"Position: {i + 1}, Letter: , ID: {fasta.ID}, Description: {fasta.Description}"
                            });
                        }
                    }

                    Dictionary<int, int> rowEndPositions = new Dictionary<int, int>();

                    foreach (var sequence in sortedSequences.Where(s => s.TargetOrigin == fasta.ID))
                    {
                        var alignmentKey = $"{sequence.SourceOrigin}-{sequence.StartPositions.Max()}";
                        if (processedAlignments.Contains(alignmentKey))
                        {
                            continue;
                        }

                        processedAlignments.Add(alignmentKey);

                        var sequenceViewModel = new SequencesViewModel
                        {
                            ToolTipContent = $"Start Position: {sequence.StartPositions.Max()} - Source: {sequence.SourceOrigin}"
                        };

                        var dataTableViewModel = new DataTableAlign
                        {
                            ToolTipContent = $"Start Position: {sequence.StartPositions.Max()} - Source: {sequence.SourceOrigin}",
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

                        viewModel.GlobalAlignments.Add(dataTableViewModel);

                        int startPosition = sequence.StartPositions.Max();
                        int rowIndex = AssemblyParameters.FindAvailableRow(rowEndPositions, startPosition, sequence.AlignedSmallSequence.Length);

                        if (!rowEndPositions.ContainsKey(rowIndex))
                        {
                            rowEndPositions[rowIndex] = 0;
                        }

                        if (rowIndex == 0 && rowEndPositions[rowIndex] > 0)
                        {
                            rowIndex = AssemblyParameters.FindNextAvailableRow(rowEndPositions, startPosition, sequence.AlignedSmallSequence.Length);
                        }

                        for (int i = rowEndPositions[rowIndex]; i < startPosition; i++)
                        {
                            sequenceViewModel.VisualAlignment.Add(new VisualAlignment
                            {
                                Letra = " ",
                                CorDeFundo = Brushes.WhiteSmoke,
                                ToolTipContent = $"Position: {i + 1}, Letter: , ID: {sequence.SourceOrigin}"
                            });
                        }

                        rowEndPositions[rowIndex] = startPosition + sequence.AlignedSmallSequence.Length;

                        for (int i = 0; i < sequence.AlignedSmallSequence.Length; i++)
                        {
                            char seqChar = sequence.AlignedSmallSequence[i];
                            Brush backgroundColor;
                            string letter = seqChar.ToString();
                            int refIndex = startPosition++;

                            if (!positionChars.ContainsKey(refIndex))
                            {
                                positionChars[refIndex] = new List<char>();
                            }
                            positionChars[refIndex].Add(seqChar);

                            if (refIndex < fasta.Sequence.Length)
                            {
                                char refChar = fasta.Sequence[refIndex];
                                if (seqChar == '-')
                                {
                                    backgroundColor = gapColor;
                                }
                                else if (seqChar == refChar || alignedSequences.Any(a => a.Sequence.Length > refIndex && a.Sequence[refIndex] == seqChar))
                                {
                                    backgroundColor = correctAlignmentColor;
                                }
                                else
                                {
                                    backgroundColor = incorrectAlignmentColor;
                                    if (colorIL && (positionChars[refIndex].All(c => c == 'I' || c == 'L')))
                                    {
                                        backgroundColor = Brushes.LightGreen; // Color 'I' or 'L' green if all chars at this position are 'I' or 'L'
                                    }
                                }
                            }
                            else
                            {
                                backgroundColor = Brushes.WhiteSmoke;
                            }

                            sequenceViewModel.VisualAlignment.Add(new VisualAlignment
                            {
                                Letra = letter,
                                CorDeFundo = backgroundColor,
                                ToolTipContent = $"Position: {refIndex + 1}, Letter: {letter}, ID: {sequence.SourceOrigin}"
                            });
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
                        if (currentLength < maxLength)
                        {
                            for (int i = currentLength; i < maxLength; i++)
                            {
                                sequenceViewModel.VisualAlignment.Add(new VisualAlignment
                                {
                                    Letra = " ",
                                    CorDeFundo = Brushes.WhiteSmoke,
                                    ToolTipContent = $"Position: {i + 1}, Letter: , ID: {fasta.ID}"
                                });
                            }
                        }
                    }

                    if (groupViewModel.ReferenceSequence.Any() || groupViewModel.Alignments.Any())
                    {
                        viewModel.ReferenceGroups.Add(groupViewModel);
                    }
                }

                var consensusDetails = new List<(int Position, char ConsensusChar, bool IsConsensus, bool IsDifferent)>();
                var consensusSequence = AssemblyParameters.CalculateConsensusSequence(alignedSequences, alignments, out consensusDetails);
                viewModel.ConsensusSequence.Clear();

                foreach (var detail in consensusDetails)
                {
                    char consensusChar = detail.ConsensusChar == '-' ? 'X' : detail.ConsensusChar;
                    Brush backgroundColor = consensusChar == 'X' ? Brushes.White : consensusAdditionColor;
                    if (detail.IsDifferent)
                    {
                        backgroundColor = differentConsensusColor;
                        if (colorIL && positionChars.ContainsKey(detail.Position) && positionChars[detail.Position].All(c => c == 'I' || c == 'L'))
                        {
                            backgroundColor = Brushes.LightGreen; // Apply green color to 'I' or 'L' in the consensus if all chars at this position are 'I' or 'L'
                        }
                    }

                    viewModel.ConsensusSequence.Add(new VisualAlignment
                    {
                        Letra = consensusChar.ToString(),
                        CorDeFundo = backgroundColor,
                        ToolTipContent = $"Position: {detail.Position + 1}, Letter: {consensusChar}, IsConsensus: {detail.IsConsensus}, IsDifferent: {detail.IsDifferent}"
                    });
                }

                viewModel.RefreshData();
            }
        }




        public void UpdateViewMultipleModel(List<(string ID, string Sequence, string Description)> alignedSequences, List<Alignment> alignments)
        {
            if (DataContext is SequenceViewModel viewModel)
            {
                Console.WriteLine("Updating ViewModel with fasta sequences and alignments.");
                viewModel.ReferenceGroups.Clear();

                foreach (var fasta in alignedSequences)
                {
                    // Select alignments that have the TargetOrigin equal to the fasta sequence ID
                    var sequencesToAlign = alignments.Where(a => a.TargetOrigin == fasta.ID).ToList();
                    Console.WriteLine($"Fasta ID: {fasta.ID}, Sequence: {fasta.Sequence}, Description: {fasta.Description}, Alignments to process: {sequencesToAlign.Count}");

                    // Checks for alignments for the current fasta sequence
                    if (!sequencesToAlign.Any())
                    {
                        Console.WriteLine($"No alignments found for fasta ID: {fasta.ID}");
                        continue; // Ignore fasta sequences without alignments
                    }

                    // Eliminate duplicates and subsequences
                    var filteredSequencesToAlign = Utils.EliminateDuplicatesAndSubsequences(sequencesToAlign);

                    // Updates the interface with the alignments and assembly
                    UpdateUIWithMSAAlignmentAndAssembly(alignedSequences, filteredSequencesToAlign);
                }
            }
            else
            {
                Console.WriteLine("DataContext is not of type SequenceViewModel.");
            }
        }




        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAssembly();
        }

        public void ExecuteAssembly()
        {
            if (!(DataContext is SequenceViewModel viewModel))
            {
                MessageBox.Show("Failed to get the data context.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Console.WriteLine("Assembly executed successfully.");
        }


        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }


    }

}