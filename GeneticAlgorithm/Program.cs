using GeneticAlgorithm;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;


string docPath;
bool randInp = false;

if (Console.ReadLine() == "r")
{
    randInp = true;
}

int maxLessonsPerDay = 4;
int days = 5;
int maxWeekLoad = days * maxLessonsPerDay;

Dictionary<string, Dictionary<string, int>> group2sub = new Dictionary<string, Dictionary<string, int>>();
Dictionary<string, int> prof2time = new Dictionary<string, int>();
Dictionary<string, List<string>> sub2prof = new Dictionary<string, List<string>>();

if (randInp)
{
    ReqInput.GetRandInput(group2sub, prof2time, sub2prof, 7, 7, 5, maxWeekLoad);
    docPath = "PopulationsRand\\";
} else
{
    ReqInput.GetSolvInp(group2sub, prof2time, sub2prof);
    docPath = "PopulationsSolv\\";
}

if (!Directory.Exists(docPath))
{
    Directory.CreateDirectory(docPath);
}

File.WriteAllText(docPath + "Requirements.txt", "Requirements\n");
using (StreamWriter outputFile = new StreamWriter(docPath + "Requirements.txt", true))
{
    foreach (var req in group2sub)
    {
        outputFile.WriteLine("{0}:", req.Key);
        foreach (var x in req.Value)
            outputFile.WriteLine("{0} : {1}", x.Key, x.Value);
        outputFile.WriteLine();
    }

    foreach (var req in prof2time)
    {
        outputFile.WriteLine("{0} - {1}", req.Key, req.Value);
    }

    outputFile.WriteLine();

    foreach (var req in sub2prof)
    {
        outputFile.WriteLine("{0}:", req.Key);
        req.Value.ForEach(j => outputFile.WriteLine("\t{0}", j));
        outputFile.WriteLine();
    }

}



var genAlg = new GenAlg(days, maxLessonsPerDay, group2sub, prof2time, sub2prof);
genAlg.WritePopulation(docPath + "Population_0.txt");

int popCount;
if (randInp)
{
    popCount = 10000;
    for (int i = 1; i <= popCount; i++)
    {
        genAlg.NextPopulation();
        if (i % 1000 == 0)
        {
            genAlg.WritePopulation(docPath + "Population_" + i.ToString() + ".txt");
        }
    }
} else
{
    int i = 0;
    while (genAlg.ChooseBest().score != 0)
    {
        i++;
        genAlg.NextPopulation();
        genAlg.WritePopulation(docPath + "Population_" + i.ToString() + ".txt");
    }
    popCount = i;
}

genAlg.WriteSummary(genAlg.ChooseBest(), docPath + "Summary.txt");
File.AppendAllText(docPath + "Summary.txt", popCount.ToString());
