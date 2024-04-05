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

namespace SequenceAssemblerGUI
{
    public partial class Assembly : Window
    {
        private SequenceAligner sequenceAligner;


        public Assembly()
        {
            InitializeComponent();
            sequenceAligner = new SequenceAligner();
            DataContext = new SequenceViewModel();
        }


        //Alinhamento/Cores/Matchs/Gaps 
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

        //Contigs/Alinhamento/Template
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

            private string _totalScore;
            public string TotalScore
            {
                get { return _totalScore; }
                set { _totalScore = value; OnPropertyChanged(); }
            }

            private string _totalIdentities;
            public string TotalIdentities
            {
                get { return _totalIdentities; }
                set { _totalIdentities = value; OnPropertyChanged(); }
            }

            private string _totalPositives;
            public string TotalPositives
            {
                get { return _totalPositives; }
                set { _totalPositives = value; OnPropertyChanged(); }
            }

            private string _totalGaps;
            public string TotalGaps
            {
                get { return _totalGaps; }
                set { _totalGaps = value; OnPropertyChanged(); }
            }
        }



        //Botton Click/ Montagem dos alinhamentos
        //---------------------------------------------------------------------------------------------------------
        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var referenceSequenceFasta = ReferenceSequence.Text;
            var contigsFasta = ContigsSequence.Text;

            string referenceSequence = ReadFastaSequence(referenceSequenceFasta);
            var contigs = ReadContigs(contigsFasta);
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
                int startPosition = GetCorrectStartPosition(alignedReferenceSequence, alignedContigSequence, referenceSequence);
                startPositions.Add(startPosition);
            }

            viewModel.AssemblySequence = GenerateAssemblyText(referenceSequence, alignedContigs, startPositions);

            // Atualiza a UI com o alinhamento e a montagem
            UpdateUIWithAlignmentAndAssembly(viewModel, alignedContigs, startPositions, referenceSequence);

            // Definir IsReferenceSequenceAligned como true para garantir a visibilidade do rótulo "Reference"
            viewModel.IsReferenceSequenceAligned = true;
            viewModel.IsAssemblyVisible = true;
        }

        //Ajusta as posições reconhecendo os gaps
        int GetCorrectStartPosition(string alignedRef, string alignedContig, string fullRef)
        {
            int firstNonGapIndex = alignedContig.IndexOf(alignedContig.TrimStart('-').First());

            string nonGapContigStart = alignedContig.Substring(firstNonGapIndex).Replace("-", "");

            int startPosition = -1;

            for (int i = 0; i <= fullRef.Length - nonGapContigStart.Length; i++)
            {
                bool matchFound = true;
                for (int j = 0; j < nonGapContigStart.Length; j++)
                {
                    if (alignedRef[firstNonGapIndex + j] != '-')
                    {
                        if (fullRef[i + j] != alignedRef[firstNonGapIndex + j])
                        {
                            matchFound = false;
                            break;
                        }
                    }
                }

                if (matchFound)
                {
                    startPosition = i - firstNonGapIndex;  // Corrigido para subtrair os índices dos gaps iniciais
                    break;
                }
            }

            if (startPosition == -1)
            {
                throw new InvalidOperationException("Não foi possível encontrar uma posição de início válida para o contig na sequência de referência.");
            }

            // Ajustar para a contagem começar em 1 se o seu sistema espera indexação base-1
            return startPosition + 1;
        }




        //Leitura de formato fasta (>)
        //---------------------------------------------------------------------------------------------------------
        static string ReadFastaSequence(string fastaSequence)
        {
            int startIndex = fastaSequence.IndexOf('>');
            string sequence = fastaSequence.Substring(startIndex + 1);
            sequence = sequence.Replace("\r", "").Replace("\n", "");
            return sequence;
        }

        static Dictionary<string, string> ReadContigs(string fastaContigs)
        {
            string[] contigEntries = fastaContigs.Split(new[] { '>' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, string> contigs = new Dictionary<string, string>();
            int contigCount = 1;
            foreach (string contigEntry in contigEntries)
            {
                string contigId = $"Contig{contigCount}";
                StringBuilder sequence = new StringBuilder();
                string[] lines = contigEntry.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    sequence.Append(line.Trim());
                }
                if (sequence.Length > 0)
                {
                    contigs.Add(contigId, sequence.ToString());
                    contigCount++;
                }

            }

            return contigs;
        }


        //Assembly/Reference/Contigs
        //---------------------------------------------------------------------------------------------------------
        static (string alignedContigSequence, string alignedReferenceSequence) PerformAlignmentUsingAlignmentClass(string contigSequence, string referenceSequence)
        {
            SequenceAligner aligner = new SequenceAligner(); // Crie uma instância de SequenceAligner
            Alignment alignmentResult = aligner.AlignSequences(referenceSequence, contigSequence); // Chame o método AlignSequences nessa instância

            return (alignmentResult.AlignedSmallSequence, alignmentResult.AlignedLargeSequence);
        }


        private string GenerateAssemblyText(string referenceSequence, Dictionary<string, string> alignedContigs, List<int> startPositions)
        {
            // Cria uma sequência com o mesmo tamanho da sequência de referência, preenchida com hífens (representando gaps)
            StringBuilder assembly = new StringBuilder(new string('-', referenceSequence.Length));

            // Itera sobre cada contig alinhado
            foreach (var contigPair in alignedContigs)
            {
                string contigKey = contigPair.Key;
                string contigSequence = contigPair.Value;

                // Obter a posição de início do contig atual (presumindo que a chave do contig corresponde ao índice na lista startPositions)
                int startPosition = startPositions[int.Parse(contigKey.Replace("Contig", "")) - 1];

                // Adiciona o contig na sequência de montagem na posição correta
                int positionIndex = startPosition - 1; // Convertendo para índice base-0

                for (int i = 0; i < contigSequence.Length; i++)
                {
                    // Apenas adiciona o contig se a posição atual não for um gap
                    if (contigSequence[i] != '-')
                    {
                        assembly[positionIndex + i] = contigSequence[i];
                    }
                }
            }
            // Retorna a sequência de montagem como uma string
            return assembly.ToString();

        }

        private void UpdateUIWithAlignmentAndAssembly(SequenceViewModel viewModel, Dictionary<string, string> alignedContigs, List<int> startPositions, string referenceSequence)
        {
            viewModel.ReferenciaAlinhamentoCelulas.Clear();
            foreach (char letra in referenceSequence)
            {
                viewModel.ReferenciaAlinhamentoCelulas.Add(new Aligment { Letra = letra.ToString(), CorDeFundo = Brushes.White });
            }

            viewModel.Contigs.Clear();
            foreach (var contigIndex in Enumerable.Range(0, alignedContigs.Count))
            {
                var contigPair = alignedContigs.ElementAt(contigIndex);
                var contigViewModel = new ContigViewModel { Id = contigPair.Key };

                int penalty = contigPair.Value.StartsWith("-") ? 2 : 1;
                int startPosition = startPositions[contigIndex] - penalty;
                startPosition = Math.Max(startPosition, 0); // Evitar índices negativos

                for (int i = 0; i < referenceSequence.Length; i++)
                {
                    Brush corDeFundo = Brushes.LightGray; // Default para posições fora do contig
                    char contigChar = '-';

                    if (i >= startPosition && i < startPosition + contigPair.Value.Length)
                    {
                        contigChar = contigPair.Value[i - startPosition];
                        if (contigChar == '-') // Se for um gap, colorir de amarelo
                        {
                            corDeFundo = Brushes.Orange;
                        }
                        else if (i < referenceSequence.Length) // Se não for um gap, verificar a correspondência
                        {
                            corDeFundo = contigChar == referenceSequence[i] ? Brushes.LightGreen : Brushes.LightCoral;
                        }
                    }

                    contigViewModel.Aligments.Add(new Aligment { Letra = contigChar.ToString(), CorDeFundo = corDeFundo });
                }

                viewModel.Contigs.Add(contigViewModel);
            }

            // Atualizar a sequência de montagem, se necessário
            viewModel.AssemblySequence = GenerateAssemblyText(referenceSequence, alignedContigs, startPositions);
        }

    }
}


