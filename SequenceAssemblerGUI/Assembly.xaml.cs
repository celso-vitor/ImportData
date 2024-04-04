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
            // Atualizar sequência de referência na UI
            foreach (char letra in referenceSequence)
            {
                var corDeFundo = Brushes.White;
                viewModel.ReferenciaAlinhamentoCelulas.Add(new Aligment { Letra = letra.ToString(), CorDeFundo = corDeFundo });
            }

            // Atualizar contigs alinhados na UI
            for (int contigIndex = 0; contigIndex < alignedContigs.Count; contigIndex++)
            {
                var contigPair = alignedContigs.ElementAt(contigIndex);
                var contigViewModel = new ContigViewModel { Id = contigPair.Key };
                int startPosition = startPositions[contigIndex] - 1; // Ajuste para índice base-0

                // Aqui, você deve buscar o resultado do alinhamento para esse contig
                SequenceAligner aligner = new SequenceAligner();
                Alignment alignmentResult = aligner.AlignSequences(referenceSequence, contigPair.Value);

                // Adicionar espaços vazios ou hífens até a posição de início do contig
                for (int pos = 0; pos < startPosition; pos++)
                {
                    contigViewModel.Aligments.Add(new Aligment { Letra = "-", CorDeFundo = Brushes.LightGray });
                }

                // Adiciona as letras do contig com a cor correspondente
                for (int i = 0; i < alignmentResult.AlignedSmallSequence.Length; i++)
                {
                    Brush corDeFundo;

                    // Checa se a posição não é um gap
                    if (alignmentResult.AlignedSmallSequence[i] != '-')
                    {
                        if (alignmentResult.AlignedLargeSequence[i] == alignmentResult.AlignedSmallSequence[i])
                        {
                            corDeFundo = Brushes.LightGreen; // Cor para correspondência
                        }
                        else
                        {
                            corDeFundo = Brushes.LightCoral; // Cor para não correspondência
                        }
                    }
                    else
                    {
                        corDeFundo = Brushes.LightGray; // Cor para gaps
                    }

                    contigViewModel.Aligments.Add(new Aligment { Letra = alignmentResult.AlignedSmallSequence[i].ToString(), CorDeFundo = corDeFundo });
                }

                // Completar o resto da sequência com hífens se necessário
                while (contigViewModel.Aligments.Count < referenceSequence.Length)
                {
                    contigViewModel.Aligments.Add(new Aligment { Letra = "-", CorDeFundo = Brushes.LightGray });
                }

                viewModel.Contigs.Add(contigViewModel);
            }

            // Atualizar a montagem na UI
            viewModel.AssemblySequence = GenerateAssemblyText(referenceSequence, alignedContigs, startPositions);

            // Imprimir alinhamento para depuração
            foreach (var contigViewModel in viewModel.Contigs)
            {
                Console.WriteLine($"Contig: {contigViewModel.Id}");
                foreach (var aligment in contigViewModel.Aligments)
                {
                    Console.Write(aligment.Letra);
                }
                Console.WriteLine(); // Nova linha para separar os contigs
            }
        }
    }
}


