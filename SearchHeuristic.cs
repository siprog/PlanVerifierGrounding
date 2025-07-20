using PlanValidation1;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanValidationExe
{
    /// <summary>
    /// Search heuristic is a heuristic that is used for picking the next rule to create tasks. 
    /// </summary>
    abstract class SearchHeuristic
    {
        /// <summary>
        /// This returns the heuristic value of the rule. The lower the value the more important the rule is and the sooner it should be picked.  
        /// </summary>
        public abstract int GetValue(Rule r);
    }


    /// <summary>
    /// Finds distance to goal task. 
    /// If there is no goal task then finds distance to any top task. 
    /// If there are multiple paths an argument determines whether we use min or max. 
    /// </summary>
    class DistanceToGoalHeuristic : SearchHeuristic
    {

        private Rule GoalRule;

        /// <summary>
        /// This is used so we only calculate the rules once.
        /// This should not be a memory problem as there are typically not that many different rules. 
        /// Be careful about this with grounding. 
        /// </summary>
        private Dictionary<Rule, int> rules;

        public override int GetValue(Rule r)
        {
            if (Globals.KnownRootTask) return r.minDistanceToGoalTask;
            int val = 100000;
            rules.TryGetValue(r, out val);
            return val;
        }


        /// <summary>
        /// Intitialize Heuristic. This will already calculate the heuristic value for each rule. 
        /// </summary>
        /// <param name="allRules"></param>
        /// <param name="goalRule"></param>
        /// <param name=""></param>
        public DistanceToGoalHeuristic(List<Rule> allRules, Rule goalRule)
        {
            GoalRule = goalRule;
            rules = new Dictionary<Rule, int>();
            CalculateDistance(allRules);


        }

        /// <summary>
        /// Use minimum to goal distance heuristics. Cannot use maximum because of this:
        /// two rules recursive to one another:
        /// A->B,C and B->A,D because A asks B what is your depth and then B asks A what is your depth and it cycles. This is also why you can never use max for distance to goal heuristic. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="dis"></param>
        private void PropagateDown(Rule r, int dis)
        {
            foreach (TaskType t in r.TaskTypeArray)
            {
                foreach (Rule child in t.MainRules)
                {
                    if (!rules.ContainsKey(child))
                    {
                        rules.Add(child, dis + 1);
                        PropagateDown(child, dis + 1);
                    }
                    else if (rules[child] > dis + 1)
                    {
                        rules[child] = dis + 1;
                        PropagateDown(child, dis + 1);
                    }
                }
            }
        }


        private void CalculateDistance(List<Rule> allRules)
        {

            List<Rule> topRules = new List<Rule>();
            /*if goal task is known al of this is already calculated in the reachability section*/
            if (!Globals.KnownRootTask)
            {
                topRules = allRules.Where(x => x.MainTaskType.Rules == null || x.MainTaskType.Rules.Count == 0).ToList();
                foreach (Rule top in topRules)
                {
                    rules.Add(top, 0);
                    PropagateDown(top, 0);
                }
            }
        }

    }
}