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
using System.IO;
using Microsoft.Win32;
using SequenceAssemblerGUI;
using static SequenceAssemblerGUI.Assembly;
using System.Text;
using System.Windows.Input;
using System.Windows.Controls.Primitives;


namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {
        public ObservableCollection<IntervalDomain> IntervalDomains { get; set; }
        public class IntervalDomain
        {
            public int Start { get; set; }
            public int End { get; set; }
            public string Description { get; set; }
            public string ConsensusFragment { get; set; }
        }

        public Assembly()
        {
            InitializeComponent();

            IntervalDomains = new ObservableCollection<IntervalDomain>();

            var viewModel = new SequenceViewModel
            {
                ColorIL = true // Set ColorIL to true initially
            };

            DataContext = viewModel;

            // Bind the DataGrid to IntervalDomains
            // Find the DataGrid by its name and set its ItemsSource
            var intervalDataGrid = (DataGrid)FindName("IntervalsDataGrid");
            if (intervalDataGrid != null)
            {
                intervalDataGrid.ItemsSource = IntervalDomains;
            }
        }

        // Method to open the Insert Range Popup
        private void OnInsertRangeClick(object sender, RoutedEventArgs e)
        {
            var popup = (Popup)FindName("RangePopup");
            if (popup != null)
            {
                popup.IsOpen = true;
            }
        }

        private void OnConfirmRangeClick(object sender, RoutedEventArgs e)
        {
            var popup = (Popup)FindName("RangePopup");
            var startValueBox = (TextBox)FindName("StartValueBox");
            var endValueBox = (TextBox)FindName("EndValueBox");
            var descriptionValueBox = (TextBox)FindName("DescriptionValueBox");

            if (startValueBox != null && endValueBox != null && descriptionValueBox != null && popup != null)
            {
                if (int.TryParse(startValueBox.Text, out int start) && int.TryParse(endValueBox.Text, out int end) && end >= start)
                {
                    string description = descriptionValueBox.Text;

                    // Variable to store the consensus sequence fragment
                    string consensusFragment = string.Empty;

                    // Retrieve the consensus sequence and add the range to the collection
                    if (DataContext is SequenceViewModel viewModel)
                    {
                        foreach (var groupViewModel in viewModel.ReferenceGroups)
                        {
                            // Construct the consensus sequence from ConsensusSequence
                            string consensusSequence = new string(groupViewModel.ConsensusSequence.Select(c => c.Char[0]).ToArray());

                            // Extract the fragment corresponding to the defined range
                            consensusFragment = consensusSequence.Substring(start, end - start);

                            Console.WriteLine(consensusFragment);

                            // Update the range squares to reflect the new range
                            UpdateIntervalSquares(groupViewModel);
                        }
                    }

                    // Add the interval to IntervalDomains with the ConsensusFragment
                    IntervalDomains.Add(new IntervalDomain
                    {
                        Start = start,
                        End = end,
                        Description = description,
                        ConsensusFragment = consensusFragment
                    });

                    // Refresh the squares to ensure the new domain is recognized
                    if (DataContext is SequenceViewModel updatedViewModel)
                    {
                        foreach (var groupViewModel in updatedViewModel.ReferenceGroups)
                        {
                            UpdateIntervalSquares(groupViewModel);
                        }
                    }

                    // Close the popup
                    popup.IsOpen = false;
                }
                else
                {
                    MessageBox.Show("Please enter valid start and end positions.");
                }
            }
        }

        private void UpdateIntervalSquares(ReferenceGroupViewModel groupViewModel)
        {
            groupViewModel.IntervalSquares.Clear();

            // Preencher os quadrados com base nas posições da ReferenceSequence
            for (int i = 1; i <= groupViewModel.ReferenceSequence.Count; i++)
            {
                var domain = IntervalDomains.FirstOrDefault(d => i >= d.Start && i <= d.End);

                if (domain != null)
                {
                    groupViewModel.IntervalSquares.Add(new AlignmentsChar
                    {
                        Position = i,
                        BackgroundColor = Brushes.LightSteelBlue,
                        Char = " ",
                        ToolTipContent = $"Positions {domain.Start}-{domain.End}: {domain.Description} - Fragment: {domain.ConsensusFragment}"
                    });
                }
                else
                {
                    groupViewModel.IntervalSquares.Add(new AlignmentsChar
                    {
                        Position = i,
                        BackgroundColor = Brushes.Transparent,
                        Char = " ",
                        ToolTipContent = null
                    });
                }
            }

            groupViewModel.OnPropertyChanged(nameof(groupViewModel.IntervalSquares));
        }



        //private void ColorQuadradosComToolTip(ReferenceGroupViewModel groupViewModel, int start, int end, string tooltipDescription)
        //{
        //    // Itera sobre a sequência de visualização (os quadrados)
        //    foreach (var sequenceViewModel in groupViewModel.Seq)
        //    {
        //        for (int i = 0; i < sequenceViewModel.VisualAlignment.Count; i++)
        //        {
        //            var alignmentChar = sequenceViewModel.VisualAlignment[i];

        //            // Se o índice atual está dentro do intervalo definido
        //            if (i >= start - 1 && i <= end - 1)
        //            {
        //                // Altera a cor do quadrado
        //                alignmentChar.BackgroundColor = Brushes.LightSteelBlue;

        //                // Adiciona o ToolTip com a descrição do intervalo
        //                alignmentChar.ToolTipContent = tooltipDescription;
        //            }
        //        }
        //    }
        //}


        private void OnColorILChecked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SequenceViewModel;
            if (viewModel != null)
            {
                viewModel.ColorIL = true;
                viewModel.UpdateILColoring();
            }
        }

        private void OnColorILUnchecked(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as SequenceViewModel;
            if (viewModel != null)
            {
                viewModel.ColorIL = false;
                viewModel.UpdateILColoring();
            }
        }

        public class ReferenceGroupViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<SequencesViewModel> _seq;
            private ObservableCollection<DataTableAlign> _alignmetns;
            private ObservableCollection<AlignmentsChar> _referenceSequence;
            private ObservableCollection<ConsensusChar> _consensusSequence;
            private ObservableCollection<AlignmentsChar> _intervalSquares; 

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

            public ObservableCollection<AlignmentsChar> ReferenceSequence
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

            public ObservableCollection<AlignmentsChar> IntervalSquares
            {
                get => _intervalSquares;
                set
                {
                    _intervalSquares = value;
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
                ReferenceSequence = new ObservableCollection<AlignmentsChar>();
                ConsensusSequence = new ObservableCollection<ConsensusChar>();
                IntervalSquares = new ObservableCollection<AlignmentsChar>(); 

            }

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public class AlignmentsChar : INotifyPropertyChanged
        {
            private string _letter;
            private SolidColorBrush _backgroundColor;
            private Brush _borderBrush; 

            public SolidColorBrush OriginalBackgroundColor { get; set; }

            public string Char
            {
                get => _letter;
                set
                {
                    _letter = value;
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

            public Brush BorderBrush
            {
                get => _borderBrush;
                set
                {
                    _borderBrush = value;
                    OnPropertyChanged();
                }
            }

            public int Position { get; set; }

            public string ToolTipContent { get; internal set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
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

            public ObservableCollection<AlignmentsChar> VisualAlignment { get; set; } = new ObservableCollection<AlignmentsChar>();
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
                    UpdateILColoring();
                }
            }


            public ObservableCollection<ReferenceGroupViewModel> ReferenceGroups { get; set; } = new();

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            public void UpdateILColoring()
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

                        // Check if the reference sequence has 'I' or 'L' at this position
                        if (i < group.ReferenceSequence.Count)
                        {
                            var refChar = group.ReferenceSequence[i];
                            if (refChar.Char == "I" || refChar.Char == "L")
                            {
                                hasILinReference = true;
                            }
                        }

                        // Check if any aligned sequence has 'I' or 'L' at this position
                        foreach (var seq in group.Seq)
                        {
                            if (i < seq.VisualAlignment.Count)
                            {
                                var alignmentChar = seq.VisualAlignment[i];
                                if (alignmentChar.Char == "I" || alignmentChar.Char == "L")
                                {
                                    hasILinAligned = true;
                                }
                            }
                        }

                        // Update the background color of the consensus character based on the presence of 'I' or 'L'
                        if (ColorIL && hasILinReference && hasILinAligned)
                        {
                            consensusChar.BackgroundColor = new SolidColorBrush(Colors.LightGreen);
                        }
                        else
                        {
                            consensusChar.BackgroundColor = consensusChar.OriginalBackgroundColor;
                        }

                        // Update the background color of the alignment characters
                        foreach (var seq in group.Seq)
                        {
                            if (i < seq.VisualAlignment.Count)
                            {
                                var alignmentChar = seq.VisualAlignment[i];

                                // Store the original color if it is not yet defined
                                if (alignmentChar.OriginalBackgroundColor == null)
                                {
                                    alignmentChar.OriginalBackgroundColor = alignmentChar.BackgroundColor;
                                }

                                // Update the background color based on the ColorIL option and presence of 'I' or 'L'
                                if (ColorIL && (alignmentChar.Char == "I" || alignmentChar.Char == "L"))
                                {
                                    alignmentChar.BackgroundColor = new SolidColorBrush(Colors.LightGreen);
                                }
                                else
                                {
                                    alignmentChar.BackgroundColor = alignmentChar.OriginalBackgroundColor;
                                }
                            }
                        }
                    }
                }
            }


        }

        public void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, List<Alignment> sequencesToAlign, List<(string ID, string Description, string Sequence)> referenceSequences)
        {
            // Stores the IDs already processed to avoid duplications
            HashSet<string> processedIds = new HashSet<string>();

            foreach (var (id, description, referenceSequence) in referenceSequences)
            {
                var groupViewModel = new ReferenceGroupViewModel
                {
                    ReferenceHeader = $"{id} - {description}",
                    ID = id,
                    Description = description
                };

                int position = 1;  // Initialize the position with 1

                // Add each character of the reference sequence to the ReferenceSequence collection
                foreach (char letter in referenceSequence)
                {
                    groupViewModel.ReferenceSequence.Add(new AlignmentsChar
                    {
                        Char = letter.ToString(),
                        BackgroundColor = Brushes.White, 
                        Position = position 
                    });
                    position++;  
                }

                // Sort the sequences for alignment
                var sortedSequences = sequencesToAlign.OrderBy(seq => seq.StartPositions.Min()).ToList();
                Dictionary<int, int> rowEndPositions = new Dictionary<int, int>();

                foreach (var sequence in sortedSequences)
                {
                    var sequenceViewModel = new SequencesViewModel();
                    //{
                    //    ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - : {sequence.SourceOrigin}"
                    //};

                    var dataTableViewModel = new DataTableAlign
                    {
                        //ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - : {sequence.SourceOrigin}",
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

                    // Logic to fill the visual alignment
                    int startPosition = sequence.StartPositions.Min() - 1;
                    int rowIndex = SequenceAssemblerLogic.AssemblyTools.AssemblyParameters.FindAvailableRow(rowEndPositions, startPosition, sequence.AlignedSmallSequence.Length);

                    for (int i = 0; i < startPosition; i++)
                    {
                        if (i >= rowEndPositions.GetValueOrDefault(rowIndex, 0))
                        {
                            sequenceViewModel.VisualAlignment.Add(new AlignmentsChar { Char = " ", BackgroundColor = Brushes.LightGray });
                        }
                    }

                    rowEndPositions[rowIndex] = startPosition + sequence.AlignedSmallSequence.Length;

                    foreach (char seqChar in sequence.AlignedSmallSequence)
                    {
                        SolidColorBrush backgroundColor;
                        int refIndex = startPosition++;
                        string letter;

                        
                        if (seqChar == '-')
                        {
                            backgroundColor = Brushes.LightGray;
                            letter = " ";
                        }
                        else if (refIndex < referenceSequence.Length)
                        {
                            if (seqChar == referenceSequence[refIndex])
                            {
                                backgroundColor = Brushes.LightGreen;  
                            }
                            else
                            {
                                backgroundColor = Brushes.LightCoral;  
                            }
                            letter = seqChar.ToString();
                        }
                        else
                        {
                            backgroundColor = Brushes.LightGray; 
                            letter = seqChar.ToString();
                        }

                        Brush borderBrush = sequence.SourceType == "PSM" ? new SolidColorBrush(Color.FromRgb(34, 139, 34)) : new SolidColorBrush(Color.FromRgb(218, 165, 32));

                        var visualAlignment = new AlignmentsChar
                        {
                            Char = letter,
                            BackgroundColor = backgroundColor,  
                            BorderBrush = borderBrush,          
                            ToolTipContent = $"Start Position: {sequence.StartPositions.Min()} - Sequence: {sequence.SourceSeq}, Origin: {sequence.SourceOrigin}, Type: {sequence.SourceType}"
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
                            sequenceViewModel.VisualAlignment.Add(new AlignmentsChar { Char = " ", BackgroundColor = Brushes.LightGray });
                        }
                    }
                }

                // Add the consensus data
                var (consensusChars, consensusWithTemplateChars, consensusWithGaps, totalCoverage) = SequenceAssemblerLogic.AssemblyTools.AssemblyParameters.BuildConsensus(sequencesToAlign, referenceSequence);
                groupViewModel.ConsensusSequence = new ObservableCollection<ConsensusChar>();

                // Using consensusChars to keep original colors
                foreach (var (consensusChar, isFromReference, isDifferent) in consensusChars)
                {
                    SolidColorBrush color;
                    if (isFromReference) 
                    {
                        color = Brushes.White;
                    }
                    else if (consensusChar == '-') 
                    {
                        color = Brushes.LightGray;
                    }
                    else if (isDifferent)
                    {
                        color = Brushes.Orange;
                    }
                    else
                    {
                        color = Brushes.LightGreen;
                    }

                    groupViewModel.ConsensusSequence.Add(new ConsensusChar
                    {
                        Char = consensusChar.ToString(),
                        BackgroundColor = color,
                        OriginalBackgroundColor = color
                    });
                }

                // Check if the ID has already been processed
                if (!processedIds.Contains(id))
                {
                    // Save both versions of the consensus (with template letters and with gaps)
                    SequenceAssemblerLogic.AssemblyTools.AssemblyParameters.SaveConsensusToFile(referenceSequence, consensusWithTemplateChars, consensusWithGaps, id, description);
                    processedIds.Add(id);
                }

                // Add coverage to the reference group
                groupViewModel.Coverage = totalCoverage;
                viewModel.ReferenceGroups.Add(groupViewModel);
            }
        }


        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteLocalAssembly();
        }

        public void UpdateViewLocalModel(List<Fasta> allFastaSequences, List<Alignment> alignments)
        {
            if (DataContext is SequenceViewModel viewModel)
            {

                viewModel.ReferenceGroups.Clear();

                foreach (var fasta in allFastaSequences)
                {
                    //Select alignments that have the TargetOrigin equal to the fasta sequence ID
                    var sequencesToAlign = alignments.Where(a => a.TargetOrigin == fasta.ID).ToList();
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


        public void ExecuteLocalAssembly()
        {
            if (!(DataContext is SequenceViewModel viewModel))
            {
                MessageBox.Show("Failed to get the data context.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            viewModel.UpdateILColoring();

            Console.WriteLine("Assembly executed successfully.");
        }


        private void DataGridAlignments_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            // Scroll verticalmente conforme o movimento da roda do mouse.
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3);

            // Marcar o evento como tratado para evitar propagação desnecessária.
            e.Handled = true;
        }

    }

}