using PlanValidationExe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Represents a partial step between creating a task from a rule.
    /// Has all main variables of rule and subtasks filled. Creates proper conditions (with actual variables not number references).
    /// </summary>
    class RuleInstance
    {
        public Task MainTask;
        public List<Task> Subtasks;

        /// <summary>
        /// Term is the condition and the int says to which subtask is this related to. (Counted from 0)
        /// </summary>
        public List<Tuple<int, Term>> PosPreConditions { get; }
        public List<Tuple<int, Term>> NegPreConditions { get; }
        public List<Tuple<int, Term>> PosPostConditions { get; }
        public List<Tuple<int, Term>> NegPostConditions { get; }
        public List<Tuple<int, int, Term>> PosBetweenConditions { get; }
        public List<Tuple<int, int, Term>> NegBetweenConditions { get; }

        readonly bool valid = true;

        public RuleInstance(Task mainTask, List<Task> subtasks, Rule rule, List<String> allVars, List<Constant> allconstants)
        {
            this.MainTask = mainTask;
            this.Subtasks = subtasks;           
            PosPreConditions = new List<Tuple<int, Term>>();
            NegPreConditions = new List<Tuple<int, Term>>();
            PosPostConditions = new List<Tuple<int, Term>>();
            NegPostConditions = new List<Tuple<int, Term>>();
            PosBetweenConditions = new List<Tuple<int, int, Term>>();
            NegBetweenConditions = new List<Tuple<int, int, Term>>();
            if (valid) valid = CreateConditions(rule.posBetweenConditions, PosBetweenConditions, allVars, rule.AllVarsTypes); //These go first as they are most likely to break the rule instance
            if (valid) valid = CreateConditions(rule.negBetweenConditions, NegBetweenConditions, allVars, rule.AllVarsTypes); 
            if (valid) valid = CreateConditions(rule.posPreConditions, PosPreConditions, allVars, rule.AllVarsTypes, true, allconstants);
            if (valid) valid = CreateConditions(rule.negPreConditions, NegPreConditions, allVars, rule.AllVarsTypes, false, allconstants);
            if (valid) valid = CreateConditions(rule.posPostConditions, PosPostConditions, allVars, rule.AllVarsTypes, true, allconstants);
            if (valid) valid = CreateConditions(rule.negPostConditions, NegPostConditions, allVars, rule.AllVarsTypes, false, allconstants);

        }

        public bool IsValid()
        {
            return valid;
        }

        /// <summary>
        /// Checks whether subtasks are properly ordered. 
        /// </summary>
        /// <param name="orderConditions"></param>
        /// <returns></returns>
        private bool CheckOrdering(List<Tuple<int, int>> orderConditions)
        {
            if (orderConditions?.Any() != true) return true; //If there is no ordering than it's ordered properly.
            foreach (Tuple<int, int> combo in orderConditions)
            {
                Task subtask1 = Subtasks[combo.Item1];
                Task subtask2 = Subtasks[combo.Item2];
                if (!(Math.Floor(subtask1.GetEndIndex()) < Math.Ceiling(subtask2.GetStartIndex())))
                {
                    //Why ceiling?
                    //Empty task are at position -0,5. So empty task on position 2 has 1,5
                    //We want to allow this bEc (E is empty task) b and d are normal tasks. b is on position 2, c is on position 3. E must be allowed on position 3(2,5).
                    return false;
                }
            }
            return true;
        }

        public bool CheckEqualityOnly(List<Tuple<int, string, List<int>>> PostConditions1, List<String> allVars, bool pos)
        {
            bool valid = true;
            foreach (var c in PostConditions1)
            {
                if (c.Item2.Contains("equal") || c.Item2.Equals("="))
                {
                    if (pos)
                    {
                        if (allVars[c.Item3[0]] == allVars[c.Item3[1]]) valid = true;
                    }
                    else if (allVars[c.Item3[0]] != allVars[c.Item3[1]]) valid = true;
                }
            }
            return valid;
        }


        private bool CreateConditions(List<Tuple<int, string, List<int>>> PostConditions1, List<Tuple<int, Term>> PostConditions2, List<String> allVars, List<ConstantType> allVarsType, bool pos, List<Constant> allconstants)
        {
            bool valid = true;
            bool containsForallCondition = false;

            foreach (Tuple<int, string, List<int>> conditionTuple in PostConditions1)
            {
                Constant[] newVars = new Constant[conditionTuple.Item3.Count];
                for (int i = 0; i < conditionTuple.Item3.Count; i++)
                {
                    int num = conditionTuple.Item3[i];
                    if (num < 0 || num >= allVars.Count) return false;
                    newVars[i] = new Constant(allVars[num], allVarsType[num]);
                    if (allVars[num].StartsWith("!"))
                    {
                        //This is an forall variable. 
                        //So I must create many instances of this condition. With each constant of desired type. 
                        containsForallCondition = true;
                    }
                }
                Term condition = new Term(conditionTuple.Item2, newVars);
                if (conditionTuple.Item2.Contains("equal") || conditionTuple.Item2.Equals("="))
                {
                    valid = CheckEquality(pos, condition);
                    if (!valid) return false;
                }
                else
                { //We do not add equality conditions to normal conditions. 
                    if (containsForallCondition)
                    {
                        PostConditions2.AddRange(CreateForAllConditions(newVars, allconstants, conditionTuple.Item1, conditionTuple.Item2));
                    }
                    else
                    {
                        Tuple<int, Term> tuple = new Tuple<int, Term>(conditionTuple.Item1, condition);
                        PostConditions2.Add(tuple);
                    }
                }
                containsForallCondition = false;
            }
            return valid;
        }

        private List<Tuple<int, Term>> CreateForAllConditions(Constant[] newVars, List<Constant> allconstants, int subtaskNum, string name)
        {
            List<Tuple<int, Term>> solution = new List<Tuple<int, Term>>();
            for (int i = 0; i < newVars.Length; i++)
            {
                if (newVars[i].Name.StartsWith("!"))
                {
                    List<Constant> rightTypeConstants = allconstants.Where(x => newVars[i].Type.IsAncestorTo(x.Type)).ToList();
                    foreach (Constant c in rightTypeConstants)
                    {
                        c.Name = c.Name.Replace("!", "");
                        newVars[i] = c;
                        Term condition = new Term(name, newVars.ToArray());
                        Tuple<int, Term> tuple = new Tuple<int, Term>(subtaskNum, condition);
                        solution.Add(tuple);
                    }
                }
            }
            return solution;
        }

        //Handles logical equality conditions. 
        private bool CheckEquality(bool pos, Term condition)
        {
            int i = 0;
            foreach (Constant var in condition.Variables)
            {
                if (pos)
                {
                    foreach (Constant var2 in condition.Variables)
                    {
                        if (!var.Equals(var2)) return false;
                    }
                }
                else
                {
                    for (int j = 0; j < condition.Variables.Length; j++)
                    {
                        if (j != i)
                        {
                            if (var.Equals(condition.Variables[j])) return false;
                        }
                    }
                }
                i++;
            }
            return true;
        }
        /// <summary>
        /// Between conditions indexes must be ordered from 0!!!
        /// </summary>
        /// <param name="BetweenConditions1"></param>
        /// <param name="BetweenConditions2"></param>
        /// <param name="allVars"></param>
        /// <param name="allVarsType"></param>
        /// <returns></returns>
        private bool CreateConditions(List<Tuple<int, int, string, List<int>>> BetweenConditions1, List<Tuple<int, int, Term>> BetweenConditions2, List<String> allVars, List<ConstantType> allVarsType)
        {
            foreach (Tuple<int, int, string, List<int>> conditionTuple in BetweenConditions1)
            {
                Task task1 = Subtasks[conditionTuple.Item1];
                Task task2 = Subtasks[conditionTuple.Item2];
                Constant[] newVars = new Constant[conditionTuple.Item4.Count];
                for (int i = 0; i < conditionTuple.Item4.Count; i++)
                {
                    int num = conditionTuple.Item4[i];
                    if (num < 0 || num >= allVars.Count) return false;
                    newVars[i] = new Constant(allVars[num], allVarsType[num]);
                }
                Term condition = new Term(conditionTuple.Item3, newVars);
                Tuple<int, int, Term> tuple = new Tuple<int, int, Term>(conditionTuple.Item1, conditionTuple.Item2, condition);
                BetweenConditions2.Add(tuple);
            }
            return true;
        }

        public override string ToString()
        {
            string text = string.Join(",", Subtasks.Select(x => x.TaskInstance.Name));
            string text2 = string.Join(",", PosPreConditions.Select(x => x.Item2.Name));
            string text3 = string.Join(",", NegPreConditions.Select(x => x.Item2.Name));
            string text4 = string.Join(",", PosPostConditions.Select(x => x.Item2.Name));
            string text5 = string.Join(",", NegPostConditions.Select(x => x.Item2.Name));
            string text6 = string.Join(",", PosBetweenConditions.Select(x => x.Item3.Name));
            string text7 = string.Join(",", NegBetweenConditions.Select(x => x.Item3.Name));
            String s = "RuleInstance: " + this.MainTask.TaskInstance.Name + " subtasks " + text + " posPreCond" + text2 + "negPreCond " + text3 + "posPostCond " + text4 + " negPostCond " + text5 + "posBetweenCond " + text6 + "negBetweenCond" + text7;
            return s;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            RuleInstance r = obj as RuleInstance;
            if (!r.MainTask.Equals(MainTask))
            {
                return false;
            }
            if (!ConditionsEqual(PosPreConditions, r.PosPreConditions) || !ConditionsEqual(NegPreConditions, r.NegPreConditions) || !ConditionsEqual(PosBetweenConditions, r.PosBetweenConditions) || !ConditionsEqual(NegBetweenConditions, r.NegBetweenConditions))
            {
                return false;
            }
            if (!Subtasks.SequenceEqual(r.Subtasks))
            {
                return false;
            }
            return true;
        }

        private bool ConditionsEqual(List<Tuple<int, int, Term>> posBetweenConditions1, List<Tuple<int, int, Term>> posBetweenConditions2)
        {
            if (posBetweenConditions1 == null && posBetweenConditions2 == null) return true;
            else if (posBetweenConditions1 == null || posBetweenConditions2 == null) return false; //one of them is null but not both
            if (posBetweenConditions1.Count != posBetweenConditions2.Count) return false;
            for (int i = 0; i < posBetweenConditions1.Count; i++)
            {
                if (!posBetweenConditions1[i].Item1.Equals(posBetweenConditions2[i].Item1) || !posBetweenConditions1[i].Item2.Equals(posBetweenConditions2[i].Item2) || !posBetweenConditions1[i].Item3.Equals(posBetweenConditions2[i].Item3)) return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = MainTask.TaskInstance.GetHashCode();
            foreach (Task s in Subtasks)
            {
                hash = hash * 7 + s.GetHashCode();
            }
            hash = hash + 7 * GetHashCodeFC(PosPreConditions);
            hash = hash + 7 * GetHashCodeFC(NegPreConditions);
            hash = hash + 7 * GetHashCodeFC(PosBetweenConditions);
            hash = hash + 7 * GetHashCodeFC(NegBetweenConditions);
            return hash;
        }

        private int GetHashCodeFC(List<Tuple<int, int, Term>> posBetweenConditions)
        {
            int i = 0;
            if (posBetweenConditions == null) return 1;
            foreach (Tuple<int, int, Term> t in posBetweenConditions)
            {
                i = i + t.Item1 * t.Item2 * t.Item3.GetHashCode();
            }
            return i;
        }

        /// <summary>
        /// Returns hashcode for the conditions;
        /// </summary>
        /// <param name="posPreConditions"></param>
        /// <returns></returns>
        private int GetHashCodeFC(List<Tuple<int, Term>> posPreConditions)
        {
            int i = 0;
            if (posPreConditions == null) return 1;
            foreach (Tuple<int, Term> t in posPreConditions)
            {
                i = i + t.Item1 * t.Item2.GetHashCode();
            }
            return i;
        }

        private bool ConditionsEqual(List<Tuple<int, Term>> posPreConditions1, List<Tuple<int, Term>> posPreConditions2)
        {
            if (posPreConditions1 == null && posPreConditions2 == null) return true;
            else if (posPreConditions1 == null || posPreConditions2 == null) return false; //one of them is null but not both
            if (posPreConditions1.Count != posPreConditions2.Count) return false;
            for (int i = 0; i < posPreConditions1.Count; i++)
            {
                if (!posPreConditions1[i].Item1.Equals(posPreConditions2[i].Item1) || !posPreConditions1[i].Item2.Equals(posPreConditions2[i].Item2)) return false;
            }
            return true;

        }
    }
}
