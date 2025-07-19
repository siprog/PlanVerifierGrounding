using PlanValidationExe;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Program that solves the plan validation problem.
/// Author: Simona Ondrčková
/// </summary>
namespace PlanValidation1
{
    class Program
    {
        static void Main(string[] args)
        {
            String planS = "plan.txt";
            String domainS = "domain.lisp";
            String problemS = "problem";

            //To determine whether a user gives path to files or not we check whether the plan file ends with txt. 
            //This means that all plan files must end with txt extension!
            if (args != null && args.Length >= 3 && args[2].Contains("txt"))
            {
                domainS = args[0];
                problemS = args[1];
                planS = args[2];                
            }
            if (ContainsParameter(Globals.NotInterleavingS, args)) Globals.Interleaving = false;
            if (ContainsParameter(Globals.KnownGoalTaskS, args))
            {
                Globals.KnownRootTask = true;                
            }
            if (ContainsParameter(Globals.KnownGoalStateS, args))
            {
                Globals.CheckGoalState= true;
            }
            if (ContainsParameter(Globals.IgnoreCaseArgS, args))
            {
                Globals.IgnoreCase = true;
            }
            if (ContainsParameter(Globals.SometimeBeforeS, args))
            {
                Globals.SometimeBeforeCond = true;
            }
            HandleHeuristicParamaters(args);
            HashSet<Task> everyTask = null;
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();// measures time                        
                Model m = new Model();
                List<Action> plan;
                InputReader reader = new InputReader();
                reader.ReadDomain(domainS);
                m.AllActionTypes = reader.globalActions;
                m.AllConstantTypes = reader.allConstantTypes;
                Rule goalRule = null;
                List<Term> goalState = new List<Term>();
                m.InitialState = reader.ReadProblem(problemS, m.AllConstantTypes, ref reader.allConstants, out goalRule, out goalState);
                m.EmptyRules = reader.emptyRules;
                m.AllConstants = reader.allConstants;
                reader.ReadPlan(planS, m.AllActionTypes, m.AllConstants);
                m.AllTaskTypes = reader.alltaskTypes;
                plan = reader.myActions;
                m.Allrules = reader.allRules;
                PlanValidator planValidator = new PlanValidator();
                int taskCount = 0;
                if (Globals.KnownRootTask) m.Allrules.Add(goalRule);
                if (Globals.KnownRootTask) m.MarkAsReachable(goalRule);
                if (Globals.KnownRootTask) m.RemoveUnreachableTasks();
                SearchHeuristic s = new DistanceToGoalHeuristic(m.Allrules, goalRule);
                HeuristicStructure hS = new HeuristicStructure(s, m.AllTaskTypes.Count() + 1);
                int emptyTask = 0;
                bool isValid = planValidator.IsPlanValid(plan, m.AllTaskTypes, m.InitialState, m.AllConstants.Values.ToList(), m.EmptyRules, out taskCount, goalRule, goalState, watch, hS, out everyTask, out emptyTask);
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                if (isValid) Console.WriteLine("Plan  is valid. It took {0} s.Plan Length {1}. Program generated {2} tasks.Current Heuristic is {3}", elapsedMs / 1000f, plan.Count, taskCount, Globals.Heuristic);
                else Console.WriteLine("Plan  is invalid. It took {0} s. Plan Length {1}. Program generated {2} tasks.Current Heuristic is {3}", elapsedMs / 1000f, plan.Count, taskCount, Globals.Heuristic);
                RunAnalysis(everyTask, m.AllTaskTypes.Count + 1);
                Console.WriteLine("Empty Tasks are {0}", emptyTask);
            }
            catch (ActionException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Plan is invalid. ");
            }
            
            Console.ReadKey();
        }

        private static void RunAnalysis(HashSet<Task> everyTask, int max)
        {
            int isSubtaskSomewhere=0;
            int[] depthArray = new int[max];
            int[] subtaskSomewherearray = new int[max];
            foreach(Task t in everyTask)
            {
                if (t.isSubtaskSomewhere)
                {
                    isSubtaskSomewhere++;
                   if (t.distancetoGoalTask!=-1) subtaskSomewherearray[t.distancetoGoalTask]++;
                }
                if (t.distancetoGoalTask!=-1) depthArray[t.distancetoGoalTask]++;
            }

            for( int i=0;i<depthArray.Count();i++)
            {
                Console.WriteLine("Number of tasks with distance {0} to goal task is {1} and of these thos ethat are someone elses subtasks are {2}", i, depthArray[i], subtaskSomewherearray[i]);
            }
            Console.WriteLine("Number of tasks that are somebodys subtask is {0} and number of those that are not {1}", isSubtaskSomewhere,everyTask.Count()-isSubtaskSomewhere);
        }

        public static bool ContainsParameter(string s, string[] args)
        {
            if (args == null || args.Count() == 0) return false;
            for (int i = 0; i < args.Count(); i++)
            {
                if (string.Equals(args[i], s, StringComparison.CurrentCultureIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// If there is an agrument given about heuristics this wil handleit and set the correct heurstic. 
        /// The argument for heuristic looks like this h+number. NUmbe ris based on descrptiion in Globals
        /// 0=most parameters
        /// 1= least parameters
        /// 2= original
        /// 3= instances (default)
        /// </summary>
        /// <returns></returns>
        public static void HandleHeuristicParamaters(string[] args)
        {
            foreach (int i in Enum.GetValues(typeof(Globals.Heuristics)))
            {
                if (ContainsParameter("h" + i, args)) Globals.Heuristic = (Globals.Heuristics)i;
            }
        }
    }
}
