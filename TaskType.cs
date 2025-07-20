using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Type of a task used to call Rules and tell them there are task instances ready. 
    /// </summary>
    class TaskType
    {
        /// <summary>
        /// Rules for which this type of task is the main task type. 
        /// </summary>
        public List<Rule> MainRules = new List<Rule>();
        /// <summary>
        /// List of rules that contain this type of task as subtask. 
        /// </summary>
        public List<Rule> Rules;
        public String Name; //Task name
        public int NumOfVariables; // Number of variables. So that load(X,Y) and load(X,Y,Z) are different TaskTypes
        public HashSet<Task> Instances;
        public int MinTaskLength;
        private object goalRule;

        /// <summary>
        /// If true then this is either root task or some task to which root task decomposes.
        /// If this is false, it is a separate rule/task, so it won't help me get desired root task. Therefor I can remove this rule. 
        /// </summary>
        internal bool reachable = false;

        public TaskType(String name, int numOfVars)
        {
            this.Name = name;
            this.NumOfVariables = numOfVars;
            this.Instances = new HashSet<Task>();
            this.Rules = new List<Rule>();
            MinTaskLength = 100000;
        }

        public TaskType(String name, int numOfVars, HashSet<Task> instances, List<Rule> rules)
        {
            this.Rules = rules;
            this.Instances = instances;
            this.Name = name;
            this.NumOfVariables = numOfVars;
            MinTaskLength = 100000;
        }

        public TaskType(string name, int numOfVars, HashSet<Task> instances, List<Rule> rules, object goalRule) : this(name, numOfVars, instances, rules)
        {
            this.goalRule = goalRule;
        }

        internal void RemoveRule(Rule r)
        {
            if (MainRules.Contains(r)) MainRules.Remove(r);
            if (Rules.Contains(r)) Rules.Remove(r);
        }

        /// <summary>
        /// Sets mintask length to i if i is smaller than mintask length otherwise does nothing.
        /// Return true if value changed.
        /// </summary>
        /// <param name="i"></param>
        public bool SetMinTaskLengthIfSmaller(int i)
        {
            if (i < MinTaskLength)
            {
                MinTaskLength = i;
                return true;
            }
            return false;
        }

        public void AddRule(Rule r)
        {
            this.Rules.Add(r);
        }

        public void AddInstance(Task t)
        {
            Instances.Add(t); // Because this is a hashset it automatically wont allow duplications. 
        }

        /// <summary>
        /// Tells the rules that this task is now ready. If the rule is full (all tasks are ready it returns it otherwise returns null)
        /// </summary>
        /// <returns></returns>
        private Rule ActivateRule(Rule r, int i, int CreationNumber)
        {
            bool fullyActivated = r.Activate(this, i, CreationNumber);
            if (fullyActivated) return r;
            else return null;
        }

        /// <summary>
        /// Tells the rules that this task is now ready. If the rules are full (all tasks are ready it returns them otherwise returns empty list)        /// 
        /// 
        /// </summary>
        /// <returns></returns>
        public HashSet<Rule> ActivateRules(int CreationNumber)
        {
            int instancesCount = Instances.Count;
            HashSet<Rule> rulesReadyToGo = new HashSet<Rule>();
            foreach (Rule r in Rules)
            {
                Rule r2 = ActivateRule(r, instancesCount, CreationNumber);
                if (r2 != null) rulesReadyToGo.Add(r2);
            }
            return rulesReadyToGo;
        }

        public override string ToString()
        {
            string text = string.Join(",", Rules.Select(x => x.MainTaskType.Name));
            string text2 = string.Join(",", Instances.Select(x => x.TaskInstance.Name));
            String s = "TaskType:" + this.Name + " Num of Variables " + NumOfVariables + " Rules: " + text + " Instances: " + text2;
            return s;
        }

        /// <summary>
        /// Marks itself and every main rule as reached.
        /// </summary>
        internal void MarkAsReached(int i)
        {
            if (!reachable)
            {
                reachable = true;
                foreach (Rule r in MainRules)
                {
                    r.MarkAsReached(i);
                }

            }
            reachable = true;
        }
    }
}
