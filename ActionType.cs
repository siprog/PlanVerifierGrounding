using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Describes type of an action. This is used when reading all possible actions, to create references and then to be able to create specific actions.
    /// </summary>
    class ActionType
    {
        public String Name; //Action name
        public int NumOfVariables; // Number of variables. So that load(X,Y) and load(X,Y,Z) are different actions
        public List<Constant> Vars;
        /// <summary>
        /// Some actions have conditions with constants. We can't add the constants to the list of parameters like in methods so we do it here. 
        /// </summary>
        public List<Constant> Constants;
        public List<Action> Instances;
        public List<Tuple<String, List<int>>> posPreConditions;
        public List<Tuple<String, List<int>>> negPreConditions;
        public List<Tuple<String, List<int>>> posEffects;
        public List<Tuple<String, List<int>>> negEffects;


        public ActionType()
        {
            posPreConditions = new List<Tuple<string, List<int>>>();
            negPreConditions = new List<Tuple<string, List<int>>>();
            posEffects = new List<Tuple<string, List<int>>>();
            negEffects = new List<Tuple<string, List<int>>>();
            Instances = new List<Action>();
        }

        public override string ToString()
        {
            string text2 = "";
            if (Instances != null) text2 = string.Join(",", Instances.Select(x => x.ActionInstance.Name));
            String s = "ActionType:" + this.Name + " Num of Variables " + NumOfVariables + " Instances: " + text2;
            s += " PosPrecon ";
            foreach (Tuple<String, List<int>> tuple in posPreConditions)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            s += " NegPrecon ";
            foreach (Tuple<String, List<int>> tuple in negPreConditions)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            s += " PosEffects ";
            foreach (Tuple<String, List<int>> tuple in posEffects)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            s += " NegEffects ";
            foreach (Tuple<String, List<int>> tuple in negEffects)
            {
                string m = TupleConditionToString(tuple);
                s = s + " " + m;
            }
            return s;
        }

        public String TupleConditionToString(Tuple<String, List<int>> tuple)
        {
            string s = tuple.Item1 + string.Join(",", tuple.Item2);
            return s;
        }
    }
}
