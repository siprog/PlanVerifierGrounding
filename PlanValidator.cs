using PlanValidationExe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Validates the plan. 
    /// </summary>
    internal class PlanValidator
    {
        /// <summary>
        /// Creates empty timeline of size j
        /// </summary>
        /// <param name="j">size of timeline</param>
        /// <returns></returns>
        private static List<Slot> CreateEmptyTimeline(int j)
        {
            List<Slot> slots = new List<Slot>();
            for (int i = 0; i < j; i++)
            {
                Slot s = new Slot();
                slots.Add(s);
            }
            return slots;
        }

        /// <summary>
        /// Retruns true/false depending on whether the plan is valid. 
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="allTaskTypes"></param>
        /// <param name="initialConditions"></param>
        /// <param name="allConstants"></param>
        /// <param name="emptyRules"></param>
        /// <returns></returns>
        public bool IsPlanValid(List<Action> plan, Dictionary<String, List<TaskType>> allTaskTypes, List<Term> initialConditions, List<Constant> allConstants, List<Rule> emptyRules,out int taskCount, Rule goalRule,List<Term> goalState, Stopwatch watch)
        {
            int iteration = 0;
            List<Slot> timeline = CreateEmptyTimeline(plan.Count+1);
            timeline[0].AddConditions(initialConditions); //Adds initial state assuming all initial conditions are only positive. 
            CheckNullConditions(plan[0], timeline[0]);
            //if (plan[0].PosPreConditions?.Count > 0) timeline[0].AddConditions(plan[0].PosPreConditions); This was here before why. I think its wrong
            if (!timeline[0].SharesAllItems(plan[0].PosPreConditions))
            {
                Console.WriteLine("This Action {0} does not have it's preconditions {1}. Plan is invalid.", plan[0], string.Join(",", plan[0].PosPreConditions));
                taskCount = 0;
                return false;
            }
            if (timeline[0].SharesItems(plan[0].NegPreConditions))
            {
                Console.WriteLine("This Action's {0} negative precondition is present. Plan is invalid.", plan[0]);
                taskCount = 0;
                return false;
            }
            //Forward run
            //Slightly rewritten due to the fact that we have intial state. 
            //When it had initial state the original version did not work as when action removed something it returned false. But it only removes something from initial state. 
            for (int i = 1; i < timeline.Count; i++)
            {
                timeline[i].AddLeftOverConditions(timeline[i - 1].Conditions, plan[i - 1].NegEffects);
                timeline[i].AddConditions(plan[i - 1].PosEffects);
                if (i < plan.Count)
                {
                    CheckNullConditions(plan[i], timeline[i]); //Check if any of these conditions contain null. If so it's an exists condition and I can simply look if it contains another one that fits it if so remove this condition. 

                    if (!timeline[i].SharesAllItems(plan[i].PosPreConditions))
                    {
                        Console.WriteLine("This Action {0} does not have it's preconditions {1}. Plan is invalid.", plan[i], string.Join(",", plan[i].PosPreConditions));
                        taskCount = 0;
                        return false;
                    }
                    if (timeline[i].SharesItems(plan[i].NegPreConditions))
                    {
                        Console.WriteLine("This Action's {0} negative precondition is present. Plan is invalid.", plan[i]);
                        taskCount = 0;
                        return false;
                    }
                }
            }
            if (Globals.CheckGoalState)
            {
                if (!CheckGoalState(timeline[timeline.Count - 1], goalState))
                {
                    Console.WriteLine("The goal state does not match the final state after the plan. If you don't want to check the goal state and simply want to find any task that decomposes into your given plan, run the program without the parameter {0}", Globals.KnownGoalTaskS);
                    Console.WriteLine("Plan is invalid.");
                    taskCount = 0;
                    return false;
                }
            }
            HashSet<Task> newTasks = new HashSet<Task>();
            HashSet<Task> allTasks = new HashSet<Task>();
            HashSet<Task> emptyTasks = CreateEmptyTasks(emptyRules, timeline, allConstants);
            newTasks.UnionWith(emptyTasks);            
            int position = 0; //position in plan.

            //Transforms action into tasks. 
            foreach (Action a in plan)
            {
                TaskType taskType = FindTaskType(a, allTaskTypes);
                bool[] array = new bool[plan.Count];
                array[position] = true;
                Task t = new Task(a.ActionInstance, array, taskType)
                {
                    Iteration = -1
                };
                t.TaskType.SetMinTaskLengthIfSmaller(1);
                t.TaskType.AddInstance(t);
                t.BufferZoneIndex = position;
                newTasks.Add(t);
                position++;
            }
            HashSet<Task> everytask= newTasks;
            //Main loops where we find applicable rules from newTasks and then use them to create new set of tasks.  
            while (newTasks?.Any() == true)
            {
                int notNew = 0;
                //time = watch.ElapsedMilliseconds;   
                //newTasks = newTasks.Distinct().ToList(); Hashsets are always distinct               
                List <Rule> applicableRules = GetApplicableRules(newTasks, iteration - 1);
                applicableRules = applicableRules.Distinct().ToList();
                applicableRules = applicableRules.Except(emptyRules).ToList(); //We have already created basic empty rules. We dont want to create them again. 
                allTasks.UnionWith(newTasks);
                newTasks = new HashSet<Task>();                
                foreach (Rule r in applicableRules)
                {                    
                    HashSet<RuleInstance> ruleInstances = r.GetRuleInstances(plan.Count, allConstants, iteration - 1, plan.Count + 1);
                    foreach (RuleInstance ruleInstance in ruleInstances)
                    {                        
                        List<Task> subtasks = new List<Task>();
                        Term mainTaskName = ruleInstance.MainTask.TaskInstance;
                        subtasks = ruleInstance.Subtasks;
                        double min = FindMinIndex(subtasks); //Returns -1 if subtasks are empty
                        double max = FindMaxIndex(subtasks); //Returns -1 if subtasks are empty
                        bool[] mainTaskVector = new bool[plan.Count];
                        bool validNewTask = true;                        
                        //TO does not need to check intersections but since this create the maintaskvector there is no point in skipping this. It would be teh same amount of work. 
                        validNewTask=CreateActionVectorAndCheckIntersections(min, max, plan.Count, subtasks, ref mainTaskVector);
                                             
                        if (validNewTask)
                        {
                            if (Globals.TOIndicator || !Globals.Interleaving)
                            {
                                //Interlaving is forbidden
                                //So the sequence of subtasks must be continous
                                if (!CheckContinuity(mainTaskVector))
                                {
                                    validNewTask = false;
                                }
                            }
                            Tuple<bool, int> preconditionPosition;
                            preconditionPosition = GetandCheckPreconditionPos(ruleInstance, timeline, min);
                            if (!preconditionPosition.Item1)
                            {
                                validNewTask = false;                                
                            } 
                            if (!CheckBetweenConditions(ruleInstance, timeline, mainTaskVector)) validNewTask = false;
                            if (Globals.SometimeBeforeCond && !CheckBufferZones(ruleInstance, mainTaskVector)) validNewTask = false;
                            if (validNewTask)
                            {

                                Task t = new Task(mainTaskName, mainTaskVector, ruleInstance.MainTask.TaskType, min, max, iteration, preconditionPosition.Item2);
                                if (CheckNewness(everytask, t))
                                {                                   
                                    MarkSubtasks(subtasks);
                                    newTasks.Add(t);
                                    everytask.Add(t);
                                    if (IsGoalTask(t, goalRule))
                                    {
                                        taskCount = everytask.Count();
                                        return true;
                                    }
                                } else { notNew++; }
                            }
                        }
                    }                   
                }
                iteration++;
            }            
            taskCount = everytask.Count();
            return false;
        }

        /// <summary>
        /// Cheks whether multiple subtasks dont intersect (share an action that they decompose into)
        /// Min is minimal start index of subtasks and max is maximal end index of subtasks. 
        /// </summary>
        /// <returns></returns>
        private bool CreateActionVectorAndCheckIntersections(double min, double max, int planLength, List<Task> subtasks, ref bool[] mainTaskVector)
        {
            //Some empty tasks have position after the last action. So that's why the i is limited by plan size. 
            //Some empty tasks are before the first action with index -0.5 so the same thing. 
            for (int i = Math.Max(0, (int)Math.Round(min)); i <= Math.Min((int)Math.Round(max), planLength - 1); i++)
            {
                int sum = 0;
                foreach (Task t in subtasks)
                {
                    //Empty tasks have null everywhere so they are fine. 
                    sum += Convert.ToInt32(t.GetActionVector()[i]);
                }
                if (sum > 1)
                {
                    
                    return false;                    
                }               
                mainTaskVector[i] = (sum == 1);
            }
            return true;
        }



        /// <summary>
        /// If goal state is empty this just passes through. 
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="goalState"></param>
        /// <returns></returns>
        private bool CheckGoalState(Slot slot, List<Term> goalState)
        {
            foreach(Term t in goalState)
            {
                if (!slot.Conditions.Contains(t))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckContinuity(bool[] mainTaskVector)
        {
            int sequence=0;
            const int Init = 0;
            const int Started= 1;
            const int Stopped = 2;
            for(int i=0; i<mainTaskVector.Length;i++)
            {
                if (sequence == Init && mainTaskVector[i]) sequence=Started;
                if (sequence == Started && !mainTaskVector[i]) sequence=Stopped;
                if (sequence == Stopped && mainTaskVector[i]) return false; //This is not continuous subseqeunce. 
                //Empty task will fly through as that one does not even get to Started.
            }
            return true;
        }

        /// <summary>
        /// This is used to check exist conditions. 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="s"></param>
        private void CheckNullConditions(Action action, Slot s)
        {
            List<Term> nullConditions = action.PosPreConditions.Where(x => x.Variables.Contains(null)).ToList(); //We can only have exists conditions in preconditions of actions. 
            List<Term> fulfilledConditions = new List<Term>();
            if (nullConditions?.Any() == true)
            {
                foreach (Term c in nullConditions)
                {
                    List<Term> sameNameCond = s.Conditions.Where(x => x.EqualOrNull(c)).ToList(); //We found condition that satisfies this. 
                    if (sameNameCond != null) fulfilledConditions.Add(c);
                }
                if (fulfilledConditions != null) action.RemoveConditionsFromPreconditions(fulfilledConditions, true);
            }
            nullConditions = action.NegPreConditions.Where(x => x.Variables.Contains(null)).ToList(); //We can only have exists conditions in preconditions of actions. 
            fulfilledConditions = new List<Term>();
            if (nullConditions?.Any() == true)
            {
                foreach (Term c in nullConditions)
                {
                    List<Term> sameNameCond = s.Conditions.Where(x => x.EqualOrNull(c)).ToList();
                    if (sameNameCond == null) fulfilledConditions.Add(c); //We found no condition that would satisfy this. 
                }
                if (fulfilledConditions != null) action.RemoveConditionsFromPreconditions(fulfilledConditions, false);
            }
        }

        /// <summary>
        /// Returns true if this task is new. 
        /// </summary>
        /// <param name="newTasks"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool CheckNewness(List<Task> newTasks, Task t)
        {
            List<Task> sameNameTasks = newTasks.Where(x => x.TaskInstance.Equals(t.TaskInstance)).ToList();
            foreach (Task t1 in sameNameTasks)
            {
                if (t1.GetActionVector().SequenceEqual(t.GetActionVector()) && t1.GetStartIndex() == t.GetStartIndex() && t1.GetEndIndex() == t.GetEndIndex())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if this task is new. 
        /// </summary>
        /// <param name="newTasks"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool CheckNewness(HashSet<Task> newTasks, Task t)
        {
            return (!newTasks.Contains(t));
        }

        /// <summary>
        /// Creates tasks that have no subtasks. Their boolean number is dependant on which slot satisfies it's conditions.
        /// 
        /// </summary>
        private HashSet<Task> CreateEmptyTasks(List<Rule> emptyRules, List<Slot> timeline, List<Constant> allConstants)
        {
            Dictionary<Task,int> validTasksFromPrevIteration= new Dictionary<Task, int>(); //This is only relevant for sometime before conditions.
                                                                            // //Sometime before Get list of all suitable tasks from previous iteration. Get those that are not suitable in this iteration and put them in with buffer n-1. Then get new suitable tasks by keeping those that were not suitable and adding all new valid ones.  
            HashSet<Task> validTasks = new HashSet<Task>();
            foreach (Rule r in emptyRules)
            {
                for (int i = 0; i < timeline.Count; i++)
                {
                    //Constant[] emptyConst = new Constant[r.MainTaskType.NumOfVariables]; /
                    Constant[] emptyConst = new Constant[r.AllVars.Count];
                    List<Task> suitableTasks;
                    if (r.posPreConditions==null || r.posPreConditions?.Any(x=>x.Item2!="=" && x.Item2!="equal")!= true) suitableTasks = FillTaskWithNoPreconditions(r, timeline[i], emptyConst.ToList(), 0, i, timeline.Count, new List<Task>(), allConstants);
                    else
                    {
                        suitableTasks = FillTaskFromSlot(r, timeline[i], emptyConst.ToList(), 0, i, timeline.Count, new List<Task>(), allConstants, r.AllVarsTypes);
                    }

                    //Now I must check negative preconditions for these tasks. 
                    if (suitableTasks != null)
                    {
                        foreach (Task t in suitableTasks)
                        {
                            if (!t.TaskInstance.Variables.Contains(null))
                            {
                                RuleInstance rI = new RuleInstance(t, null, r, t.TaskInstance.Variables.Select(x => x.Name).ToList(), allConstants);
                                bool valid = CheckNegPreconditions(rI.NegPreConditions.Select(x => x.Item2).ToList(), timeline[i]);
                                if (valid) valid = rI.CheckEqualityOnly(r.posPreConditions, t.TaskInstance.Variables.Select(x => x.Name).ToList(), true);
                                if (valid) valid = rI.CheckEqualityOnly(r.negPreConditions, t.TaskInstance.Variables.Select(x => x.Name).ToList(), true); //TODO Possibly redundant
                                t.Iteration = -1;
                                t.TaskType.SetMinTaskLengthIfSmaller(0);
                                if (valid)
                                {

                                    if (CheckNewness(validTasks, t))
                                    {                                       
                                        validTasks.Add(t);
                                        if (Globals.SometimeBeforeCond)
                                        {
                                            Task key = validTasksFromPrevIteration.Keys.Where(x => x.TaskInstance.Equals(t.TaskInstance)).FirstOrDefault();
                                            //Returns the kez in the dictionary that is equal to my value. 
                                            if (key != null)
                                            {
                                                validTasksFromPrevIteration[key] = i;
                                            }
                                            else
                                            {
                                                validTasksFromPrevIteration.Add(t, i);
                                            }
                                        }
                                    }
                                    
                                }
                                t.BufferZoneIndex = (int)Math.Ceiling(t.StartIndex); //TODO how do we do empty tasks for bufferZones?
                            }
                        }
                    }
                    //Now I must add the empty tasks with n smaller than current slot. So for exmaple lets say empty task E was true in position 3 now I am at position 4 so I must add it with n =3.
                    if (Globals.SometimeBeforeCond)
                    {
                        foreach(var taskCombo in validTasksFromPrevIteration)
                        {
                            if (taskCombo.Value<i)
                            {
                                Task t = new Task(taskCombo.Key.TaskInstance, timeline.Count,taskCombo.Key.TaskType, i - 0.5, i - 0.5);
                                validTasks.Add(t);
                                t.BufferZoneIndex = taskCombo.Value;
                            }
                        }
                    }

                }
            }
            return validTasks;
        }

        private void MarkSubtasks(List<Task> subtasks)
        {
            if (subtasks != null)
            {
                foreach (Task t in subtasks)
                {
                    t.isSubtaskSomewhere = true;
                }
            }
        }

        public Task GetKeyEqualTo(Dictionary<Task,int> tasks, Task t)
        {
            int i = 0;
            foreach(Task task in tasks.Keys)
            {
                if (t.isEqualTo(task)) return task;
                i++;
            }
            return null;
        }

        /// <summary>
        /// Returns true if slot does not contain any of the negative conditions. 
        /// </summary>
        /// <param name="negConditions"></param>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool CheckNegPreconditions(List<Term> negConditions, Slot s)
        {
            foreach (Term c in negConditions)
            {
                List<Term> conditions = s.Conditions.Where(x => x.Name == c.Name).ToList();
                foreach (Term slotC in conditions)
                {
                    bool same = false;
                    if (slotC.Variables.Length == c.Variables.Length)
                    {
                        for (int i = 0; i < slotC.Variables.Length; i++)
                        {
                            if (slotC.Variables[i] == c.Variables[i])
                            {
                                same = true;
                            }
                        }
                        if (same) return false; //At least one negative condition is present in this slot. 
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns task created from empty rule r if the conditions fit the task otherwise return null. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="s"></param>
        /// <param name="partialAllVars"></param>
        /// <param name="index"></param>
        /// <param name="slotNumber"></param>
        /// <param name="taskBoolSize"></param>
        /// <param name="solution"></param>
        /// <param name="allConstants"></param>
        /// <param name="AllVarsTypes"></param>
        /// <returns></returns>
        private List<Task> FillTaskFromSlot(Rule r, Slot s, List<Constant> partialAllVars, int index, int slotNumber, int taskBoolSize, List<Task> solution, List<Constant> allConstants, List<ConstantType> AllVarsTypes)
        {
            //forall conditions. The parameter in the task has ! and the conditions relates to it. So we know it's forall. 
            List<Constant> newPartialVars = new List<Constant>(partialAllVars);
            if (index == r.posPreConditions.Count)
            {
                //I have checked all conditions. So create task from filled partialallvars.
                List<List<Constant>> newAllVars = new List<List<Constant>>();
                if (newPartialVars.Contains(null))
                {
                    newAllVars = r.FillWithAllConstants(newPartialVars, AllVarsTypes, allConstants, new List<List<Constant>>());
                }
                else { newAllVars.Add(newPartialVars); }
                foreach (List<Constant> partialVars in newAllVars)
                {
                    Term term = r.FillMainTaskFromAllVarsReturnTerm(partialVars);
                    //Term term = new Term(r.MainTaskType.Name, partialVars.ToArray()); //TODO to je cele vymenene za to nahore ,protoze partial vars jsou ted podle poctu parametru
                    Task t;
                    t = new Task(term, taskBoolSize, r.MainTaskType, slotNumber - 0.5, slotNumber - 0.5);
                    solution.Add(t);
                }
                return solution;
            }
            Tuple<int, string, List<int>> cond = r.posPreConditions[index];
            if (cond.Item2.Equals("equal") || cond.Item2.Equals("="))
            {
                //Simply skip equality conditions as they are checked in rule instance. 
                List<Task> newTasks = FillTaskFromSlot(r, s, newPartialVars, index + 1, slotNumber, taskBoolSize, solution, allConstants, AllVarsTypes);
                solution.AddRange(newTasks);
                solution = solution.Distinct().ToList();
                newPartialVars = new List<Constant>(partialAllVars);
                return solution; //There is no need to keep checking this condition. It is done. 
            }
            else
            {
                bool forall = false;
                for (int i = 0; i < cond.Item3.Count; i++)
                {
                    if (r.AllVars[cond.Item3[i]].StartsWith("!"))
                    {
                        forall = true;
                        //This is a positive forall condition.
                        //So all the conditions with the same name must be correct. 
                        if (CheckForallConditions(cond.Item2, r.AllVarsTypes[cond.Item3[i]], i, s, allConstants))
                        {
                            List<Task> newTasks = FillTaskFromSlot(r, s, newPartialVars, index + 1, slotNumber, taskBoolSize, solution, allConstants, AllVarsTypes);
                            solution.AddRange(newTasks);
                            solution = solution.Distinct().ToList();
                            newPartialVars = new List<Constant>(partialAllVars);
                            return solution; //There is no need to keep checking this condition. It is done. 
                        }
                        else
                        {
                            //forallcondition is invalid. This slot will not fulfill my empty task
                            //TODo couldprobably be improved by moving above as if forallcondition will not be fulfilled it doesn't matter if other conditions were fulfilled. 
                            return solution;
                        }
                    }
                }
                if (!forall)
                {
                    List<Term> conditions = s.Conditions.Where(x => x.Name == cond.Item2).ToList(); //conditions of same name in slot. 
                    if (conditions?.Any() != true)
                    {
                        return solution; //there is no condition that could fill my preconditions. I can skip this slot. It cannot fulfill my rule.
                    }
                    else
                    {
                        //We go through each conditions that has the same name as the desired condition of the rule. 
                        foreach (Term c in conditions)
                        {
                            //This conditions might fill my task. If so I must fill allvars in this task with appropiate string. 
                            //So now we go one by one through parameters. 
                            bool valid=true;  //Represents whether this particular condition fulfill my task in a valid way. 
                            for (int i = 0; i < cond.Item3.Count; i++)
                            {

                                ConstantType DesiredType = r.AllVarsTypes[cond.Item3[i]]; //cond.item3 refernces to what parameter in rule is the condition related. That parameter must be the right type. 
                                Constant myConst = c.Variables[i];
                                if (DesiredType.IsAncestorTo(myConst.Type))
                                {
                                    if (newPartialVars[cond.Item3[i]] == null) newPartialVars[cond.Item3[i]] = myConst;
                                    else if (newPartialVars[cond.Item3[i]] != myConst) valid=false;
                                } else
                                {
                                    //Here used to be return solution and return null  but both are wrong.
                                    valid=false;
                                }

                            }
                            if (valid)
                            {
                                //This is not a forall conditons, so any of these conditions can fill my task.
                                //The condition above is valid so I send it through. 
                                List<Task> newTasks = FillTaskFromSlot(r, s, newPartialVars, index + 1, slotNumber, taskBoolSize, solution, allConstants, AllVarsTypes);
                                solution.AddRange(newTasks);
                                solution = solution.Distinct().ToList();
                            }
                            //The condition is not valid, but we still have to remove any filling that we did from it. 
                            newPartialVars = new List<Constant>(partialAllVars);
                        }
                        return solution;
                    }
                }
                //We should never get here. 
                return solution;
            }
        }

        /// <summary>
        /// Return trues if in this slot all constants of given forallType are fulfilling given forallCondition. 
        /// </summary>
        /// <param name="forallCondition">name of the condition that we want to fulfill for all constants of given type</param>
        /// <param name="forallType">type of constants we want to fill the forall condition</param>
        /// <param name="forallPosition">position of the forall constant in condition parameters</param>
        /// <param name="s">Slot</param>
        /// <param name="allConstants">list of all consatnts</param>
        private bool CheckForallConditions(string forallCondition, ConstantType forallType,int forallPosition,Slot s, List<Constant> allConstants)
        {
            foreach (Constant c in allConstants.Where(x => forallType.IsAncestorTo(x.Type)))
            {
                if (s.Conditions == null) return false;
                bool conditionExists = s.Conditions.Any(x => x.Name == forallCondition && x.Variables[forallPosition] == c);
                if (!conditionExists) return false;
            }
            //All constants have the right condition. 
            return true;
        }

        /// <summary>
        /// Returns list of task with filled appropiate constants based on type of constant. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="s"></param>
        /// <param name="partialAllVars"></param>
        /// <param name="index"></param>
        /// <param name="slotNumber"></param>
        /// <param name="taskBoolSize"></param>
        /// <param name="solution"></param>
        private List<Task> FillTaskWithNoPreconditions(Rule r, Slot s, List<Constant> partialAllVars, int index, int slotNumber, int taskBoolSize, List<Task> solution, List<Constant> allConstants)
        {
            if (index == partialAllVars.Count)
            {
                //Term term = new Term(r.MainTaskType.Name, partialAllVars.ToArray());
                Term term= r.FillMainTaskFromAllVarsReturnTerm(partialAllVars);
                Task t;
                t = new Task(term, taskBoolSize, r.MainTaskType, slotNumber - 0.5, slotNumber - 0.5);
                solution.Add(t);
                return solution;
            }
            else
            {
                if (partialAllVars[index] != null)
                {
                    //This only happens if the empty rule has real constant as parameter. 
                    List<Task> newTasks = FillTaskWithNoPreconditions(r, s, partialAllVars, index++, slotNumber, taskBoolSize, solution, allConstants);
                    solution.AddRange(newTasks);
                    solution = solution.Distinct().ToList();
                }
                else
                {
                    ConstantType desiredType = r.AllVarsTypes[index];
                    List<Constant> fittingConstants = allConstants.Where(x => desiredType.IsAncestorTo(x.Type)).ToList();
                    if (r.AllVars[index].Contains("!")) {
                        //This is a forall condition.
                        //We dont have to worry this will be handled in ruleInstance. 
                        //WE just have to mark each constant with !
                        fittingConstants = fittingConstants.Select(x => { x.Name = "!" + x.Name; return x; }).ToList();
                    }
                    List<Constant> newPartialVars = new List<Constant>(partialAllVars);
                    foreach (Constant c in fittingConstants)
                    {
                        newPartialVars[index] = c;
                        List<Task> newTasks = FillTaskWithNoPreconditions(r, s, newPartialVars, index + 1, slotNumber, taskBoolSize, solution, allConstants);
                        solution.AddRange(newTasks);
                        solution = solution.Distinct().ToList();
                        newPartialVars = new List<Constant>(partialAllVars);
                    }
                }
                return solution;
            }
        }

        private bool CheckBetweenConditions(RuleInstance r, List<Slot> timeline, bool[] mainTaskVector)
        {
            foreach (Tuple<int, int, Term> tuple in r.PosBetweenConditions)
            {
                Term condition = tuple.Item3;
                //Get the actual slot number from the boolean vector and the number of the subtask in rule
                // int k = GetSlotNumber(tuple.Item1, mainTaskVector);
                //int l = GetSlotNumber(tuple.Item2, mainTaskVector);
                int k = (int)Math.Floor(r.Subtasks[tuple.Item1].EndIndex); //Why floor. if its a normal action then lets say its 4 then the conditions must be true from slot 5.
                //If it is an empty subtask. Then lets say the index is 4.5 it looks at its conditions on slot 5. The between conditions must also be true from slot 5. 
                int l = (int)Math.Ceiling(r.Subtasks[tuple.Item2].StartIndex); //The between condition ends on the same position that the empty task looks at its conditions. So for empty task on 4,5 which looks at slot 5 for preconditions it will also looks to 5.
                for (int i = k+1; i <= l; i++)
                {
                    if (!timeline[i].Conditions.Contains(condition)) return false;
                }
            }
            foreach (Tuple<int, int, Term> tuple in r.NegBetweenConditions)
            {
                Term condition = tuple.Item3;
                //Get the actual slot number from the boolean vector and the number of the subtask in rule
                int k = (int)Math.Floor(r.Subtasks[tuple.Item1].EndIndex);
                int l = (int)Math.Ceiling(r.Subtasks[tuple.Item2].StartIndex);
                for (int i = k+1; i <= l; i++)
                {
                    if (timeline[i].Conditions.Contains(condition)) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// This functsion should only be called if we do sometime before conditions. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        private bool CheckBufferZones(RuleInstance r,bool[] vector)
        {
            foreach(Task t in r.Subtasks)
            {
                for(int i=t.BufferZoneIndex;i<t.StartIndex;i++) //default value of buffer zone is on start index. So if the task has no preconditions then bufferYone is equal start index and it will go through here. 
                {
                    if (vector[i])
                    {
                        //Some other task is in this before zone and it invalidates my preconditions. 
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Check preconditions of a task. It returns whether conditions are satisfied and the slot number. 
        /// Note that for immediatelly before conditions if the slot number is not the start index then it return false,
        /// otherwise for sometime before conditions if the conditions are true at anz slot before the start index it will return true and the slot number. 
        /// </summary>
        /// <param name="r"></param>
        /// <param name="timeline"></param>
        /// <param name="StartIndex"></param>
        /// <returns></returns>
        private Tuple<bool,int> GetandCheckPreconditionPos(RuleInstance r, List<Slot> timeline, double StartIndex)
        {

            //Gets the start index of this task. 
            int i = (int)Math.Ceiling(StartIndex);
            int j = i; 
            int lowerLimit = i;// For immediately before conditions, the precondition must be true right before the first action. 
            if (Globals.SometimeBeforeCond) lowerLimit = 0;
            while (j <= i && j >= lowerLimit)
            {
                bool satisfiedConditions = CheckPreconditionsOnSlot(r, timeline[j]);
                if (satisfiedConditions)
                {
                    return new Tuple<bool, int>(true, j);
                }
                j--;
            }
            return new Tuple<bool,int>(false,0);
        }


        private bool CheckPreconditionsOnSlot (RuleInstance r, Slot t)
        {
            foreach (Tuple<int, Term> tuple in r.PosPreConditions)
            {
                Term condition = tuple.Item2;
                if (!condition.Name.Equals("="))//Already handled in ruleinstance. We can simply ignore it here. 
                {                   
                    if (!t.Conditions.Contains(condition))
                    {
                        return false;
                    }
                }
            }
            foreach (Tuple<int, Term> tuple in r.NegPreConditions)
            {
                Term condition = tuple.Item2;
                if (!condition.Name.Equals("=")) //Handled in ruleinstance
                {
                    if (t.Conditions.Contains(condition)) return false;
                }
            }
            return true;
        }

        private bool CheckPreconditions(RuleInstance r, List<Slot> timeline, bool[] mainTaskVector)
        {
            foreach (Tuple<int, Term> tuple in r.PosPreConditions)
            {
                Term condition = tuple.Item2;
                if (!condition.Name.Equals("="))//Already handled in ruleinstance. We can simply ignore it here. 
                {
                    //Get the actual slot number from the boolean vector and the number of the subtask in rule
                    int i = GetSlotNumber(tuple.Item1, mainTaskVector); //There is no slot number because its not related to any subtask. So it must be true on the first slot of this task.

                    if (!timeline[i].Conditions.Contains(condition))
                    {
                        return false;
                    }
                }
            }
            foreach (Tuple<int, Term> tuple in r.NegPreConditions)
            {
                Term condition = tuple.Item2;
                if (condition.Name.Equals("=")) Console.WriteLine("Special equality condition. Currently Ignoring");
                else
                {
                    //Get the actual slot number from the boolean vector and the number of the subtask in rule
                    int i = GetSlotNumber(tuple.Item1, mainTaskVector);
                    if (timeline[i].Conditions.Contains(condition)) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns actual slot number from boolean vector and the number of the subtasks in this rule.
        /// </summary>
        /// <param name="item1"></param>
        /// <param name="mainTaskVector"></param>
        /// <returns></returns>
        private int GetSlotNumber(int item1, bool[] mainTaskVector)
        {
            int count = -1;
            if (item1 == -1)
            {
                //This is related to the whole task so currently we take it that it must be true before the whole task but mabe it should remain trhough the whole task instead. 
                for (int i = 0; i < mainTaskVector.Length; i++)
                {
                    if (mainTaskVector[i]) return i;
                }
            }
            for (int i = 0; i < mainTaskVector.Length; i++)
            {
                if (mainTaskVector[i]) count++;
                if (count == item1) return i;
            }
            return -1;
        }

        private TaskType FindTaskType(Action a, Dictionary<String, List<TaskType>> allTaskTypes)
        {
            if (allTaskTypes.ContainsKey(a.ActionInstance.Name))
            {
                foreach (TaskType t in allTaskTypes[a.ActionInstance.Name])
                {
                    if (t.NumOfVariables == a.ActionInstance.Variables.Length)
                    {
                        if (Globals.KnownRootTask && !t.reachable)
                        {
                            Console.WriteLine("Given goal task does not decompose to this action, so this action is invalid.");
                            return null;
                        }
                        else return t;
                    }
                }
            }
            string ErrorMessage = "Error: No task type matches this action {0}" + a.ActionInstance;
            throw new ActionException(ErrorMessage);
            return null;
        }

        private double FindMaxIndex(List<Task> subtasks)
        {
            double curMax = -1;
            foreach (Task t in subtasks)
            {
                double eI = t.GetEndIndex();
                if (eI > curMax) curMax = eI;
            }
            return curMax;
        }

        private double FindMinIndex(List<Task> subtasks)
        {
            double curMin = -1;
            foreach (Task t in subtasks)
            {
                double eI = t.GetStartIndex();
                if (eI < curMin || curMin == -1) curMin = eI;
            }
            return curMin;
        }

        private List<Rule> GetApplicableRules(HashSet<Task> newTasks,int iteration)
        {
            List<Rule> readyRules = new List<Rule>();
            foreach (Task t in newTasks)
            {
                t.AddToTaskType();
                TaskType taskType = t.TaskType;
                List<Rule> taskRules = taskType.ActivateRules(iteration);
                readyRules.AddRange(taskRules);
            }
            return readyRules;
        }

        /// <summary>
        /// Goal task is any task that spans over the whole timeline.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool IsGoalTask(Task t,Rule goalRule)
        {           
                bool[] actionVector = t.GetActionVector();
                for (int i = 0; i < actionVector.Length; i++)
                {
                if (!actionVector[i])
                    return false;
                }                
                if (Globals.KnownRootTask)
                {
                //The task must span over all actions and be the goal task. 
                return t.TaskType.Equals(goalRule.MainTaskType);
                } else return true;
                //There is no goaltask so anz task that spans over all actions will do. 
        }
    }
}
