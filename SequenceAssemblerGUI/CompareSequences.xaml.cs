using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SequenceAssemblerGUI
{
    public partial class CompareSequences : Window
    {
        public CompareSequences()
        {
            InitializeComponent();
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            var sequenceA = TextBoxSequenceA.Text;
            var sequenceB = TextBoxSequenceB.Text;

            if (!int.TryParse(TextBoxMaxGaps.Text, out int maxGaps) || maxGaps < 0)
            {
                MessageBox.Show("Max Gaps must be a positive integer.");
                return;
            }

            if (!int.TryParse(TextBoxGapPenalty.Text, out int gapPenalty))
            {
                MessageBox.Show("Gap Penalty must be an integer.");
                return;
            }

            bool ignoreILDifference = CheckBoxIgnoreILDifference.IsChecked == true;

            var (alignmentA, alignmentB, score) = AlignSequencesWithFilters(sequenceA, sequenceB, maxGaps, gapPenalty, ignoreILDifference);

            TextBoxResults.Text = $"A: {alignmentA}\nB: {alignmentB}\nScore: {score}";
        }

        private (string alignmentA, string alignmentB, int score) AlignSequencesWithFilters(string sequenceA, string sequenceB, int maxGaps, int gapPenalty, bool ignoreILDifference)
        {
            var alignmentA = new StringBuilder();
            var alignmentB = new StringBuilder();
            int score = 0;
            int gaps = 0;

            int i = 0, j = 0;
            while (i < sequenceA.Length && j < sequenceB.Length)
            {
                bool isMatch = sequenceA[i] == sequenceB[j] || (ignoreILDifference && (sequenceA[i] == 'I' && sequenceB[j] == 'L' || sequenceA[i] == 'L' && sequenceB[j] == 'I'));

                if (isMatch)
                {
                    alignmentA.Append(sequenceA[i]);
                    alignmentB.Append(sequenceB[j]);
                    score++; // Incrementa a pontuação para cada correspondência
                    i++; j++;
                }
                else if (gaps < maxGaps) // Permite inserir gaps se não atingiu o limite
                {
                    // Insere gap baseado na comparação do comprimento das sequências
                    if (sequenceA.Length > sequenceB.Length)
                    {
                        alignmentA.Append(sequenceA[i]);
                        alignmentB.Append("-");
                        i++;
                    }
                    else
                    {
                        alignmentA.Append("-");
                        alignmentB.Append(sequenceB[j]);
                        j++;
                    }
                    gaps++;
                    score += gapPenalty; // Aplica penalidade por gap
                }
                else
                {
                    // Para o alinhamento se atingiu o número máximo de gaps permitidos
                    break;
                }
            }

            // Completa o alinhamento para o restante da sequência mais longa após alcançar o limite de gaps
            while (i < sequenceA.Length) { alignmentA.Append(sequenceA[i++]); alignmentB.Append("-"); }
            while (j < sequenceB.Length) { alignmentA.Append("-"); alignmentB.Append(sequenceB[j++]); }

            return (alignmentA.ToString(), alignmentB.ToString(), score);
        }
    }
}
