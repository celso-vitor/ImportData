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

// Namespace declaration for the user’s GUI project.
namespace SequenceAssemblerGUI
{
    // Partial class definition for the AlignmentViewer, which is a UserControl in WPF.
    public partial class AlignmentViewer : UserControl
    {
        // Private field to store the state of the checkbox that determines whether 'I' and 'L' are colored.
        private bool colorIL = true;

        // Lists to store the current aligned sequences and alignments being worked on.
        private List<(string ID, string Sequence, string Description)> currentAlignedSequences;
        private List<Alignment> currentAlignments;

        // Event handler method that gets triggered when the 'ColorIL' checkbox is checked.
        public void OnColorILChecked(object sender, RoutedEventArgs e)
        {
            colorIL = true;
            // Updates the UI with the current sequences and alignments if they are not null.
            if (currentAlignedSequences != null && currentAlignments != null)
            {
                UpdateUIWithMSAAlignmentAndAssembly(currentAlignedSequences, currentAlignments);
            }
        }

        // Event handler method that gets triggered when the 'ColorIL' checkbox is unchecked.
        public void OnColorILUnchecked(object sender, RoutedEventArgs e)
        {
            colorIL = false;
            // Updates the UI with the current sequences and alignments if they are not null.
            if (currentAlignedSequences != null && currentAlignments != null)
            {
                UpdateUIWithMSAAlignmentAndAssembly(currentAlignedSequences, currentAlignments);
            }
        }

        // Constructor for the AlignmentViewer class. Initializes the component and sets the DataContext to a new SequenceViewModel instance.
        public AlignmentViewer()
        {
            InitializeComponent();
            DataContext = new SequenceViewModel();
        }

        // Inner class representing the view model for a reference group, implementing INotifyPropertyChanged to support data binding.
        public class ReferenceGroupViewModel : INotifyPropertyChanged
        {
            // Private fields for various properties related to sequences, alignments, and reference sequences.
            private ObservableCollection<SequencesViewModel> _seq;
            private ObservableCollection<DataTableAlign> _alignmetns;
            private ObservableCollection<VisualAlignment> _referenceSequence;

            // Public properties for reference header, ID, description, sequences, alignments, and reference sequences.
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

            // Constructor initializing the collections for alignments, sequences, and reference sequences.
            public ReferenceGroupViewModel()
            {
                Alignments = new ObservableCollection<DataTableAlign>();
                Seq = new ObservableCollection<SequencesViewModel>();
                ReferenceSequence = new ObservableCollection<VisualAlignment>();
            }

            // Event for property change notification, necessary for data binding in WPF.
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Inner class representing the visual alignment of a sequence, implementing INotifyPropertyChanged for data binding.
        public class VisualAlignment : INotifyPropertyChanged
        {
            // Private fields for the character and background color of the alignment.
            private string _letra;
            private Brush _corDeFundo;

            // Public properties for the letter and background color.
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

            // Property for the tooltip content associated with the alignment.
            public string ToolTipContent { get; internal set; }

            // Event for property change notification.
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Inner class representing the view model for sequences, also implementing INotifyPropertyChanged.
        public class SequencesViewModel : INotifyPropertyChanged
        {
            // Collection for visual alignment and property for tooltip content.
            public ObservableCollection<VisualAlignment> VisualAlignment { get; set; } = new ObservableCollection<VisualAlignment>();
            public string ToolTipContent { get; internal set; }

            // Event for property change notification.
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Inner class representing the data table alignment, also implementing INotifyPropertyChanged.
        public class DataTableAlign : INotifyPropertyChanged
        {
            // Private fields for various properties related to alignment data.
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

            // Properties for tooltips, start positions, identity, similarity, aligned sequences, and gaps.
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

            // Event for property change notification.
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Main view model class for sequences, implementing INotifyPropertyChanged to support data binding.
        public class SequenceViewModel : INotifyPropertyChanged
        {
            // Collections for global alignments, reference groups, all alignments, and consensus sequences.
            public ObservableCollection<DataTableAlign> GlobalAlignments { get; set; } = new ObservableCollection<DataTableAlign>();
            public ObservableCollection<ReferenceGroupViewModel> ReferenceGroups { get; set; } = new();
            public ObservableCollection<SequencesViewModel> AllAlignments { get; set; } = new ObservableCollection<SequencesViewModel>();
            public ObservableCollection<VisualAlignment> ConsensusSequence { get; set; } = new ObservableCollection<VisualAlignment>();

            // Method to update all alignments by clearing and re-adding sequences from reference groups.
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

            // Private field and public property for coverage.
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

            // Method to refresh data by updating alignments and notifying property changes.
            public void RefreshData()
            {
                UpdateAllAlignments();
                OnPropertyChanged(nameof(ConsensusSequence));
            }

            // Event for property change notification.
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        // Method to update the UI with multiple sequence alignments and assembly details.
        public void UpdateUIWithMSAAlignmentAndAssembly(List<(string ID, string Sequence, string Description)> alignedSequences, List<Alignment> alignments)
        {
            // Null checks for the sequences and alignments.
            if (alignedSequences == null || alignments == null)
            {
                throw new ArgumentNullException("alignedSequences or alignments", "Aligned sequences and alignments cannot be null.");
            }

            // Storing the current sequences and alignments.
            currentAlignedSequences = alignedSequences;
            currentAlignments = alignments;

            // Main logic for updating the UI based on the sequences and alignments.
            if (DataContext is SequenceViewModel viewModel)
            {
                // Clearing the existing reference groups and global alignments.
                viewModel.ReferenceGroups.Clear();
                viewModel.GlobalAlignments.Clear();

                // Defining colors for various alignment scenarios.
                Brush correctAlignmentColor = Brushes.LightGreen;
                Brush incorrectAlignmentColor = Brushes.LightCoral;
                Brush consensusAdditionColor = Brushes.LightGreen;
                Brush gapColor = Brushes.White;
                Brush differentConsensusColor = Brushes.Orange;

                // Calculating the coverage of the sequences.
                double coverage = AssemblyParameters.CalculateCoverage(alignedSequences, alignments);
                viewModel.Coverage = $"{coverage:F2}%";

                // Sorting sequences based on their starting positions.
                var sortedSequences = alignments.OrderBy(seq => seq.StartPositions.Max()).ToList();
                var processedAlignments = new HashSet<string>();
                int maxLength = alignedSequences.Max(s => s.Sequence.Length);

                // Dictionary to store characters at each position for consensus calculation.
                Dictionary<int, List<char>> positionChars = new Dictionary<int, List<char>>();

                // Indicator for the first entry in the log file.
                bool isFirstEntry = true;

                foreach (var fasta in alignedSequences)
                {
                    // Creating a new ReferenceGroupViewModel for each fasta sequence.
                    var groupViewModel = new ReferenceGroupViewModel
                    {
                        ReferenceHeader = $"{fasta.ID} - {fasta.Description}",
                    };

                    // Adding the sequence characters to the visual alignment list with default background color.
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

                    // Padding the sequence with spaces if it is shorter than the maximum length.
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

                    // Dictionary to keep track of end positions in rows.
                    Dictionary<int, int> rowEndPositions = new Dictionary<int, int>();

                    // Processing each sequence that targets the current fasta sequence.
                    foreach (var sequence in sortedSequences.Where(s => s.TargetOrigin == fasta.ID))
                    {
                        // Creating a unique key for each alignment based on source origin and starting position.
                        var alignmentKey = $"{sequence.SourceOrigin}-{sequence.StartPositions.Max()}";
                        if (processedAlignments.Contains(alignmentKey))
                        {
                            continue;
                        }

                        // Marking the alignment as processed.
                        processedAlignments.Add(alignmentKey);

                        // Creating view models for the sequence and data table alignment.
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

                        // Adding the data table alignment to the global alignments.
                        viewModel.GlobalAlignments.Add(dataTableViewModel);

                        // Finding the appropriate row index for the sequence.
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

                        // Adding spaces to align the sequence with the reference.
                        for (int i = rowEndPositions[rowIndex]; i < startPosition; i++)
                        {
                            sequenceViewModel.VisualAlignment.Add(new VisualAlignment
                            {
                                Letra = " ",
                                CorDeFundo = Brushes.WhiteSmoke,
                                ToolTipContent = $"Position: {i + 1}, Letter: , ID: {sequence.SourceOrigin}"
                            });
                        }

                        // Updating the row end position.
                        rowEndPositions[rowIndex] = startPosition + sequence.AlignedSmallSequence.Length;

                        // Adding the aligned sequence characters with appropriate background colors.
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

                        // Ensuring the sequence is added to the correct row.
                        while (groupViewModel.Seq.Count <= rowIndex)
                        {
                            groupViewModel.Seq.Add(new SequencesViewModel());
                        }

                        // Adding the sequence visual alignment to the group.
                        foreach (var item in sequenceViewModel.VisualAlignment)
                        {
                            groupViewModel.Seq[rowIndex].VisualAlignment.Add(item);
                        }
                    }

                    // Padding the sequences to the maximum length with spaces.
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

                    // Adding the reference group to the view model if it contains sequences or alignments.
                    if (groupViewModel.ReferenceSequence.Any() || groupViewModel.Alignments.Any())
                    {
                        viewModel.ReferenceGroups.Add(groupViewModel);
                    }
                }

                // Calculating and updating the consensus sequence.
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

                // Saving the consensus and reference sequences to a file for MSA logging.
                AssemblyParameters.SaveMSALogToFile(alignedSequences, consensusSequence.Select(c => c).ToList(), "MSA Consensus", "Multiple Sequence Alignment", isFirstEntry);
                isFirstEntry = false;

                // Refreshing the data in the view model.
                viewModel.RefreshData();
            }
        }

        // Method to update the view with multiple model sequences and alignments.
        public void UpdateViewMultipleModel(List<(string ID, string Sequence, string Description)> alignedSequences, List<Alignment> alignments)
        {
            if (DataContext is SequenceViewModel viewModel)
            {
                viewModel.ReferenceGroups.Clear();

                foreach (var fasta in alignedSequences)
                {
                    // Select alignments that have the TargetOrigin equal to the fasta sequence ID.
                    var sequencesToAlign = alignments.Where(a => a.TargetOrigin == fasta.ID).ToList();

                    // Checks for alignments for the current fasta sequence.
                    if (!sequencesToAlign.Any())
                    {
                        Console.WriteLine($"No alignments found for fasta ID: {fasta.ID}");
                        continue;
                    }

                    // Eliminate duplicates and subsequences.
                    var filteredSequencesToAlign = Utils.EliminateDuplicatesAndSubsequences(sequencesToAlign);

                    // Updates the interface with the alignments and assembly.
                    UpdateUIWithMSAAlignmentAndAssembly(alignedSequences, filteredSequencesToAlign);
                }
            }
            else
            {
                Console.WriteLine("DataContext is not of type SequenceViewModel.");
            }
        }

        // Event handler for the 'Compare' button click event.
        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteMultipleAssembly();
        }

        // Method to execute multiple sequence assemblies.
        public void ExecuteMultipleAssembly()
        {
            if (!(DataContext is SequenceViewModel viewModel))
            {
                // Displays an error message if the DataContext is not of type SequenceViewModel.
                MessageBox.Show("Failed to get the data context.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        // Event handler for loading rows in the DataGrid of alignments, setting the row header with the row index.
        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}