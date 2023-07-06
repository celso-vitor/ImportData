using SequenceAssemblerLogic;
using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{


    public static void Main()
    {
        // Inicializa uma lista de sequências de strings
        List<string> trypsina = new() { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" };
        List<string> pepsina = new() { "a", "b", "c", "d", "e", "f", "g", "h", "i", "l", "m", "n", "o" };
        List<string> lysc = new() { "f", "g", "h", "i" };
        List<string> v3 = new() { "r", "s", "t", "u", "v" };
        List<string> micro = new() { "i", "z" };

        // Coloca todas as listas em uma lista de listas
        Dictionary<string, List<string>> listOfLists = new Dictionary<string, List<string>>
        {
            {"trypsina", trypsina},
            {"pepsina", pepsina},
            {"lysc", lysc},
            {"v3", v3},
            {"micro", micro}
        };

        // Encontrar a lista com o maior número de sequências
        string nameOfLargestList = "";
        int maxCount = 0;

        foreach (var entry in listOfLists)
        {
            if (entry.Value.Count > maxCount)
            {
                maxCount = entry.Value.Count;
                nameOfLargestList = entry.Key;
            }
        }

        List<string> largestList = listOfLists[nameOfLargestList];

        Console.WriteLine($"{nameOfLargestList} = {string.Join(", ", largestList)}");

        // Encontrar as sequências que não estão presentes na maior lista e guardar em uma lista auxiliar
        List<Tuple<string, List<string>>> complementarySequences = new List<Tuple<string, List<string>>>();

        foreach (var entry in listOfLists)
        {
            if (entry.Key != nameOfLargestList)
            {
                var complementary = entry.Value.Except(largestList).ToList();
                complementarySequences.Add(Tuple.Create(entry.Key, complementary));
            }
        }

        // Ordenar as sequências complementares por quantidade de elementos
        complementarySequences.Sort((x, y) => y.Item2.Count.CompareTo(x.Item2.Count));

        // Exibir as sequências complementares ordenadas
        foreach (var entry in complementarySequences)
        {
            Console.WriteLine($"{entry.Item1} = {string.Join(", ", entry.Item2)}");
        }
    }
}