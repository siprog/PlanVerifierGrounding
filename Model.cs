using PlanValidation1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanValidationExe
{
    class Model
    {
        internal List<Term> InitialState { get; set; }
        internal List<Rule> EmptyRules { get; set; }
        internal Dictionary<String, Constant> AllConstants { get; set; }
        internal Dictionary<String, List<TaskType>> AllTaskTypes { get; set; }
        internal List<Rule> Allrules { get; set; }
        internal List<ActionType> AllActionTypes { get; set; }
        internal Dictionary<String, ConstantType> AllConstantTypes { get; set; }

        /// <summary>
        /// Marks all task and rules that can be reached from decomposition of goal rule. 
        /// This does not mark actionTypes. But that is not necessary as when we create actions, we look at tasktypes and their reachability.   
        /// </summary>
        /// <param name="goalRule"></param>
        internal void MarkAsReachable(Rule goalRule)
        {
            //This will mark the root rule and it's main subtask as reached. Then the goal rule will mark it's subtasks and those recursively their rules and then their subtasks. 
            goalRule.MarkAsReached();
        }

        internal void RemoveUnreachableTasks()
        {
            List<Rule> deleteRules = new List<Rule>();
            foreach (Rule r in Allrules)
            {
                if (!r.reachable)
                {
                    //This lets the rule know it will be removed. So the rule will tell it's main task and all subtasks to remove it from their lists of rules. So we remove two pointers to this rule. 
                    //The last pointer goes from allrules so this one will be deleted after. 
                    r.NotifyOfRemoval();
                    deleteRules.Add(r);
                }
            }
            foreach (Rule r in deleteRules)
            {
                Allrules.Remove(r);
            }
            deleteRules = null;
            //At this point the rules in delete rules are unreachable so the garbage collector can remove them.
            //Tasks have removed connection to their deleted rules hoewever they still have to be deleted. 
            List<TaskType> deleteTasks = new List<TaskType>();
            foreach (List<TaskType> list in AllTaskTypes.Values)
            {
                foreach (TaskType t in list)
                {
                    if (!t.reachable)
                    {
                        deleteTasks.Add(t);
                    }
                }
            }
            foreach (TaskType t in deleteTasks)
            {
                AllTaskTypes[t.Name].Remove(t);
            }
            deleteTasks = null;
        }
    }
}
