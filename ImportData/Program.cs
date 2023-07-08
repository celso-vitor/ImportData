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

        List<(string ID, int Gain)> sequenceGains = Useful.GenerateOrderedGains(listOfLists);

        // Imprimir os ganhos ordenados
        foreach (var entry in sequenceGains)
        {
            Console.WriteLine($"ID: {entry.ID}, Gain: {entry.Gain}");
        }
    }
}