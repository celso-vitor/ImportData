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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SequenceAssemblerGUI.UniprotApi;
using System.Net.Http;
using System.Threading.Tasks;
using OxyPlot;
using System.Windows.Data;


namespace SequenceAssemblerGUI
{
    public partial class Assembly : UserControl
    {

        public Assembly()
        {
            InitializeComponent();

            //// Initialize the ObservableCollection to store the intervals Domains
            IntervalDomains = new ObservableCollection<IntervalDomain>();

            IntervalPTMs = new ObservableCollection<IntervalPTM>();

            // Create and configure the ViewModel (if necessary)
            var viewModel = new SequenceViewModel
            {
                ColorIL = true // Set ColorIL to true by default
            };

            DataContext = viewModel;

            //// Bind the DataGrid to the IntervalDomains collection
            var intervalDataGrid = (DataGrid)FindName("IntervalsDataGrid");
            if (intervalDataGrid != null)
            {
                intervalDataGrid.ItemsSource = IntervalDomains;
            }

        }

        private static readonly HttpClient client = new HttpClient();

        //-----------------------------------------------------------------------BLASTP
        public async Task<List<IntervalDomain>> GetBlastResults(string rid, string referenceSequence)
        {
            Console.WriteLine("Waiting for BLAST results...");
            string results = await WaitForBlastResults(rid);
            Console.WriteLine("Filtering results by identity...");
            var filteredResults = FilterResultsByIdentity(results);
            //foreach (var result in filteredResults)
            //{
            //    Console.WriteLine(result);
            //}

            var identifiers = ExtractIdentifiers(filteredResults);

            IntervalDomains.Clear();

            foreach (var identifier in identifiers)
            {
                try
                {

                    string genBankInfo = await GetGenBankInfo(identifier);

                    if (!string.IsNullOrEmpty(genBankInfo))
                    {

                        string fullSequence = ExtractSequence(genBankInfo).ToUpper();

                        var structuralRegions = ExtractStructuralRegions(genBankInfo, fullSequence);

                        foreach (var region in structuralRegions)
                        {
                            Console.WriteLine($"{region.Key}: {region.Value.Item1}-{region.Value.Item2}");
                        }

                        string cleanedReferenceSequence = referenceSequence.Replace("\n", "").Replace("\r", "").Trim().ToUpper();

                        var sequenceAligner = new SequenceAssemblerLogic.ProteinAlignmentCode.SequenceAligner();

                        foreach (var region in structuralRegions)
                        {
                            try
                            {
                                string regionName = region.Key;
                                int start = region.Value.Item1;
                                int end = region.Value.Item2;

                                string correctSubsequence = fullSequence.Substring(start - 1, end - start + 1);


                                if (string.IsNullOrEmpty(cleanedReferenceSequence))
                                {
                                    Console.WriteLine(" Error: Reference string is empty or null!");
                                    continue;
                                }

                                var alignmentResult = sequenceAligner.AlignerLocal(cleanedReferenceSequence, correctSubsequence, regionName);

                                if (alignmentResult != null && alignmentResult.StartPositions.Any())
                                {

                                    foreach (int alignedStart in alignmentResult.StartPositions)
                                    {
                                        int alignedEnd = alignedStart + correctSubsequence.Length - 1;

                                        var domain = new IntervalDomain
                                        {
                                            Start = alignedStart,
                                            End = alignedEnd,
                                            Description = regionName,
                                            SequenceId = identifier,
                                            ConsensusFragment = correctSubsequence,
                                        };

                                        if (!IntervalDomains.Any(d => d.Description == domain.Description))
                                        {
                                            IntervalDomains.Add(domain);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($" {regionName}: No alignment found in reference sequence.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($" Error aligning region {region.Key}: {ex.Message}");
                            }
                        }

                        Console.WriteLine("\n Alignment completed!\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing identifier {identifier}: {ex.Message}");
                }

                await Task.Delay(1000);
            }

            return IntervalDomains.ToList();
        }



        private async Task<string> WaitForBlastResults(string rid)
        {
            int delay = 3000; // Initial delay in milliseconds
            int maxDelay = 60000; // Maximum delay (e.g., 1 minute)

            while (true)
            {
                var statusParameters = new System.Collections.Specialized.NameValueCollection
    {
        { "CMD", "Get" },
        { "FORMAT_OBJECT", "SearchInfo" },
        { "RID", rid }
    };

                var statusContent = new FormUrlEncodedContent(statusParameters.AllKeys
                    .SelectMany(key => statusParameters.GetValues(key)
                    .Select(value => new KeyValuePair<string, string>(key, value))));

                try
                {
                    var statusResponse = await client.PostAsync("https://blast.ncbi.nlm.nih.gov/blast/Blast.cgi", statusContent);
                    var responseBody = await statusResponse.Content.ReadAsStringAsync();

                    // Console.WriteLine($"BLAST Status Response: {responseBody}");

                    if (responseBody.Contains("Status=READY"))
                    {
                        break;
                    }
                    else if (responseBody.Contains("Status=FAILED"))
                    {
                        throw new Exception("BLAST query failed.");
                    }

                    // Increase delay exponentially (e.g., double the delay each time)
                    delay = Math.Min(delay * 2, maxDelay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking BLAST status: {ex.Message}");
                    // Optionally retry or handle the error based on your requirements
                }

                await Task.Delay(delay); // Wait before checking again
            }

            // Fetch the final results once the job is ready
            var resultsParameters = new System.Collections.Specialized.NameValueCollection
{
    { "CMD", "Get" },
    { "FORMAT_TYPE", "Text" },
    { "RID", rid }
};

            var resultsContent = new FormUrlEncodedContent(resultsParameters.AllKeys
                .SelectMany(key => resultsParameters.GetValues(key)
                .Select(value => new KeyValuePair<string, string>(key, value))));

            var resultsResponse = await client.PostAsync("https://blast.ncbi.nlm.nih.gov/blast/Blast.cgi", resultsContent);
            var results = await resultsResponse.Content.ReadAsStringAsync();

            // Print the BLAST results to the console
            //Console.WriteLine("BLAST Results:");
            //Console.WriteLine(results);

            return results;
        }

        private List<string> FilterResultsByIdentity(string blastResults)
        {
            var filteredResults = new List<string>();
            var lines = blastResults.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("100%"))
                {
                    filteredResults.Add(line);
                }
            }
            return filteredResults;
        }

        private List<string> ExtractIdentifiers(List<string> filteredResults)
        {
            var identifiers = new List<string>();
            foreach (var line in filteredResults)
            {
                var match = Regex.Match(line, @"^\S+");
                if (match.Success)
                {
                    identifiers.Add(match.Value);
                }
            }
            return identifiers;

        }

        private static async Task<string> GetGenBankInfo(string identifier)
        {
            string baseUrl = "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/";
            string searchUrl = $"{baseUrl}esearch.fcgi?db=protein&term={identifier}";

            try
            {
                var searchResponse = await client.GetStringAsync(searchUrl);
                var idMatch = Regex.Match(searchResponse, @"<Id>(\d+)</Id>");
                if (!idMatch.Success)
                {
                    throw new Exception("ID not found in NCBI.");
                }
                string id = idMatch.Groups[1].Value;

                string fetchUrl = $"{baseUrl}efetch.fcgi?db=protein&id={id}&rettype=gb&retmode=text";
                var fetchResponse = await client.GetStringAsync(fetchUrl);

                return fetchResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing NCBI GenBank: {ex.Message}");
                return null;
            }
        }

        private string ExtractSequence(string genBankInfo)
        {
            var sequenceRegex = new Regex(@"ORIGIN\s+([\s\S]+?)//");
            var match = sequenceRegex.Match(genBankInfo);

            if (!match.Success)
            {
                throw new Exception("Could not find sequence in ORIGIN section.");
            }

            var rawSequence = match.Groups[1].Value;
            var cleanedSequence = Regex.Replace(rawSequence, @"\d+|\s+", ""); ;

            return cleanedSequence;
        }

        private Dictionary<string, (int Start, int End, string Sequence)> ExtractStructuralRegions(string genBankInfo, string fullSequence)
        {
            var regionRegex = new Regex(@"(\d+)\.\.(\d+)\s+/region_name=""(.*?)""");
            var matches = regionRegex.Matches(genBankInfo);
            var structuralRegions = new Dictionary<string, (int Start, int End, string Sequence)>();

            foreach (Match match in matches)
            {
                string regionName = match.Groups[3].Value;
                int start = int.Parse(match.Groups[1].Value);
                int end = int.Parse(match.Groups[2].Value);

                string subsequence = fullSequence.Substring(start - 1, end - start + 1).ToUpper();

                structuralRegions[regionName] = (start, end, subsequence);
            }

            return structuralRegions;
        }

        private void OnBlastSearchClick(object sender, RoutedEventArgs e)
        {
            var popup = (Popup)FindName("BlastPopup");
            if (popup != null)
            {
                popup.IsOpen = true;
            }
        }
        private void CloseBlastPopupButton_Click(object sender, RoutedEventArgs e)
        {
            var popup = (Popup)FindName("BlastPopup");
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }


        private string GetConsensusFragment(string consensusSequence, int start, int end)
        {
            int startIndex = start - 1;
            int endIndex = end - 1;

            if (startIndex >= 0 && endIndex < consensusSequence.Length)
            {
                return consensusSequence.Substring(startIndex, endIndex - startIndex + 1);
            }

            return string.Empty;
        }

        private static async Task<string> SubmitBlastQuery(string query)
        {
            using var client = new HttpClient();

            string formattedQuery = query.Replace("\r", "").Replace("\n", "");

            var submitParameters = new Dictionary<string, string>
    {
        { "CMD", "Put" },
        { "QUERY", formattedQuery },
        { "DATABASE", "nr" },
        { "PROGRAM", "blastp" },
        { "FILTER", "L" },
        { "FORMAT_TYPE", "XML" }
    };

            var submitContent = new FormUrlEncodedContent(submitParameters);

            var submitResponse = await client.PostAsync("https://blast.ncbi.nlm.nih.gov/Blast.cgi", submitContent);
            var responseBody = await submitResponse.Content.ReadAsStringAsync();

            var match = Regex.Match(responseBody, @"RID\s*=\s*(\S+)");
            if (!match.Success)
            {
                throw new Exception("Could not get RID from response.");
            }

            return match.Groups[1].Value.Trim();
        }


        private async void OnConfirmBlastRangeClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("OnConfirmBlastRangeClick method started.");

            if (BlastSequenceSelector.SelectedValue is string selectedSequenceId)
            {

                var popup = (Popup)FindName("BlastPopup");
                if (popup != null)
                {
                    popup.IsOpen = false;
                    Console.WriteLine("Popup closed.");
                }

                try
                {
                    BlastProgressBar.Visibility = Visibility.Visible;
                    BlastStatusLabel.Visibility = Visibility.Visible;
                    BlastStatusLabel.Text = "Starting search in BLAST...";
                    Console.WriteLine("Status: Iniciando busca BLAST...");
                    BlastSearchButton.IsEnabled = false;

                    var sequenceViewModel = DataContext as SequenceViewModel;
                    var groupViewModel = sequenceViewModel?.ReferenceGroups.FirstOrDefault(g => g.ID == selectedSequenceId);

                    if (groupViewModel != null)
                    {
                        Console.WriteLine("Sequence group found.");
                        Console.WriteLine($"Reference sequence length: {groupViewModel.ConsensusSequence.Count}");

                        string consensusSequence = new string(groupViewModel.ConsensusSequence.Select(c => c.Char[0]).ToArray());
                        Console.WriteLine($"Generated consensus sequence: {consensusSequence}");

                        BlastStatusLabel.Text = "Submit BLAST query...";

                        string rid = await SubmitBlastQuery(consensusSequence);

                        BlastStatusLabel.Text = "Waiting for BLAST results...";

                        var domains = await GetBlastResults(rid, consensusSequence);

                        if (domains.Any())
                        {

                            foreach (var domain in domains)
                            {
                                if (!IntervalDomains.Any(d => d.Start == domain.Start && d.End == domain.End && d.SequenceId == domain.SequenceId))
                                {
                                    groupViewModel.AddIntervalIfNotExists(domain); Console.WriteLine($"Domínio adicionado: {domain.Start}-{domain.End}");
                                }
                            }

                            UpdateIntervalSquares(groupViewModel);
                            BlastSearchButton.IsEnabled = true;

                        }

                        else
                        {
                            MessageBox.Show("No structural regions found for the selected sequence.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("The selected sequence could not be found.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error retrieving structural regions: {ex.Message}");
                }
                finally
                {
                    // Ocultar o ProgressBar e a Label após o término
                    BlastProgressBar.Visibility = Visibility.Collapsed;
                    BlastStatusLabel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                Console.WriteLine("No sequence selected.");
                MessageBox.Show("No sequence selected. Please select a sequence before applying the range.");
            }

            Console.WriteLine("Método OnConfirmBlastRangeClick finalizado.");
        }



        private void UpdateIntervalSquares(ReferenceGroupViewModel groupViewModel)
        {
            IntervalsDataGrid.Visibility = Visibility.Visible;

            groupViewModel.IntervalSquares.Clear();

            var positionMap = new Dictionary<int, AlignmentsChar>();

            foreach (var domain in IntervalDomains)
            {
                for (int i = domain.Start; i <= domain.End; i++)
                {
                    if (i < 1 || i > groupViewModel.ReferenceSequence.Count)
                    {
                        continue;
                    }

                    int adjustedPosition = i;

                    if (!positionMap.ContainsKey(adjustedPosition))
                    {
                        positionMap[adjustedPosition] = new AlignmentsChar
                        {
                            Position = adjustedPosition,
                            Char = groupViewModel.ReferenceSequence[adjustedPosition - 1].ToString(),
                            BackgroundColor = Brushes.Transparent,
                            ToolTipContent = null
                        };
                    }

                    var alignmentChar = positionMap[adjustedPosition];

                    var domainColor = GetDomainColor(domain.Description);
                    var domainTooltip = $"Region: {domain.Start}-{domain.End} - {domain.Description}";

                    if (ShouldOverwriteColor(alignmentChar.BackgroundColor, domainColor))
                    {
                        alignmentChar.BackgroundColor = domainColor;
                        alignmentChar.ToolTipContent = domainTooltip;
                    }
                }
            }

            for (int pos = 1; pos <= groupViewModel.ReferenceSequence.Count; pos++)
            {
                if (!positionMap.ContainsKey(pos))
                {
                    positionMap[pos] = new AlignmentsChar
                    {
                        Position = pos,
                        Char = groupViewModel.ReferenceSequence[pos - 1].ToString(),
                        BackgroundColor = Brushes.Transparent,
                        ToolTipContent = null
                    };
                }
            }

            foreach (var pos in positionMap.Keys.OrderBy(p => p))
            {
                groupViewModel.IntervalSquares.Add(positionMap[pos]);
            }

            groupViewModel.OnPropertyChanged(nameof(groupViewModel.IntervalSquares));

            BlastSearchButton.IsEnabled = true;

        }

        private bool ShouldOverwriteColor(SolidColorBrush existingColor, SolidColorBrush newColor)
        {
            if (existingColor == null || existingColor.Color == Colors.Transparent)
            {
                return true;
            }

            if (existingColor.Color == Colors.OrangeRed)
            {
                return false;
            }

            if (existingColor.Color == Colors.LightSteelBlue && newColor.Color != Colors.OrangeRed)
            {
                return false;
            }

            if (existingColor.Color == Colors.Goldenrod &&
                newColor.Color != Colors.OrangeRed && newColor.Color != Colors.LightSteelBlue)
            {
                return false;
            }

            if (existingColor.Color == Colors.LightSlateGray &&
                (newColor.Color == Colors.LightSteelBlue || newColor.Color == Colors.Goldenrod || newColor.Color == Colors.OrangeRed))
            {
                return false;
            }

            return true;
        }



        private SolidColorBrush GetDomainColor(string regionName)
        {
            if (regionName.Contains("CDR"))
            {
                return new SolidColorBrush(Colors.OrangeRed);
            }
            else if (regionName.Contains("FR"))
            {

                return new SolidColorBrush(Colors.LightSteelBlue);
            }
            else if (regionName.Contains("Ig") || regionName.Contains("strand"))
            {
                return new SolidColorBrush(Colors.Goldenrod);
            }
            else if (regionName.Contains("Domain"))
            {
                return new SolidColorBrush(Colors.LightSlateGray);
            }
            else
            {
                return new SolidColorBrush(Colors.LightGray);
            }
        }

        //-----------------------------------------------------------------------------------

        public async Task<List<IntervalDomain>> GetDomainsFromUniProt(string proteinId, string consensusSequence)
        {
            string url = $"https://www.uniprot.org/uniprot/{proteinId}.json";

            try
            {
                var response = await client.GetStringAsync(url);
                var proteinData = JsonConvert.DeserializeObject<ProteinData>(response);

                if (proteinData?.Features == null || !proteinData.Features.Any())
                {
                    throw new Exception("No domain or PTM data found.");
                }

                IntervalDomains.Clear();

                var domains = proteinData.Features
                    .Where(f => f.Type == "Domain")
                    .Select(f => new IntervalDomain
                    {
                        Start = f.Location.Start.Value,
                        End = f.Location.End.Value,
                        Description = f.Description,
                        ConsensusFragment = GetConsensusFragment(consensusSequence, f.Location.Start.Value, f.Location.End.Value),
                        SequenceId = proteinId
                    })
                    .ToList();

                var ptms = proteinData.Features
                    .Where(f => f.Type == "Modified residue" || f.Type == "Glycosylation" || f.Type == "Disulfide bond")
                    .Select(f => new IntervalPTM
                    {
                        Start = f.Location.Start.Value,
                        End = f.Location.End.Value,
                        Description = f.Description,
                        Type = f.Type,
                        SequenceId = proteinId
                    })
                    .ToList();

                foreach (var domain in domains)
                {
                    IntervalDomains.Add(domain);
                }

                foreach (var ptm in ptms)
                {
                    IntervalPTMs.Add(ptm);
                }


                return domains;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error accessing UniProt API data: {ex.Message}");
            }
        }


        private void OnInsertRangeClick(object sender, RoutedEventArgs e)
        {
            var popup = (Popup)FindName("RangePopup");
            if (popup != null)
            {
                popup.IsOpen = true;
            }
        }

        private void ClosePopupButton_Click(object sender, RoutedEventArgs e)
        {
            RangePopup.IsOpen = false;
        }
        private async void OnConfirmRangeClick(object sender, RoutedEventArgs e)
        {
            if (SequenceSelector.SelectedValue is string selectedSequenceId)
            {
                try
                {
                    var sequenceIdParts = selectedSequenceId.Split('|');
                    string proteinId = sequenceIdParts.Length > 1 ? sequenceIdParts[1] : selectedSequenceId;

                    var sequenceViewModel = (DataContext as SequenceViewModel);
                    var groupViewModel = sequenceViewModel?.ReferenceGroups.FirstOrDefault(g => g.ID == selectedSequenceId);

                    var popup = (Popup)FindName("RangePopup");
                    if (popup != null)
                    {
                        popup.IsOpen = false;
                    }
                    InsertRangeButton.IsEnabled = false;

                    if (groupViewModel != null)
                    {
                        string consensusSequence = new string(groupViewModel.ConsensusSequence.Select(c => c.Char[0]).ToArray());

                        var domains = await GetDomainsFromUniProt(proteinId, consensusSequence);

                        if (domains.Any())
                        {
                            foreach (var domain in domains)
                            {
                                if (!IntervalDomains.Any(d => d.Start == domain.Start && d.End == domain.End && d.SequenceId == domain.SequenceId))
                                {
                                    IntervalDomains.Add(domain);
                                }
                            }

                            UpdateIntervalUniprot(groupViewModel);
                            InsertRangeButton.IsEnabled = true;

                            var popupUni = (Popup)FindName("RangePopup");
                            if (popupUni != null)
                            {
                                popupUni.IsOpen = false;
                            }
                        }
                        else
                        {
                            MessageBox.Show("No domains found for the selected sequence.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("The selected sequence could not be found.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error retrieving domains: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("No sequence selected. Please select a sequence before applying the range.");
            }
        }

        private void UpdateIntervalUniprot(ReferenceGroupViewModel groupViewModel)
        {

            IntervalsDataGrid.Visibility = Visibility.Visible;
            groupViewModel.IntervalSquares.Clear();

            for (int i = 1; i <= groupViewModel.ReferenceSequence.Count; i++)
            {
                var domain = IntervalDomains.FirstOrDefault(d => i >= d.Start && i <= d.End);
                var ptm = IntervalPTMs.FirstOrDefault(p => i >= p.Start && i <= p.End);

                var alignmentChar = new AlignmentsChar
                {
                    Position = i,
                    Char = " ",
                    BackgroundColor = Brushes.Transparent,
                    ToolTipContent = null
                };

                if (domain != null)
                {
                    alignmentChar.BackgroundColor = Brushes.LightSteelBlue;
                    alignmentChar.ToolTipContent = $"Domain: {domain.Start}-{domain.End} - {domain.Description}";
                }

                if (ptm != null)
                {
                    alignmentChar.Char = "•";


                    switch (ptm.Type)
                    {
                        case "Modified residue":
                            alignmentChar.BackgroundColor = Brushes.LightSteelBlue;
                            break;
                        case "Glycosylation":
                            alignmentChar.BackgroundColor = Brushes.OrangeRed;
                            break;
                        case "Disulfide bond":
                            alignmentChar.BackgroundColor = Brushes.LightSlateGray;
                            break;
                        default:
                            alignmentChar.BackgroundColor = Brushes.Goldenrod;
                            break;
                    }

                    if (domain != null)
                    {
                        alignmentChar.ToolTipContent += $"\nPTM: {ptm.Start}-{ptm.End} - {ptm.Description} ({ptm.Type})";
                    }
                    else
                    {
                        alignmentChar.ToolTipContent = $"PTM: {ptm.Start}-{ptm.End} - {ptm.Description} ({ptm.Type})";
                    }
                }



                groupViewModel.IntervalSquares.Add(alignmentChar);
            }

            groupViewModel.OnPropertyChanged(nameof(groupViewModel.IntervalSquares));
        }

        public ObservableCollection<IntervalDomain> IntervalDomains { get; set; }
        public class IntervalDomain
        {
            public int Start { get; set; }
            public int End { get; set; }
            public string SequenceId { get; set; }
            public string Description { get; set; }
            public string ConsensusFragment { get; set; }
        }
        public ObservableCollection<IntervalPTM> IntervalPTMs { get; set; }
        public class IntervalPTM
        {
            public int Start { get; set; }
            public int End { get; set; }
            public string SequenceId { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }
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

            private ObservableCollection<IntervalDomain> _intervalDomains;

            public ObservableCollection<IntervalDomain> IntervalDomains
            {
                get => _intervalDomains;
                set
                {
                    _intervalDomains = value;
                    OnPropertyChanged(nameof(IntervalDomains));
                }
            }
            public void AddIntervalIfNotExists(IntervalDomain newInterval)
            {
                if (!IntervalDomains.Any(item => item.Description == newInterval.Description))
                {
                    IntervalDomains.Add(newInterval);
                }
            }
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

                        bool hasIinReference = false, hasLinReference = false;
                        bool hasIinAligned = false, hasLinAligned = false;
                        string refChar = null;

                        // Obtém o caractere do template (sequência de referência)
                        if (i < group.ReferenceSequence.Count)
                        {
                            refChar = group.ReferenceSequence[i].Char;

                            // Verifica se a referência contém 'I' ou 'L'
                            if (!string.IsNullOrWhiteSpace(refChar) && refChar != "-")
                            {
                                if (refChar == "I") hasIinReference = true;
                                if (refChar == "L") hasLinReference = true;
                            }
                        }

                        // Verifica as sequências alinhadas
                        foreach (var seq in group.Seq)
                        {
                            if (i < seq.VisualAlignment.Count)
                            {
                                var alignmentChar = seq.VisualAlignment[i].Char;

                                // Considera apenas 'I' e 'L', ignorando outros caracteres
                                if (!string.IsNullOrWhiteSpace(alignmentChar) && alignmentChar != "-")
                                {
                                    if (alignmentChar == "I") hasIinAligned = true;
                                    if (alignmentChar == "L") hasLinAligned = true;
                                }
                            }
                        }

                        // Apenas atualiza a cor de 'I' e 'L'
                        if (consensusChar.Char == "I" || consensusChar.Char == "L")
                        {
                            if (ColorIL && hasIinAligned && hasLinAligned)
                            {
                                consensusChar.BackgroundColor = new SolidColorBrush(Colors.LightCyan); // Verde se 'I' e 'L' estiverem juntos
                            }
                            else
                            {
                                consensusChar.BackgroundColor = consensusChar.OriginalBackgroundColor; // Mantém cor original
                            }
                        }

                        // Atualiza a cor dos caracteres alinhados
                        foreach (var seq in group.Seq)
                        {
                            if (i < seq.VisualAlignment.Count)
                            {
                                var alignmentChar = seq.VisualAlignment[i];

                                // Armazena a cor original se ainda não foi definida
                                if (alignmentChar.OriginalBackgroundColor == null)
                                {
                                    alignmentChar.OriginalBackgroundColor = alignmentChar.BackgroundColor;
                                }

                                // Apenas atualiza 'I' e 'L', deixando outras letras intactas
                                if (alignmentChar.Char == "I" || alignmentChar.Char == "L")
                                {
                                    // Verde se 'I' e 'L' estiverem juntos
                                    if (ColorIL && hasIinAligned && hasLinAligned)
                                    {
                                        alignmentChar.BackgroundColor = new SolidColorBrush(Colors.LightCyan);
                                    }
                                    // Vermelho se apenas 'I' ou 'L' aparecer sozinho e o template não for nenhum dos dois
                                    else if (ColorIL && (alignmentChar.Char == "I" || alignmentChar.Char == "L") && (refChar != "I" && refChar != "L"))
                                    {
                                        alignmentChar.BackgroundColor = new SolidColorBrush(Colors.LightCoral);
                                    }
                                    else
                                    {
                                        alignmentChar.BackgroundColor = alignmentChar.OriginalBackgroundColor; // Mantém a cor original
                                    }
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
                                backgroundColor = Brushes.LightCyan;
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
                        color = Brushes.LightCyan;
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