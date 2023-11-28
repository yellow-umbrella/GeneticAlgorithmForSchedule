using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithm
{
    using static GeneticAlgorithm.GenAlg;
    using Schedule = Dictionary<Tuple<int, int>, List<GenAlg.Lesson>>;
    internal class GenAlg
    {
        public struct Lesson
        {
            public string professor;
            public string group;
            public string subject;
        }

        public class Individual : IComparable<Individual>
        {
            public Schedule schedule;
            public int score;

            public Individual()
            {
                schedule = new Schedule();
            }

            public Individual(Individual other)
            {
                schedule = new Schedule();
                for (int i = 0; i < other.schedule.Count; i++)
                {
                    schedule.Add(
                        new Tuple<int, int>(other.schedule.Keys.ElementAt(i).Item1, 
                                            other.schedule.Keys.ElementAt(i).Item2),
                        new List<Lesson>(other.schedule.Values.ElementAt(i))
                    );
                }
                score = other.score;
            }

            public int CompareTo(Individual other)
            {
                if (this.score < other.score)
                {
                    return -1;
                } else if (this.score > other.score)
                {
                    return 1;
                }
                return 0;
            }
        }


        Dictionary<string, Dictionary<string,int>> group2sub;
        Dictionary<string, int> prof2time;
        Dictionary<string, List<string>> sub2prof;

        List<Individual> curPopulation;

        int days;
        int maxLessonsPerDay;
        
        const int populationCount = 30;
        const int eliteCount = 5;
        const int crossoverCount = populationCount - eliteCount;
        const double mutationChance = .1;

        const int subDiffMult = 1;
        const int profDiffMult = 1;
        const int groupOverlayMult = 10;
        const int profOverlayMult = 10;


        Random rand;

        public GenAlg(int days, int maxLessonsPerDay, Dictionary<string, Dictionary<string, int>> group2sub, 
            Dictionary<string, int> prof2time, Dictionary<string, List<string>> sub2prof)
        {
            this.days = days;
            this.maxLessonsPerDay = maxLessonsPerDay;

            this.group2sub = group2sub;
            this.prof2time = prof2time;
            this.sub2prof = sub2prof;

            rand = new Random();

            curPopulation = new List<Individual>();

            GenFirstPopulation();
        }
        
        
        public void NextPopulation()
        {
            for (int i = 0; i < curPopulation.Count; i++)
            {
                curPopulation[i].score = CalculateScore(curPopulation[i].schedule);
            }
            curPopulation.Sort();

            var nextPopulation = new List<Individual>();

            for (int i = 0; i < eliteCount; i++)
            {
                nextPopulation.Add(new Individual(curPopulation[i]));
            }

            for (int i = 0; i < crossoverCount; i++)
            {
                var parents = ChooseParents();
                nextPopulation.Add(Crossover(parents.Item1, parents.Item2));
            }

            curPopulation = nextPopulation;
        }

        private Tuple<int, int> ChooseParents()
        {
            int ind1 = 0;
            int ind2 = 0;

            for (int i = 0; i < populationCount; i++)
            {
                curPopulation[i].score = populationCount - i;
            }

            double sum = populationCount * (populationCount - 1) / 2;

            double p1 = rand.NextDouble() * sum;
            double p2 = (p1 + sum / 2) % sum;

            double t = 0;

            for (int i = 0; i < populationCount; i++)
            {
                double newT = t + curPopulation[i].score;
                if (t <= p1 && p1 <= newT)
                {
                    ind1 = i;
                }
                if (t <= p2 && p2 <= newT)
                {
                    ind2 = i;
                }
                t = newT;
            }
            return new Tuple<int, int>(ind1, ind2);
        }

        private Individual Crossover(int ind1, int ind2)
        {
            var p1 = curPopulation[ind1].schedule;
            var p2 = curPopulation[ind2].schedule;

            int n = days * maxLessonsPerDay;

            int pos1 = rand.Next(n);
            int pos2 = rand.Next(n);

            Schedule newSchedule = new Schedule();

            for (int i = 0; i < n; i++)
            {
                Tuple<int, int> pos = new Tuple<int, int>(p1.Keys.ElementAt(i).Item1, p1.Keys.ElementAt(i).Item2);
                if (i < Math.Min(pos1, pos2) ||  i > Math.Max(pos1, pos2))
                {
                    newSchedule.Add(
                        pos,
                        new List<Lesson>(p1.Values.ElementAt(i))
                    );
                } else
                {
                    newSchedule.Add(
                        pos,
                        new List<Lesson>(p2.Values.ElementAt(i))
                    );
                }
            }

            newSchedule = Mutate(newSchedule);

            var ans = new Individual();
            ans.schedule = newSchedule;
            ans.score = CalculateScore(newSchedule);

            return ans;
        }

        private Schedule Mutate(Schedule schedule)
        {
            foreach (var time in schedule)
            {
                if (rand.NextDouble() < mutationChance)
                {
                    if (time.Value.Count == 0)
                    {
                        schedule[time.Key].Add(ChooseGroup());
                    } else
                    {
                        int ind = rand.Next(time.Value.Count);
                        var lesson = schedule[time.Key][ind];

                        double r = rand.NextDouble();
                        if (r < 0.25)
                        {
                            schedule[time.Key][ind] = ChooseProfessor(lesson);
                        } else if (r < 0.5)
                        {
                            schedule[time.Key][ind] = ChooseSubject(lesson);
                        } else if (r < 0.75)
                        {
                            schedule[time.Key][ind] = ChooseGroup();
                        } else
                        {
                            schedule[time.Key].RemoveAt(ind);
                        }
                    }
                }
            }

            return schedule;
        }
        
        private int CalculateScore(Schedule schedule)
        {
            var cur_group2sub = new Dictionary<string, Dictionary<string, int>>();
            var cur_prof2time = new Dictionary<string, int>();
            int overlaysProf = 0;
            int overlaysGroup = 0;

            foreach (var item in schedule)
            {
                var lessons = item.Value;
                var usedProfessors = new HashSet<string>();
                var usedGroups = new HashSet<string>();
                lessons.ForEach(lesson =>
                {
                    if (usedGroups.Contains(lesson.group))
                    {
                        overlaysGroup++;
                    }
                    else
                    {
                        cur_group2sub.TryAdd(lesson.group, new Dictionary<string, int>());
                        cur_group2sub[lesson.group].TryAdd(lesson.subject, 0);
                        cur_group2sub[lesson.group][lesson.subject]++;
                        usedGroups.Add(lesson.group);
                    }

                    if (usedProfessors.Contains(lesson.professor))
                    {
                        overlaysProf++;
                    } else
                    {
                        cur_prof2time.TryAdd(lesson.professor, 0);
                        cur_prof2time[lesson.professor]++;
                        usedProfessors.Add(lesson.professor);
                    }
                });
            }

            int differenceSubjectTime = 0;
            int differenceProfessorTime = 0;

            foreach(var item in group2sub)
            {
                foreach(var subject in item.Value)
                {
                    cur_group2sub.TryAdd(item.Key, new Dictionary<string, int>());
                    cur_group2sub[item.Key].TryAdd(subject.Key, 0);

                    differenceSubjectTime += Math.Abs(cur_group2sub[item.Key][subject.Key] - subject.Value);
                }
            }

            foreach (var professor in prof2time)
            {
                cur_prof2time.TryAdd(professor.Key, 0);
                differenceProfessorTime += Math.Abs(cur_prof2time[professor.Key] - professor.Value);
            }

            return profDiffMult * differenceProfessorTime 
                    + subDiffMult * differenceSubjectTime 
                    + profOverlayMult * overlaysProf 
                    + groupOverlayMult * overlaysGroup;
        }

        
        // generating schedule and its parts
        private void GenFirstPopulation()
        {
            for (int i = 0; i < populationCount; i++)
            {
                var ind = new Individual();
                ind.schedule = GenSchedule();
                ind.score = CalculateScore(ind.schedule);
                curPopulation.Add(ind);
            }
        }
        
        private Schedule GenSchedule()
        {
            Schedule schedule = new Schedule();

            for (int day = 0; day < days; day++)
            {
                for (int hour = 0; hour < maxLessonsPerDay; hour++)
                {
                    var time = new Tuple<int, int>(day, hour);

                    int n = group2sub.Count;
                    int chooseN = rand.Next(n + 1);

                    schedule[time] = new List<Lesson>();

                    for (int i = 0; i < n; i++)
                    {
                        if (rand.Next(n - i) < chooseN)
                        {
                            chooseN--;

                            Lesson les = new Lesson();
                            les.group = group2sub.Keys.ElementAt(i);
                            //var subjects = group2sub[les.group];

                            /*
                            // choose random subject from their subjects 
                            int index = rand.Next(subjects.Count);
                            les.subject = subjects.Keys.ElementAt(index);

                            // choose random professor for this subject
                            index = rand.Next(sub2prof[les.subject].Count);
                            les.professor = sub2prof[les.subject][index];
                            */

                            schedule[time].Add(ChooseSubject(les));
                        }

                    }
                }
            }
            return schedule;
        }

        private Lesson ChooseProfessor(Lesson lesson)
        {
            int ind = rand.Next(sub2prof[lesson.subject].Count);
            lesson.professor = sub2prof[lesson.subject][ind];
            return lesson;
        }

        private Lesson ChooseSubject(Lesson lesson)
        {
            int ind = rand.Next(group2sub[lesson.group].Count);
            lesson.subject = group2sub[lesson.group].Keys.ElementAt(ind);
            return ChooseProfessor(lesson);
        }

        private Lesson ChooseGroup()
        {
            Lesson lesson = new Lesson();
            int ind = rand.Next(group2sub.Count);
            lesson.group = group2sub.Keys.ElementAt(ind);
            return ChooseSubject(lesson);
        }
        
        
        // writing to files
        public void WritePopulation(string path)
        {
            WritePopulation(curPopulation, path);
        }

        public void WritePopulation(List<Individual> population, string path)
        {
            for (int i = 0; i < curPopulation.Count; i++)
            {
                curPopulation[i].score = CalculateScore(curPopulation[i].schedule);
            }
            curPopulation.Sort();

            File.WriteAllText(path, "\n");

            foreach (var ind in population)
            {
                WriteSchedule(ind, path);
            }
        }

        public void WriteSchedule(Individual ind, string path)
        {
            using (StreamWriter outputFile = new StreamWriter(path, true))
            {
                outputFile.WriteLine("----------------");
                outputFile.WriteLine("Score: {0}", ind.score);
                foreach (var lesson in ind.schedule)
                {
                    outputFile.WriteLine("{0}.{1} : ", lesson.Key.Item1 + 1, lesson.Key.Item2 + 1);
                    lesson.Value.ForEach(x => outputFile.WriteLine("{0} - {1} - {2}",
                                            x.group, x.subject, x.professor));
                }
                outputFile.WriteLine("----------------");
            }
        }

        public void WriteSummary(Individual ind, string path)
        {
            File.WriteAllText(path, "Summary\n");
            using (StreamWriter outputFile = new StreamWriter(path, true))
            {
                outputFile.WriteLine("----------------");
                outputFile.WriteLine("Score: {0}", ind.score);
                var cur_group2sub = new SortedDictionary<string, SortedDictionary<string, int>>();
                var cur_prof2time = new SortedDictionary<string, int>();
                int overlaysProf = 0;
                int overlaysGroup = 0;
                foreach (var item in ind.schedule)
                {
                    var lessons = item.Value;
                    var usedProfessors = new HashSet<string>();
                    var usedGroups = new HashSet<string>();
                    lessons.ForEach(lesson =>
                    {
                        // check for every group hours for their subjects
                        if (usedGroups.Contains(lesson.group))
                        {
                            overlaysGroup++;
                        }
                        else
                        {
                            cur_group2sub.TryAdd(lesson.group, new SortedDictionary<string, int>());
                            cur_group2sub[lesson.group].TryAdd(lesson.subject, 0);
                            cur_group2sub[lesson.group][lesson.subject]++;
                            usedGroups.Add(lesson.group);
                        }

                        // check for professors hours and if they have two subjects at one time
                        if (usedProfessors.Contains(lesson.professor))
                        {
                            overlaysProf++;
                        }
                        else
                        {
                            cur_prof2time.TryAdd(lesson.professor, 0);
                            cur_prof2time[lesson.professor]++;
                            usedProfessors.Add(lesson.professor);
                        }
                    });
                }

                foreach (var req in cur_group2sub)
                {
                    outputFile.WriteLine("{0}:", req.Key);
                    foreach (var x in req.Value)
                        outputFile.WriteLine("{0} : {1}", x.Key, x.Value);
                    outputFile.WriteLine();
                }

                foreach (var req in cur_prof2time)
                {
                    outputFile.WriteLine("{0} - {1}", req.Key, req.Value);
                }
                outputFile.WriteLine("Overlays for professors: {0}", overlaysProf);
                outputFile.WriteLine("Overlays for groups: {0}", overlaysGroup);
                outputFile.WriteLine("----------------");
            }
        }

        public Individual ChooseBest()
        {
            for (int i = 0; i < curPopulation.Count; i++)
            {
                curPopulation[i].score = CalculateScore(curPopulation[i].schedule);
            }
            curPopulation.Sort();
            return curPopulation[0];
        }
    }
}
