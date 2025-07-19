using PlanValidationExe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PlanValidation1
{
    /// <summary>
    /// Reads input and creates all action/task types and all rules. 
    /// </summary>
    class InputReader
    {
        enum State { inMethod, inSubtasks, nowhere, inTaskInfo, ordering, conditions, inAction, actPrecond, actEffects, inTypes, inConstants, betweenConditions };
        enum Ordering { Preset, Later, None };
        bool TOWasTrueBeforeThisTask = false; //Sometimes a user might name task for ordering later but won't give any order for ordering later. This variable is so we catch that. 
        bool firstOrderingLaterCondition = false; //This is true if this is the first condition for ordering later. 
        public List<ActionType> globalActions;
        public Dictionary<String, List<TaskType>> alltaskTypes;
        public List<Rule> allRules;
        public List<Rule> emptyRules = new List<Rule>();
        public List<Action> myActions = new List<Action>();
        public Dictionary<String, ConstantType> allConstantTypes = new Dictionary<string, ConstantType>();
        public Dictionary<String, Constant> allConstants = new Dictionary<String, Constant>();
        bool forall = false;
        Constant forallConst = null; //INFO so far we just allow one. 


        public void ReadDomain(String fileName)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            String line;
            Ordering ordering = Ordering.None;
            State state = State.inTaskInfo;
            alltaskTypes = new Dictionary<string, List<TaskType>>();
            Rule curRule = new Rule();
            allRules = new List<Rule>();
            Dictionary<String, Constant> paramTypeInfo = new Dictionary<String, Constant>();
            List<String> parameters = new List<string>();
            List<Tuple<TaskType, String, int>> namedTasks = new List<Tuple<TaskType, string, int>>();
            List<TaskType> curSubtaskList = new List<TaskType>();
            List<List<int>> referenceLists = new List<List<int>>();
            Rule lastRule = null;
            ActionType curActionType = new ActionType();
            int num = 0;
            List<Tuple<Term, bool>> preconditions = new List<Tuple<Term, bool>>();
            List<Tuple<List<int>, Term, bool>> betweenConditions = new List<Tuple<List<int>, Term, bool>>();
            globalActions = new List<ActionType>();
            int subtaskCount = 0;
            bool doneSubtask = false;
            bool doneConditions = false;
            bool doneConstants = false;
            bool doneActEff = false;
            bool doneOrder = false;

            String actName = "";
            bool lastInConditions = false;
            while ((line = file.ReadLine()) != null)
            {
                if (Globals.IgnoreCase) line = line.ToLower(new CultureInfo("en-US", false));
                line = line.Trim();
                if (line.Contains(":types"))
                {
                    state = State.inTypes;
                }
                if (line.Contains(":constants"))
                {
                    state = State.inConstants;
                }
                if (state == State.inTypes)
                {
                    if (line.Trim().Equals(")"))
                    {
                        FinishTypeHierarchy(ref allConstantTypes);
                        state = State.inTaskInfo;
                    }
                    else
                    {
                        CreateTypeHieararchy(line, allConstantTypes);
                    }
                }
                if (state == State.inConstants)
                {
                    if (line.Trim().Equals(")"))
                    {
                        state = State.inTaskInfo;
                    }
                    else
                    {
                        GetConstants(line, ref allConstants, allConstantTypes);
                        doneConstants = CheckParenthesis(line) > 0;
                        if (doneConstants)
                        {
                            state = State.inTaskInfo;
                            doneConstants = false;
                        }
                    }
                }
                if (state == State.inTaskInfo && line.Contains(":task"))
                {
                    //Getting list of all tasks
                    TaskType tT = CreateTaskType(line);
                    if (alltaskTypes.ContainsKey(tT.Name)) alltaskTypes[tT.Name].Add(tT);
                    else
                    {
                        List<TaskType> listofTaskypesWithSameName = new List<TaskType>();
                        listofTaskypesWithSameName.Add(tT);
                        alltaskTypes.Add(tT.Name, listofTaskypesWithSameName);
                    }
                }
                if (line.Contains("(:method"))
                {
                    //This means we are starting a new rule. So if the previous rule had named tasks but didn't use them, now we need to reset the names
                    namedTasks = new List<Tuple<TaskType, string, int>>();
                    num = 0;
                    state = State.inMethod;
                }
                if (line.Contains("(:action"))
                {
                    state = State.inAction;
                }
                if (state == State.inMethod)
                {
                    if (line.Trim().Equals(")") && lastInConditions)
                    {
                        //This is an empty rule.   
                        if (paramTypeInfo != null)
                        {
                            curRule.AllVars = paramTypeInfo.Keys.ToList();
                            curRule.AllVarsTypes = paramTypeInfo.Values.Select(x => x.Type).ToList();
                        }
                        CreateConditions(curRule, preconditions);
                        preconditions = new List<Tuple<Term, bool>>();
                        emptyRules.Add(curRule);
                        allRules.Add(curRule);
                        lastInConditions = false;
                        curRule = new Rule();
                        paramTypeInfo = null;
                    }
                    if (line.Contains(":parameters"))
                    {
                        HandleParameters(line, ref curRule, ref paramTypeInfo);
                    }
                    else if (line.Contains(":task"))
                    {
                        //Getting  main task
                        TaskType tT = CreateTaskType(line, ref paramTypeInfo, out List<int> refList, allConstants);
                        if (tT != null)
                        {
                            TaskType t = FindTask(tT, alltaskTypes);
                            curRule.MainTaskType = t;
                            curRule.MainTaskReferences = refList;
                        }
                    }
                    else if (line.Contains(":subtasks") || line.Contains(":ordered-subtasks"))
                    {
                        lastInConditions = false;
                        state = State.inSubtasks;
                        subtaskCount = 0;
                        if (line.Contains("ordered")) ordering = Ordering.Preset;
                        doneSubtask = false;
                    }
                    else if (line.Contains(":ordering"))
                    {
                        state = State.ordering;
                        num = 0;
                    }
                    else if (line.Contains(":precondition"))
                    {
                        state = State.conditions;
                        doneConditions = false;
                    }
                    else if (line.Contains(":between-condition"))
                    {
                        state = State.betweenConditions;
                    }
                }
                else if (state == State.conditions)
                {
                    //Checks if there are more closed parenthesis and this section is over. 
                    if (forall) doneConditions = CheckParenthesis(line) > 1; // one closed parentehis is from forall. 
                    else doneConditions = CheckParenthesis(line) > 0;
                    if (line.Trim().Equals(")"))
                    {
                        state = State.inMethod;
                    }
                    else
                    {
                        Tuple<Term, bool> condition = CreateCondition(line, ref paramTypeInfo, allConstants);
                        if (condition != null) preconditions.Add(condition);
                        if (doneConditions)
                        {
                            //If the rule is empty as in has no substasks than this is the last thing it will go through.
                            state = State.inMethod;
                            lastInConditions = true;
                        }
                    }
                }
                else if (state == State.betweenConditions)
                {
                    doneConditions = CheckParenthesis(line) > 0;
                    if (line.Trim().Equals(")"))
                    {
                        state = State.inMethod;
                    }
                    else
                    {
                        line = line.Replace("(", "");
                        string[] parts = line.Split(' '); //line loks like this>     1 2 powerco-of ?town ?powerco))
                        while (parts.Length >= 1 && parts[0] == "")
                        {
                            parts = (string[])parts.Skip(1).ToArray();
                        }
                        try
                        {
                            List<int> betweenTasks = new List<int>
                            {
                                Int32.Parse(parts[0]),
                                Int32.Parse(parts[1])
                            };
                            line = line.Replace(Int32.Parse(parts[0]) + " " + Int32.Parse(parts[1]) + " ", ""); //Now this looks like a normal condition.     powerco-of ?town ?powerco))
                            line = line.Replace("not ", "(not ");
                            Tuple<Term, bool> condition = CreateCondition(line, ref paramTypeInfo, allConstants);
                            betweenConditions.Add(new Tuple<List<int>, Term, bool>(betweenTasks, condition.Item1, condition.Item2));
                            if (doneConditions)
                            {
                                //If the rule is empty as in has no substasks than this is the last thing it will go through.
                                state = State.inMethod;
                                lastInConditions = true; //We cant have empty subtask with between conditions. 
                            }
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Error: Invalid description of a between condition: " + line);
                        }
                    }
                }
                else if (state == State.inSubtasks)
                {
                    if (!line.Trim().Equals(")"))
                    {
                        //Checks if there are more closed parenthesis and this section is over. 
                        doneSubtask = CheckParenthesis(line) > 0;
                        if (subtaskCount == 0 && ordering != Ordering.Preset)
                        {
                            //Only check this if ordering is not preset. Cause then I know the ordering. 
                            int parenthesisCount = line.Count(x => x == '(');
                            if (parenthesisCount > 1) ordering = Ordering.Later;
                            else ordering = Ordering.None;
                        }
                        if (ordering == Ordering.Preset || ordering == Ordering.None)
                        {
                            //Ordered subtasks look almost the same as regular tasks. 
                            List<int> refList = new List<int>();
                            TaskType tT = CreateTaskType(line, ref paramTypeInfo, out refList, allConstants);
                            if (tT != null)
                            {
                                TaskType t = FindTask(tT, alltaskTypes);
                                if (t == tT)
                                {
                                    if (alltaskTypes.ContainsKey(t.Name)) alltaskTypes[t.Name].Add(t);
                                    else
                                    {
                                        List<TaskType> listofTaskypesWithSameName = new List<TaskType>();
                                        listofTaskypesWithSameName.Add(t);
                                        alltaskTypes.Add(t.Name, listofTaskypesWithSameName);
                                    }
                                }
                                if (!t.Rules.Contains(curRule)) t.AddRule(curRule);
                                referenceLists.Add(refList);
                                curSubtaskList.Add(t);
                                subtaskCount++;
                            }
                        }
                        else
                        {
                            //Unordered subtask. After subtasks there will be ordering.
                            List<int> refList = new List<int>();
                            Tuple<TaskType, string> tupleTaskName = CreateNamedTaskType(line, ref paramTypeInfo, out refList, allConstants);
                            Tuple<TaskType, string, int> tupleFull = new Tuple<TaskType, string, int>(tupleTaskName.Item1, tupleTaskName.Item2, num);
                            namedTasks.Add(tupleFull);
                            TaskType t = FindTask(tupleTaskName.Item1, alltaskTypes);  //Finds the task in lists of all tasks. 
                            if (t == tupleTaskName.Item1)
                            {
                                if (alltaskTypes.ContainsKey(t.Name)) alltaskTypes[t.Name].Add(t);
                                else
                                {
                                    List<TaskType> listofTaskypesWithSameName = new List<TaskType>();
                                    listofTaskypesWithSameName.Add(t);
                                    alltaskTypes.Add(t.Name, listofTaskypesWithSameName);
                                }
                            }
                            if (!t.Rules.Contains(curRule)) t.AddRule(curRule); //Adds a link from this tasktype to the rule. 
                            curSubtaskList.Add(t);
                            referenceLists.Add(refList);
                            num++;
                            subtaskCount++;
                        }
                    }
                    if (line.Trim().Equals(")") || doneSubtask)
                    {
                        if (paramTypeInfo != null)
                        {
                            curRule.AllVars = paramTypeInfo.Keys.ToList();
                            curRule.AllVarsTypes = paramTypeInfo.Values.Select(x => x.Type).ToList();
                        }
                        //At least one subtask is not fully ordered. 
                        if (Globals.TOIndicator && ordering == Ordering.None && curSubtaskList?.Count > 1) Globals.TOIndicator = false;
                        CreateConditions(curRule, preconditions);
                        CreateBetweenConditions(curRule, betweenConditions);
                        curRule.TaskTypeArray = curSubtaskList.ToArray();
                        curRule.Finish(referenceLists);
                        if (curRule.TaskTypeArray.Length == 0)
                        {
                            emptyRules.Add(curRule);
                        }
                        if (ordering == Ordering.Preset) curRule.CreateFullOrder();
                        curSubtaskList = new List<TaskType>();
                        referenceLists = new List<List<int>>();
                        paramTypeInfo = null;
                        allRules.Add(curRule);
                        lastRule = curRule; //for ordering
                        curRule = new Rule();
                        if (ordering != Ordering.Later) state = State.nowhere;
                        else
                        {
                            state = State.inMethod;
                            if (Globals.TOIndicator == true) TOWasTrueBeforeThisTask = true;
                            firstOrderingLaterCondition = false;
                            Globals.TOIndicator = false;
                        }
                        preconditions = new List<Tuple<Term, bool>>();
                        betweenConditions = new List<Tuple<List<int>, Term, bool>>();
                        ordering = Ordering.None;
                        subtaskCount = 0;
                    }
                }
                else if (state == State.ordering)
                {
                    doneOrder = CheckParenthesis(line) > 0;
                    if (!line.Trim().Equals(")"))
                    {
                        if (TOWasTrueBeforeThisTask && !firstOrderingLaterCondition)
                        {
                            //Before this task all the ordering was TO. The user has named tasks so we assume there will be ordering later. 
                            //Now we know that they put in at least one ordering later condition. 
                            //So we can now turn TO back to true and these ordering condition will determine whether its TO or not. 
                            //This is done in such a complicated way because its possisble that someone will name tasks but not put any ordering later. 
                            //Hence they would simply continue with the next task/action. Since we dont know with  what we must preemptively turn TO off and then back on if they actually give any ordering. 
                            firstOrderingLaterCondition = true;
                            Globals.TOIndicator = true;

                        }
                        CreatePartialOrder(line, namedTasks, ref lastRule);

                    }
                    if (line.Trim().Equals(")") || doneOrder)
                    {
                        num = 0;
                        namedTasks = new List<Tuple<TaskType, string, int>>();
                        state = State.nowhere;
                        ordering = Ordering.None;
                        if (Globals.TOIndicator && !IsFullyOrdered(lastRule)) Globals.TOIndicator = false;
                        lastRule.FinishPartialOrder();
                    }
                }
                else if (state == State.inAction)
                {
                    Dictionary<String, Constant> actVars = new Dictionary<String, Constant>();
                    if (line.Contains(":action"))
                    {
                        //This is here because some actions might not have preconditions or effects so the normal finish for actions after effects does not apply. But if it is followed by another action we can use this one.
                        if (curActionType.Name != null) globalActions.Add(curActionType);
                        curActionType = new ActionType();

                        actName = GetActionName(line);
                        curActionType.Name = actName;
                    }
                    else if (line.Contains("parameters"))
                    {
                        actVars = GetParameters(line, allConstantTypes);
                        if (actVars != null)
                        {
                            curActionType.NumOfVariables = actVars.Count;
                            curActionType.Vars = actVars.Values.ToList();
                        }
                        else
                        {
                            curActionType.NumOfVariables = 0;
                        }
                    }
                    else if (line.Contains(":precondition"))
                    {
                        state = State.actPrecond;
                    }
                    else if (line.Contains(":effect"))
                    {
                        state = State.actEffects; // in case the action has no preconditions but only effects. 
                    }
                }
                if (state == State.actPrecond) //since preconditions have the first condition on the same line as the declaration this must be if and not else if. 
                {
                    bool isPos;
                    if (line.Contains(":effect"))
                    {
                        state = State.actEffects;
                    }
                    else
                    {
                        Tuple<String, List<int>> condition = null;
                        isPos = true;
                        //What if I have action without parameters and with conditions?
                        //The question mark does that if vars is null it will just pass null inside the method. 
                        condition = GetActionCondition(line, curActionType.Vars?.Select(x => x.Name).ToList(), ref curActionType.Constants, out isPos);

                        if (condition != null)
                        {
                            if (isPos) curActionType.posPreConditions.Add(condition);
                            else curActionType.negPreConditions.Add(condition);
                        }
                    }
                }
                if (state == State.actEffects)
                {
                    if (forall) doneActEff = CheckParenthesis(line) > 1;
                    else doneActEff = CheckParenthesis(line) > 0;
                    if (!line.Trim().Equals(")"))
                    {
                        bool isPos;
                        if (!line.Trim().Equals(""))
                        {
                            Tuple<String, List<int>> condition = null;
                            isPos = true;
                            condition = GetActionCondition(line, curActionType.Vars?.Select(x => x.Name).ToList(), ref curActionType.Constants, out isPos);

                            if (condition != null)
                            {
                                if (isPos) curActionType.posEffects.Add(condition);
                                else curActionType.negEffects.Add(condition);
                            }
                        }
                    }
                    if (doneActEff)
                    {
                        if (curActionType.Name != null) globalActions.Add(curActionType);
                        curActionType = new ActionType();
                    }
                }
            }
            //If the last action did not have effects we still want to add it to the list of actions.
            if (curActionType.Name != null) globalActions.Add(curActionType);
            curActionType = new ActionType();
        }

        /// <summary>
        /// Returns true if this ordering is full.
        /// this is right because we only have non transitive order conditions
        /// </summary>
        /// <param name="lastRule"></param>
        /// <returns></returns>
        private bool IsFullyOrdered(Rule lastRule)
        {
            //This is true if a subtask at given posisiton is immediately before some other task
            bool[] isImmediatelyBefore = new bool[lastRule.TaskTypeArray.Length];
            //This is true if a subtask at given posisiton is immediately after some other task
            bool[] isImmediatelyAfter = new bool[lastRule.TaskTypeArray.Length];
            foreach (var order in lastRule.orderConditions)
            {
                isImmediatelyBefore[order.Item1] = true;
                isImmediatelyAfter[order.Item2] = true;
            }
            if (isImmediatelyBefore.Count(x => !x) == 1 && isImmediatelyAfter.Count(x => !x) == 1) return true;
            return false;
        }

        /// <summary>
        /// Reads constants from list of constants.Line looks like this:fema ebs police-chief - callable
        /// </summary>
        /// <param name="line"></param>
        /// <param name="allConstants"></param>
        /// <param name="allConstantTypes"></param>
        /// 
        private void GetConstants(string line, ref Dictionary<String, Constant> allConstants, Dictionary<String, ConstantType> allConstantTypes)
        {
            line = CleanUpInput(line, new List<string>() { "(:constants", "(and", "(", ")" }, ";;");
            string[] parts = line.Trim().Split(' ');
            if (parts.Length < 1) return;
            while (parts.Length >= 1 && parts[0] == "")
            {
                parts = (string[])parts.Skip(1).ToArray();
            }
            if (parts.Length < 1) return;
            String s = parts[parts.Length - 1]; //The final type. In example it's callable.
            ConstantType t = ContainsType(allConstantTypes, s);
            if (t == null)
            {
                //This type does not exist. 
                if (s != ":constants") Console.WriteLine("Warning:Constants have non existent Type {0}", s);
                return;
            }
            parts[parts.Length - 1] = null;
            foreach (String m in parts)
            {
                if (m != null && m != "-")
                {
                    Constant c1 = new Constant(m, t);
                    allConstants.Add(c1.Name, c1);
                }
            }
        }

        /// <summary>
        /// I have created the entire type hierarchy. Now I must add type any which is a child to everything.  
        /// This is used in rules or actiontypes, when we have a constant without a type. 
        /// Can also be used to ignore all types.
        /// </summary>
        /// <param name="types"></param>
        private void FinishTypeHierarchy(ref Dictionary<String, ConstantType> types)
        {
            ConstantType any = new ConstantType("any");
            foreach (ConstantType c in types.Values)
            {
                any.AddAncestor(c);
                c.AddChild(any);
                c.CreateDescendantLine();
            }
            any.CreateDescendantLine();
            types.Add("any", any);
        }

        //line looks like this:
        //waterco powerco - callable
        private void CreateTypeHieararchy(string line, Dictionary<String, ConstantType> types)
        {
            string[] parts = line.Trim().Split(' ');
            if (line.Trim().Equals("(:types")) return;
            String s = parts[parts.Length - 1]; //The final type.
            ConstantType t = ContainsType(types, s);
            if (t == null)
            {
                //This type does not exist create it. 
                t = new ConstantType(s);
                types.Add(t.Name, t);
            }
            parts[parts.Length - 1] = null;
            foreach (String m in parts)
            {
                if (m != null && m != "-")
                {
                    ConstantType t1 = ContainsType(types, m);
                    if (t1 == null)
                    {
                        t1 = new ConstantType(m);
                        types.Add(t1.Name, t1);
                    }
                    t1.AddAncestor(t);
                    t.AddChild(t1);
                }
            }
        }

        private ConstantType ContainsType(Dictionary<String, ConstantType> types, String name)
        {
            if (types.ContainsKey(name)) return types[name];
            return null;
        }

        private string GetActionName(string line)
        {
            line = line.Replace("(:action ", "");
            line = line.Trim();
            return line;
        }

        private int CheckParenthesis(string line)
        {
            int openParenthesisCount = line.Count(x => x == '(');
            int closedParenthesisCount = line.Count(x => x == ')');
            return closedParenthesisCount - openParenthesisCount;
        }

        /// <summary>
        /// Cleans up input. Removes all required words. If you wanna check for space after the words you must add it in the string.   
        /// Also removes all comments after commentMark
        /// </summary>
        /// <param name="line"></param>
        /// <param name="wordsToRemove"></param>
        /// <returns></returns>
        private String CleanUpInput(String line, List<String> wordsToRemove, String commentMark)
        {
            foreach (String s in wordsToRemove)
            {
                line = line.Replace(s, "");
            }
            line = line.Trim();
            int index = line.IndexOf(commentMark); //Removes everything after commentMark which symbolizes comment
            if (index > 0)
            {
                line = line.Substring(0, index);
            }
            return line;
        }
        [Obsolete("GetActionCondition is deprecated, please use GetActionCondition with 4 parameters instead.")]
        private Tuple<string, List<int>> GetActionCondition(string line, List<string> vars, out bool isPos)
        {
            if (forall) forall = false;
            isPos = true;
            if (line.Contains("(not"))
            {
                line = line.Replace("(not", "");
                isPos = false;
            }
            line = CleanUpInput(line, new List<string>() { ":precondition ", ":effect", ")", "(", }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length < 1) return null;
            if (parts.Length < 1 || line.Trim().Equals("and")) return null;
            string name = parts[0];
            if (name.Trim().Equals("exists")) return null;//Currently ignores both exist conditions, which is proper behaviour. 
            if (name.Trim().Equals("forall"))
            {
                forall = true;
                for (int i = 1; i + 2 < parts.Length; i += 3)
                {
                    name = parts[i];
                    ConstantType t = ContainsType(allConstantTypes, parts[i + 2]);
                    if (t == null) t = ContainsType(allConstantTypes, "any");
                    forallConst = new Constant(name, t);
                    vars.Add("!" + name);
                }
                return null;
            }
            string[] myVars = (string[])parts.Skip(1).ToArray();
            List<int> references = new List<int>();
            foreach (string var in myVars)
            {
                if (!var.Equals("-"))
                {
                    int i = vars.IndexOf(var);
                    if (i == -1) i = vars.IndexOf("!" + var); //In case this links to forall constant. 
                    if (i == -1)
                    {
                        if (!var.StartsWith("?"))
                        {
                            //this is a constant
                            //vars.Add(var); 
                            //To remember what constant we shall add it to name of this condition
                            name = name + "!" + var;
                            i = -3;
                        }
                        else
                        {
                            vars.Add(var);
                            i = -2; //this is either exists or forall condition.
                        }
                    }
                    references.Add(i);
                }
            }
            forallConst = null;
            return new Tuple<string, List<int>>(name, references);
        }


        private Tuple<string, List<int>> GetActionCondition(string line, List<string> vars, ref List<Constant> constants, out bool isPos)
        {
            if (constants == null) constants = new List<Constant>();
            //if (forall) forall = false;
            isPos = true;
            if (line.Contains("(not"))
            {
                line = line.Replace("(not", "");
                isPos = false;
            }
            line = CleanUpInput(line, new List<string>() { "(and ", ":precondition ", ":effect", ")", "(", }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length < 1 || line.Trim().Equals("and")) return null;
            string name = parts[0];
            if (forall)
            {
                forall = false;
                name = "!" + name;
            }
            if (name.Trim().Equals("exists")) return null;//Currently ignores both exist conditions, which is proper behaviour. 
            if (name.Trim().Equals("forall"))
            {
                forall = true;
                for (int i = 1; i + 2 < parts.Length; i += 3)
                {
                    name = parts[i];
                    ConstantType t = ContainsType(allConstantTypes, parts[i + 2]);
                    if (t == null) t = ContainsType(allConstantTypes, "any");
                    forallConst = new Constant("!" + name, t);
                    //We will add the forallconstant to list of constants
                    constants.Add(forallConst);
                }
                return null;
            }
            string[] myVars = (string[])parts.Skip(1).ToArray();
            List<int> references = new List<int>();
            foreach (string var in myVars)
            {
                if (!var.Equals("-"))
                {
                    int i;
                    if (vars == null) i = -1;
                    else i = vars.IndexOf(var);
                    if (i == -1)
                    {
                        //this is a constant or a forall condition
                        //First we check if it is already in our list of constants.
                        ConstantType t = ContainsType(allConstantTypes, var);
                        if (t == null) t = ContainsType(allConstantTypes, "any");
                        Constant c = new Constant(var, t);
                        int index = constants.FindIndex(x => x.Name == var);
                        if (index == -1)
                        {
                            if (!var.StartsWith("?"))
                            {
                                //This constant is not in the list of constants for this actionType we must add it.                                 
                                constants.Add(c);
                                index = constants.Count - 1;
                            }
                            else
                            {
                                //This is a forall condition
                                index = constants.FindIndex(x => x.Name == "!" + var);
                            }
                        }
                        i = Globals.ConstReferenceNumber - index; //This ensures that this number remains negative and won't trickle over to normal references
                    }
                    references.Add(i);
                }
            }
            forallConst = null;
            return new Tuple<string, List<int>>(name, references);
        }




        /// <summary>
        /// Finds task in alltasktypes that has the same name and variable. If that one does not exist it returns the original task. 
        /// </summary>
        /// <param name="tT"></param>
        /// <param name="alltaskTypes"></param>
        /// <returns></returns>
        private TaskType FindTask(TaskType tT, Dictionary<String, List<TaskType>> alltaskTypes)
        {
            if (alltaskTypes.ContainsKey(tT.Name))
            {
                foreach (TaskType t in alltaskTypes[tT.Name])
                {
                    if (t.NumOfVariables == tT.NumOfVariables) return t;
                }
            }
            return tT;

        }

        //Creates proper rule conditions.
        private void CreateConditions(Rule curRule, List<Tuple<Term, bool>> preconditions)
        {
            List<string> methodParams = curRule.AllVars;
            List<int> varReferences = new List<int>();
            Tuple<int, String, List<int>> condition;
            if (curRule.posPreConditions == null) curRule.posPreConditions = new List<Tuple<int, string, List<int>>>();
            if (curRule.negPreConditions == null) curRule.negPreConditions = new List<Tuple<int, string, List<int>>>();
            foreach (Tuple<Term, bool> cond in preconditions)
            {

                for (int i = 0; i < cond.Item1.Variables.Length; i++)
                {
                    int j = 0;
                    foreach (String s in methodParams)
                    {
                        if (s.Equals(cond.Item1.Variables[i].Name) && curRule.AllVarsTypes[j] == cond.Item1.Variables[i].Type) break;
                        j++;
                    }
                    varReferences.Add(j);
                    if (j == -1 || j > methodParams.Count - 1)
                    {
                        if (cond.Item1.Variables[i] != forallConst && cond.Item1.Variables[i].Name.StartsWith("?")) Console.WriteLine("Warning: Coudnt find condition {0} in allvars {1} in rule {2}", cond.Item1.Variables[i], string.Join(",", methodParams.ToArray()), curRule.MainTaskType.Name);
                        //If it doesnt start with a ? then it is a constant so of course its not in the main rule. It will be added later on. Non need to call warning
                    }
                }
                condition = new Tuple<int, string, List<int>>(-1, cond.Item1.Name, varReferences);
                varReferences = new List<int>();
                if (cond.Item2) curRule.posPreConditions.Add(condition);
                else curRule.negPreConditions.Add(condition);
            }
        }

        private void CreateBetweenConditions(Rule curRule, List<Tuple<List<int>, Term, bool>> betweenconditions)
        {
            List<string> methodParams = curRule.AllVars;
            List<int> varReferences = new List<int>();
            Tuple<int, int, String, List<int>> condition;
            if (curRule.posBetweenConditions == null) curRule.posBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            if (curRule.negBetweenConditions == null) curRule.negBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            foreach (Tuple<List<int>, Term, bool> cond in betweenconditions)
            {
                for (int i = 0; i < cond.Item2.Variables.Length; i++)
                {
                    int j = 0;
                    foreach (String s in methodParams)
                    {
                        if (s.Equals(cond.Item2.Variables[i].Name) && curRule.AllVarsTypes[j] == cond.Item2.Variables[i].Type) break;
                        j++;
                    }
                    varReferences.Add(j);
                    if (j == -1 || j > methodParams.Count - 1)
                    {
                        if (cond.Item2.Variables[i] != forallConst && cond.Item2.Variables[i].Name.StartsWith("?")) Console.WriteLine("Warning: Coudnt find condition {0} in allvars {1} in rule {2}", cond.Item2.Variables[i], string.Join(",", methodParams.ToArray()), curRule.MainTaskType.Name);
                        //If it doesnt start with a ? then it is a constant so of course its not in the main rule. It will be added later on. Non need to call warning
                    }
                }
                condition = new Tuple<int, int, string, List<int>>(cond.Item1[0], cond.Item1[1], cond.Item2.Name, varReferences);
                varReferences = new List<int>();
                if (cond.Item3) curRule.posBetweenConditions.Add(condition);
                else curRule.negBetweenConditions.Add(condition);
            }
        }

        // line looks like this: (contentOf ?b ?c)
        // or like this: (not(= ?b ?b2))        
        //returns condition and bool is true if condition is positive false, if negative. 
        private Tuple<Term, bool> CreateCondition(string line, ref Dictionary<String, Constant> methodInfo, Dictionary<String, Constant> allConstants)
        {
            line = line.Trim();
            if (forall) forall = false; //Last condition was for all now is the one it applies to.
            bool isPositive = true;
            if (line.Contains("(not(") || line.Contains("(not "))
            {
                line = line.Replace("(not", "");
                isPositive = false;
            }
            //now line loks like this> (contentOf ?b ?c) or this: (= ?b ?b2))
            line = CleanUpInput(line, new List<string>() { "(and ", ")", "(", ":precondition", ":effect" }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) return null;
            parts[0] = parts[0].Trim();
            string name = parts[0];
            if (name.Trim().Equals("exists")) return null;
            if (name.Trim().Equals("forall"))
            {
                for (int i = 1; i + 2 < parts.Length; i += 3)
                {
                    name = parts[i];
                    ConstantType t = ContainsType(allConstantTypes, parts[i + 2]);
                    if (t == null) t = ContainsType(allConstantTypes, "any");
                    forallConst = new Constant("!" + name, t);
                    if (methodInfo == null) { methodInfo = new Dictionary<String, Constant>(); } //this was added because we had a forall condition on method with no parameters.
                    methodInfo.Add(forallConst.Name, forallConst);
                }
                return null;
            }
            string[] vars = (string[])parts.Skip(1).ToArray();
            List<Constant> conVars = new List<Constant>();
            foreach (String s in vars)
            {
                Constant c = FindConstant(s, methodInfo);
                ConstantType any = ContainsType(allConstantTypes, "any");
                if (c == null)
                {
                    c = FindConstant("!" + s, methodInfo);
                    if (c == null)
                    {
                        //This constant is not in the rules paramaters. We should add it there. 
                        c = FindConstant(s, allConstants);
                        if (c == null) c = new Constant(s, any);
                        methodInfo.Add(c.Name, c);
                    }
                }
                conVars.Add(c);
            }
            Term term = new Term(name, conVars.ToArray());
            Tuple<Term, bool> tuple = new Tuple<Term, bool>(term, isPositive);
            return tuple;
        }

        /// <summary>
        /// Creates method parameters (including the ?)
        /// The line loks like this: :parameters (?b1 ?b2 - bowl ?c1 ?c2 - content)
        /// We ignore types for now. 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private Dictionary<String, Constant> GetParameters(string line, Dictionary<String, ConstantType> types)
        {
            Dictionary<String, Constant> parameters = new Dictionary<String, Constant>();
            line = CleanUpInput(line, new List<string>() { "(and ", ":parameters ", "(", ")" }, ";;");
            List<String> curNames = new List<string>();
            ConstantType type;
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) return null;
            parts[0] = parts[0].Trim();
            foreach (string par in parts)
            {
                if (par.Contains("?"))
                {
                    curNames.Add(par);
                }
                else
                {
                    if (par != "-")
                    {
                        type = types[par];
                        if (type == null) Console.WriteLine("This has not type {0}", par);
                        if (curNames?.Any() == true)
                        {
                            foreach (String name in curNames)
                            {
                                String name2 = name;
                                if (Globals.IgnoreCase) name2 = name2.ToLower(new CultureInfo("en-US", false));
                                parameters.Add(name2, new Constant(name2, type));
                            }
                        }
                        curNames = new List<string>();
                    }
                }
            }
            return parameters;
        }

        //The line loks like this: (st1 <st2) or like this (< st1 st2)
        //The tuple is ordered the same way the tasks are in rule. So based on which tuple it is in list it is the num. 
        private void CreatePartialOrder(string line, List<Tuple<TaskType, string, int>> namedTasks, ref Rule curRule)
        {
            if (line.Equals(")")) return;
            line = CleanUpInput(line, new List<string>() { "(and ", "(", ")", "<" }, ";;");
            string[] parts = line.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0) return;
            parts[0] = parts[0].Trim();
            Tuple<TaskType, string, int> tuple1 = namedTasks.First(c => c.Item2.Equals(parts[0]));
            Tuple<TaskType, string, int> tuple2 = namedTasks.First(c => c.Item2.Equals(parts[1]));
            curRule.AddPartialCondition(tuple1.Item3, tuple2.Item3, true);
        }

        //The line loks like this: st1 (add cream ?b1))
        private Tuple<TaskType, String> CreateNamedTaskType(string line, ref Dictionary<String, Constant> methodParam, out List<int> refList, Dictionary<String, Constant> fixedConstants)
        {
            line = line.Replace("(and ", "("); // if the line starts with (and we should ignore it. 
            int index = line.IndexOf(";;"); //Removes everythign after ;; which symbolizes comment
            if (index > 0)
            {
                line = line.Substring(0, index);
            }
            string[] parts = line.Trim().Split('(').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            string name = parts[0].Trim();
            if (parts.Length > 1) line = line.Replace(name, ""); //This means that there is ordering. Sometimes there is no ordering so if there is none then. the task is just normal.              
            TaskType t = CreateTaskType(line, ref methodParam, out refList, fixedConstants);
            return new Tuple<TaskType, string>(t, name);
        }

        ///The line loks like this: :task (makeNoodles ?n ?p)
        ///or like this:  (add water ?p)
        ///Depends on whether this is the main task of rule or subtask. 
        private TaskType CreateTaskType(string line, ref Dictionary<String, Constant> methodParam, out List<int> refList, Dictionary<String, Constant> fixedConstants)
        {
            refList = new List<int>();
            line = CleanUpInput(line, new List<string>() { ":subtasks", "(and", ":task ", "(", ")" }, ";;");
            string[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0)
            {
                return null;
            }
            string name = parts[0];
            string[] parameters = (string[])parts.Skip(1).ToArray();
            foreach (string param in parameters)
            {
                if (param != "")
                {
                    Constant c1 = null;
                    //If this parameter references some parameter in the rule we just add that to refList. 
                    if (methodParam != null)
                    {
                        string param2 = param;
                        if (Globals.IgnoreCase) param2 = param2.ToLower(new CultureInfo("en-US", false));
                        c1 = Globals.NullLookUp(methodParam, param2);
                        if (c1 != null)
                        {
                            refList.Add(methodParam.Values.ToList().IndexOf(c1));
                        }
                    }
                    if (methodParam == null || c1 == null) //Either there are no parameters for the rule, or the rule does not have this param as parameter. 
                    {
                        if (methodParam == null) methodParam = new Dictionary<string, Constant>();
                        Constant c = FindConstant(param, fixedConstants);
                        if (c == null)
                        {
                            Console.WriteLine("Warning: We were given constant that does not exist {0}. Please describe this constant in the domain under the :constants tag. ", param);
                            ConstantType a = ContainsType(allConstantTypes, "any");
                            c = new Constant(param, a);
                            fixedConstants.Add(c.Name, c);
                        }
                        methodParam.Add(c.Name, c);
                        refList.Add(methodParam.Count - 1);
                    }
                }
            }
            TaskType tT = new TaskType(name, parameters.Length);
            return tT;
        }

        /// <summary>
        /// Gets a list of constants and a name and returns the constant associated to it. 
        /// </summary>
        /// <param name="param"></param>
        /// <param name="fixedConstants"></param>
        /// <returns></returns>
        private Constant FindConstant(string param, Dictionary<String, Constant> fixedConstants)
        {
            if (fixedConstants == null) return null;
            if (Globals.IgnoreCase) param = param.ToLower(new CultureInfo("en-US", false));
            Constant c = Globals.NullLookUp(fixedConstants, param);
            return c;
        }

        //The line loks like this: (:task makeTomatoSoup :parameters (?p - cookingPot))
        private TaskType CreateTaskType(string line) //From list of main tasks
        {
            line = CleanUpInput(line, new List<string>() { "(:task " }, ";;");
            String[] parts = line.Trim().Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();  //parts: makeTomatoSoup :parameters (?p - cookingPot))
            String name = parts[0]; //makeTomatoSoup
            String[] parameters = (string[])parts.Skip(2).ToArray();// (? p - cookingPot))
            List<string> myParams = new List<string>();
            foreach (String possibleParam in parameters)
            {
                //Currently we ignore types so just find the one that contains ?
                if (possibleParam.Contains("?")) myParams.Add(possibleParam);
            }
            TaskType tT = new TaskType(name, myParams.Count);
            return tT;
        }

        public List<Action> ReadPlan(String fileName, List<ActionType> allActionTypes, Dictionary<String, Constant> allConstants)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            myActions = new List<Action>();
            String line;
            while ((line = file.ReadLine()) != null)
            {
                if (Globals.IgnoreCase) line = line.ToLower(new CultureInfo("en-US", false));
                string[] actions = line.Split('(').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                foreach (String a in actions)
                {
                    Term actionInstance = CreateActionInstance(a, allActionTypes, allConstants);
                    if (actionInstance != null)
                    {
                        Action action = new Action(actionInstance);
                        ActionType aT = FindActionType(allActionTypes, action);
                        aT.Instances.Add(action);
                        action.ActionType = aT;
                        action.CreateConditions(allActionTypes, allConstants);
                        myActions.Add(action);
                    }
                }
            }
            return myActions;
        }

        private ActionType FindActionType(List<ActionType> allActionTypes, Action a)
        {
            if (allActionTypes.FirstOrDefault(x => x.Name.ToLower(new CultureInfo("en-US", false)).Equals(a.ActionInstance.Name.ToLower(new CultureInfo("en-US", false))) && x.NumOfVariables == a.ActionInstance.Variables.Length) == null)
            {
                string ErrorMessage = "This action " + a + " is not described anywhere in the domain file.";
                throw new ActionException(ErrorMessage);
            }
            return allActionTypes.First(x => x.Name.ToLower(new CultureInfo("en-US", false)).Equals(a.ActionInstance.Name.ToLower(new CultureInfo("en-US", false))) && x.NumOfVariables == a.ActionInstance.Variables.Length);

        }

        private ActionType FindActionType(List<ActionType> allActionTypes, string name, int vars)
        {
            if (vars > 0)
            {
                return allActionTypes.First(x => x.Name.ToLower(new CultureInfo("en-US", false)).Equals(name.ToLower(new CultureInfo("en-US", false))) && x.Vars != null && x.Vars.Count == vars);
            }
            else
            {
                if (allActionTypes.FirstOrDefault(x => x.Name.ToLower(new CultureInfo("en-US", false)).Equals(name.ToLower(new CultureInfo("en-US", false)))) == null)
                {
                    string ErrorMessage = "This action " + name + " is not described anywhere in the domain file.";
                    throw new ActionException(ErrorMessage);
                }
                return allActionTypes.First(x => x.Name.ToLower(new CultureInfo("en-US", false)).Equals(name.ToLower(new CultureInfo("en-US", false))) && (x.Vars == null || x.Vars.Count == 0));
            }
        }


        private void HandleParameters(String line, ref Rule curRule, ref Dictionary<String, Constant> paramTypeInfo)
        {
            if (line.Contains(":parameters"))
            {
                line = line.Replace(":htn ", "");
                paramTypeInfo = GetParameters(line, allConstantTypes);
                if (paramTypeInfo != null)
                {
                    curRule.AllVars = paramTypeInfo.Keys.ToList();//This must be ready for constants
                    curRule.AllVarsTypes = paramTypeInfo.Values.Select(x => x.Type).ToList();
                }
            }
        }


        /// <summary>
        /// Read the file explaining the problem.
        /// </summary>
        /// <param name="fileName"></param>
        public List<Term> ReadProblem(String fileName, Dictionary<String, ConstantType> allConstantTypes, ref Dictionary<String, Constant> constants, out Rule goalRule, out List<Term> goalState)
        {
            goalState = new List<Term>();
            goalRule = new Rule();
            Dictionary<String, Constant> inputConstants = new Dictionary<String, Constant>();
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            String line;
            List<Term> conditions = new List<Term>();
            bool inInit = false;
            bool inObjects = false;
            bool inGoalTask = false;
            bool inSubtasks = false;
            bool inOrdering = false;
            bool inGoalState = false;
            bool unknownOrdering = true;
            List<List<int>> referenceLists = new List<List<int>>();
            List<TaskType> curSubtaskList = new List<TaskType>();
            Ordering ordering = Ordering.None;
            List<Tuple<TaskType, string, int>> namedTasks = new List<Tuple<TaskType, string, int>>();
            Dictionary<String, Constant> paramTypeInfo = new Dictionary<String, Constant>();
            while ((line = file.ReadLine()) != null)
            {
                if (Globals.IgnoreCase) line = line.ToLower(new CultureInfo("en-US", false));
                if (line.Trim().Equals(")") && inInit)
                {
                    //return conditions
                    inInit = false;
                }

                if (line.Contains(":init"))
                {
                    //We are now in inInit so if there was a goal task we are done with it. 
                    if (Globals.KnownRootTask)
                    {

                        goalRule = FinishGoalRule(goalRule, referenceLists, paramTypeInfo);
                        inSubtasks = false;
                    }
                    inInit = true;
                }
                if (line.Contains(":goal") || inGoalState)
                {
                    if (line.Trim().Equals(")"))
                    {
                        inGoalState = false;
                    }
                    else
                    {
                        //This part described the goal state. This part only matters if the user wants to check the goal state. 
                        if (Globals.CheckGoalState)
                        {
                            line = line.Replace("(:goal", "");
                            string[] parts = Regex.Split(line, @"(?=\()");
                            foreach (string part in parts)
                            {
                                if (part != "(and")
                                {
                                    Term c = CreateStateCondition(part, ref inputConstants, constants);
                                    if (c != null) goalState.Add(c);
                                }
                            }
                            inGoalState = true;
                            inInit = false;
                        }
                    }
                }
                if (line.Contains(":htn"))
                {
                    inGoalTask = true;
                    inObjects = false;
                }
                if (line.Contains(":objects")) inObjects = true;
                else if (inInit)
                {
                    string[] parts = Regex.Split(line, @"(?=\()");
                    foreach (string part in parts)
                    {
                        Term c = CreateStateCondition(part, ref inputConstants, constants);
                        if (c != null) conditions.Add(c);
                    }
                }
                else if (inObjects)
                {
                    if (line.Trim().Equals(")"))
                    {
                        inObjects = false;
                        AddNewConstants(inputConstants, ref constants); //Adds inputconstants in constants. Check uniqueness and substitute constantswith type any if possible.                         
                    }
                    GetConstants(line, ref inputConstants, allConstantTypes);
                }
                else if (inGoalTask && Globals.KnownRootTask)
                {
                    int num = 0;
                    //parameters are always just one line even if they are in the problem file. 
                    if (line.Contains(":parameters"))
                    {
                        HandleParameters(line, ref goalRule, ref paramTypeInfo);
                    }
                    else if (line.Contains("ordering") || inOrdering)
                    {
                        inSubtasks = false;
                        if (!line.Trim().Equals(")"))
                        {
                            if (namedTasks?.Count < 1)
                            {
                                Console.WriteLine("Warning: There is \"ordering()\" in problem file, but no actual ordering.");
                            }
                            else
                            {
                                CreatePartialOrder(line, namedTasks, ref goalRule);
                            }
                        }
                        if (line.Trim().Equals(")"))
                        {
                            ordering = Ordering.None;
                            if (Globals.TOIndicator && !IsFullyOrdered(goalRule)) Globals.TOIndicator = false;
                            goalRule.FinishPartialOrder();
                            inOrdering = false;
                        }

                    }
                    if (Globals.KnownRootTask && line.Contains("ordered-subtasks")) ordering = Ordering.Preset;
                    if (Globals.KnownRootTask && line.Contains("subtasks"))
                    {
                        //This means that first task cant be on the same line as subtasks. 
                        inSubtasks = true;
                        //If the line looks like this: :subtasks (and (t__top))
                        // We need this (t__top)
                        //If line looks like this  :subtasks (and (t__top) and then continues below
                        //then we need this: (t__top)
                        //If line looks like this:  :subtasks (and
                        //we want nothing 
                        line = line.Replace(":subtasks (and", "");
                        line = line.Trim().Replace("))", ")");
                    }
                    if (line.Trim().Equals(")") && inSubtasks)
                    {
                        inSubtasks = false;
                    }
                    if (Globals.KnownRootTask && inSubtasks && line != "")
                    {
                        if (unknownOrdering && ordering != Ordering.Preset)
                        {
                            //Only check this if ordering is not preset. Cause then I know the ordering. 
                            int parenthesisCount = line.Count(x => x == '(');
                            if (parenthesisCount > 1) ordering = Ordering.Later;
                            else ordering = Ordering.None;
                            unknownOrdering = false;
                        }
                        List<int> refList = new List<int>();
                        TaskType tT;
                        if (ordering == Ordering.None || ordering == Ordering.Preset)
                        {
                            tT = CreateTaskType(line, ref paramTypeInfo, out refList, allConstants);
                            if (tT != null) tT = FindTask(tT, alltaskTypes);
                        }
                        else
                        {
                            Tuple<TaskType, string> tupleTaskName = CreateNamedTaskType(line, ref paramTypeInfo, out refList, allConstants);
                            Tuple<TaskType, string, int> tupleFull = new Tuple<TaskType, string, int>(tupleTaskName.Item1, tupleTaskName.Item2, num);
                            namedTasks.Add(tupleFull);
                            tT = FindTask(tupleTaskName.Item1, alltaskTypes);  //Finds the task in lists of all tasks.
                            num++;
                        }
                        if (tT != null)
                        {
                            curSubtaskList.Add(tT);
                            referenceLists.Add(refList);
                            goalRule.TaskTypeArray = curSubtaskList.ToArray();
                        }
                    }

                }
            }
            return conditions;
        }

        private Rule FinishGoalRule(Rule goalRule, List<List<int>> referenceLists, Dictionary<String, Constant> paramTypeInfo)
        {
            if (paramTypeInfo != null)
            {
                goalRule.AllVars = paramTypeInfo.Keys.ToList();
                goalRule.AllVarsTypes = paramTypeInfo.Values.Select(x => x.Type).ToList();
            }
            TaskType GoalTask = new TaskType("GoalTask", 0, new HashSet<Task>(), new List<Rule>(), goalRule);
            goalRule.MainTaskType = GoalTask;
            goalRule.MainTaskReferences = new List<int>();
            foreach (TaskType t in goalRule.TaskTypeArray)
            {
                t.Rules.Add(goalRule);
            }
            goalRule.Finish(referenceLists);
            return goalRule;
        }

        private void AddNewConstants(Dictionary<String, Constant> inputConstants, ref Dictionary<String, Constant> constants)
        {
            foreach (Constant c in inputConstants.Values)
            {
                Constant sameName = Globals.NullLookUp(constants, c.Name);
                if (sameName == null) constants.Add(c.Name, c); //If my type is subset of a previous type. We change it. 
                else
                {
                    if (sameName.Type.IsAncestorTo(c.Type))
                    {
                        sameName.Type = c.Type;
                    }
                    if (sameName.Type.Name == "any") sameName.Type = c.Type; //We change the type to my type as we now know better what constant this is. 
                }
            }
        }

        //The line loks like this: (contentof pot1 contentpot1)
        private Term CreateStateCondition(string line, ref Dictionary<String, Constant> methodInfo, Dictionary<String, Constant> allConstants)
        {
            Tuple<Term, bool> tupleC = CreateCondition(line, ref methodInfo, allConstants);
            if (tupleC != null)
            {
                if (tupleC.Item2)
                {// we only remember positive initial conditions. Negative just means that it's not in the list of positive ones. 
                    Term c = tupleC.Item1;
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// From a file with a solution creates actionInstance which is an action. 
        /// </summary>
        private Term CreateActionInstance(String s, List<ActionType> allActionTypes, Dictionary<String, Constant> allConstants)
        {
            s = s.Replace(")", "");
            string[] parts = s.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (parts.Length == 0)
            {
                return null;
            }
            else
            {
                string name = parts[0];
                string[] variables = (string[])parts.Skip(1).ToArray();
                Constant[] vars = new Constant[variables.Length];
                ActionType m = FindActionType(allActionTypes, name, variables.Length);
                for (int i = 0; i < variables.Length; i++)
                {
                    Constant c = FindConstant(variables[i], allConstants);
                    vars[i] = c;
                }
                Term actionInstance = new Term(name, vars);
                return actionInstance;
            }
        }

        public List<TaskType> TransformToList(Dictionary<String, List<TaskType>> allTaskTypes)
        {
            List<TaskType> output = new List<TaskType>();
            foreach (List<TaskType> list in alltaskTypes.Values)
            {
                output.AddRange(list);
            }
            return output;
        }
    }
}
