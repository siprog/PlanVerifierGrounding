using PlanValidationExe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Program that solves the plan validation problem.
/// Author: Simona Ondrčková
/// </summary>
namespace PlanValidation1
{
    /// <summary>
    /// Describes an actual action instance. 
    /// </summary>
    class Action
    {
        /// <summary>
        /// Describes the actual action an it's variables. 
        /// </summary>
        public Term ActionInstance { get; }

        /// <summary>
        /// Describes positive precodnition with filled variables. 
        /// </summary>
        public List<Term> PosPreConditions { get; private set; }

        /// <summary>
        /// Describes negative precodnition with filled variables. 
        /// </summary>
        public List<Term> NegPreConditions { get; } //Checked in validation. 

        /// <summary>
        /// Describes positive effects with filled variables. 
        /// </summary>
        public List<Term> PosEffects { get; private set; }

        /// <summary>
        /// Describes negative effects with filled variables. 
        /// </summary>
        public List<Term> NegEffects { get; private set; }


        public ActionType ActionType;

        public Action(Term actionInstance)
        {
            this.ActionInstance = actionInstance;
            this.PosPreConditions = new List<Term>();
            this.NegPreConditions = new List<Term>();
            this.PosEffects = new List<Term>();
            this.NegEffects = new List<Term>();
        }

        public void AddConditions(List<Term> preConditions, List<Term> posEffects, List<Term> negEffects)
        {
            this.PosPreConditions = preConditions;
            this.PosEffects = posEffects;
            this.NegEffects = negEffects;
        }

        /// <summary>
        /// Removes conditions from preconditions. 
        /// bool says whether we are removing from positive or negative precondition.
        /// </summary>
        /// <param name="tobeRemoved"></param>
        /// <param name="i"></param>
        public void RemoveConditionsFromPreconditions(List<Term> tobeRemoved, bool i)
        {
            List<Term> myConditions = new List<Term>();
            if (i) myConditions = this.PosPreConditions;
            else myConditions = this.NegPreConditions;
            foreach (Term t in tobeRemoved)
            {
                myConditions.Remove(t);
            }
        }

        /// <summary>
        /// Creates a condition. 
        /// </summary>
        /// <param name="actionTypes">list of all actions types</param>
        /// <param name="allConstants">list of all constants</param>
        /// <param name="targetConditions">references to action paramateres for the condition</param>
        /// <param name="FinalConditions">Adds the created condition to this list</param>
        public void CreateCondition(List<Constant> myTypeConstants, Dictionary<String, Constant> allConstants, List<Tuple<string, List<int>>> targetConditions, List<Term> FinalConditions)
        {
            foreach (Tuple<String, List<int>> tuple in targetConditions)
            {
                Constant[] vars = new Constant[tuple.Item2.Count];
                if (tuple.Item1.Contains("!"))
                {
                    //This is a forall condition.We will handle it separately. 
                    List<Term> conditions = CreateForAllCondition(tuple, myTypeConstants, allConstants);
                    FinalConditions.AddRange(conditions);
                }
                else
                {
                    Term condition = FillRest(tuple, myTypeConstants, -1, vars, allConstants);
                    FinalConditions.Add(condition);
                }
            }
        }

        private List<Term> CreateForAllCondition(Tuple<string, List<int>> tuple, List<Constant> myTypeConstants, Dictionary<String, Constant> allConstants)
        {
            String name = tuple.Item1.Replace("!", "");
            List<Term> conditions = new List<Term>();
            for (int i = 0; i < tuple.Item2.Count; i++)
            {
                int j = tuple.Item2[i]; //Reference to either vars or constant list
                if (j <= Globals.ConstReferenceNumber)
                {
                    //First I must simply find the forallacondition. 
                    Constant cExclamation = myTypeConstants[-j + Globals.ConstReferenceNumber];
                    if (cExclamation.Name.Contains("!"))
                    {
                        //I found the forallcondition. 
                        foreach (Constant c in allConstants?.Values?.Where(x => cExclamation.Type.IsAncestorTo(x.Type)))
                        {
                            Constant[] vars = new Constant[tuple.Item2.Count];
                            vars[i] = c;
                            Term cond = FillRest(tuple, myTypeConstants, i, vars, allConstants);
                            conditions.Add(cond);
                        }
                        //If there is no constant of given type then both negative and positive forallconditions are valid and so we don't create any conditions for this action is it's essentially always true. 

                    }
                }
            }
            return conditions;
        }

        /// <summary>
        /// Fills all vars for this condition, except for the one with given index. 
        /// This method is used both for creating normal and forall conditions. With forallcondition the index is the index of the forall variable.
        /// For normal conditions just set index to -1.
        /// </summary>
        /// <param name="tuple"></param>
        /// <param name="myTypeConstants"></param>
        /// <param name="index"></param>
        /// <param name="vars"></param>
        /// <returns></returns>
        private Term FillRest(Tuple<string, List<int>> tuple, List<Constant> myTypeConstants, int index, Constant[] vars, Dictionary<String, Constant> allConstants)
        {
            String name = tuple.Item1.Replace("!", ""); //In case this was forall condition
            for (int i = 0; i < tuple.Item2.Count; i++)
            {
                if (i != index)
                {
                    int j = tuple.Item2[i]; //Reference to either vars or constant list
                    if (j == -2)
                    {
                        //This can only happen in two cases. This condition belongs to an exist condition 
                        //If the j=-2. This means its an exists condition.
                        vars[i] = null;
                    }
                    else if (j <= Globals.ConstReferenceNumber)
                    {
                        //This is a constant 
                        //It cannot be a reference to forall condition, because then i would be equal to index.
                        Constant cExclamation = myTypeConstants[-j + Globals.ConstReferenceNumber];
                        Constant c = Globals.NullLookUp(allConstants, cExclamation.Name);
                        if (vars[i] != null) Console.WriteLine("Warning: The parameters of this action's {0} condition {1} are invalid.", this.ActionInstance.Name, name);
                        vars[i] = c;
                    }
                    else
                    {
                        if (ActionInstance.Variables[j] == null)
                        {
                            string ErrorMessage = "Error: This action " + ActionInstance.Name + " contains non existent constants (constant " + j + " numbered from 0). All used constants must be described in the domain file.";
                            throw new ActionException(ErrorMessage);
                        }
                        //this is a normal reference to parameters. 
                        Constant c = Globals.NullLookUp(allConstants, ActionInstance.Variables[j].Name); //INFO we do not allow multiple constants with same name but different types.                                                                               
                        if (vars[i] != null) Console.WriteLine("Warning: The parameters of this action's {0} condition {1} are invalid.", this.ActionInstance.Name, name);
                        vars[i] = c;
                    }
                }
            }
            Term condition = new Term(name, vars);
            return condition;
        }

        public void CreateConditions(List<ActionType> actionTypes, Dictionary<String, Constant> constants)
        {
            if (ActionType != null)
            {
                CreateCondition(ActionType.Constants, constants, ActionType.posPreConditions, PosPreConditions);
                CreateCondition(ActionType.Constants, constants, ActionType.negPreConditions, NegPreConditions);
                CreateCondition(ActionType.Constants, constants, ActionType.posEffects, PosEffects);
                CreateCondition(ActionType.Constants, constants, ActionType.negEffects, NegEffects);
            }
        }

        public override string ToString()
        {
            string text = string.Join(",", PosPreConditions.Select(x => x.Name));
            string text2 = string.Join(",", PosEffects.Select(x => x.Name));
            string text3 = string.Join(",", NegEffects.Select(x => x.Name));
            string text4 = string.Join(",", NegPreConditions.Select(x => x.Name));
            string vars = string.Join(",", ActionInstance.Variables.Select(x => x.Name));
            String s = "Action: " + this.ActionInstance.Name + " variables " + vars + " preconditions: " + text + " negpreconditions: " + text4 + " posEffects: " + text2 + " negEffects " + text3;
            return s;
        }
    }
}
