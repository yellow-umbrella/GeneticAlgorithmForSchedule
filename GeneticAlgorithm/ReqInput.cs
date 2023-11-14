using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithm
{
    internal class ReqInput
    {
        static public void GetRandInput(Dictionary<string, Dictionary<string, int>> group2sub,
            Dictionary<string, int> prof2time, Dictionary<string, List<string>> sub2prof,
            int groupN, int subN, int profN, int maxWeekLoad)
        {
            List<string> subjects = new List<string>();
            List<string> groups = new List<string>();
            List<string> professors = new List<string>();

            for (int i = 1; i <= groupN; i++)
            {
                groups.Add("g" + i.ToString());
            }
            for (int i = 1; i <= subN; i++)
            {
                subjects.Add("sub" + i.ToString());
            }
            for (int i = 1; i <= profN; i++)
            {
                professors.Add("prof" + i.ToString());
            }

            var rand = new Random();

            foreach (var group in groups)
            {
                group2sub.Add(group, new Dictionary<string, int>());
                foreach (var subject in subjects)
                {
                    int time = rand.Next(maxWeekLoad / subjects.Count + 2);
                    if (time > 0)
                    {
                        group2sub[group][subject] = time;
                    }
                }
            }

            foreach (var professor in professors)
            {
                prof2time.Add(professor, rand.Next(1, maxWeekLoad + 1));
            }

            foreach (var subject in subjects)
            {
                int n = professors.Count;
                int chooseN = rand.Next(1, n + 1);

                sub2prof[subject] = new List<string>();

                for (int i = 0; i < n; i++)
                {
                    if (rand.Next(n - i) < chooseN)
                    {
                        chooseN--;
                        sub2prof[subject].Add(professors[i]);
                    }
                }
            }
        }
    
        
        static public void GetSolvInp(Dictionary<string, Dictionary<string, int>> group2sub,
            Dictionary<string, int> prof2time, Dictionary<string, List<string>> sub2prof)
        {
            group2sub.Add("g1", new Dictionary<string, int>()
            {
                {"s1", 3},
                {"s2", 4}
            });
            
            group2sub.Add("g2", new Dictionary<string, int>()
            {
                {"s1", 4},
                {"s2", 3}
            });

            sub2prof.Add("s1", new List<string>()
            {
                "p1", "p2"
            });
            
            sub2prof.Add("s2", new List<string>()
            {
                "p1", "p2"
            });

            prof2time.Add("p1", 7);
            prof2time.Add("p2", 7);
        }
    
    }
}
