using PlanValidationExe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Represents a method. 
    /// </summary>
    class Rule
    {
        private TaskType mainTaskType;
        internal TaskType MainTaskType
        {
            get { return mainTaskType; }
            set
            {
                mainTaskType = value;
                //This tells the main task type that I am it's main rule.
                mainTaskType.MainRules.Add(this);
            }
        }
        internal TaskType[] TaskTypeArray;
        /// <summary>
        /// If true then this rule is either connected to the root task or to some task to which root task decomposes.
        /// If this is false, it is a separate rule/task, so it won't help me get desired root task. Therefor I can remove this rule. 
        /// </summary>
        internal bool reachable = false;
        /// <summary>
        /// Has 1 if the given task type has at least one task instance 
        /// </summary>
        bool[] TaskTypeActivationArray;
        int[] TaskTypeActivationIterationArray;
        int[] TaskMinLegthArray; //This is set to fixed 100000. 
        int[] minOrderedTaskPositionAfter;
        int[] minOrderedTaskPosition;
        int ActivatedTasks;

        /// <summary>
        /// One list represents one task and the numbers in him say which variable of all vars this corresponds to. So for example for rule:
        /// Transfer(L1,C,R,L2):-Load(C,L1,R),Move(R,L1,L2),Unload(C,L2,R) with all vars (L1,C,R,L2)
        /// The array looks like this{(1,0,2),(2,0,3),(1,3,2)}.
        /// 
        /// </summary>
        public List<int>[] ArrayOfReferenceLists;

        internal void NotifyOfRemoval()
        {
            mainTaskType.RemoveRule(this);
            foreach (TaskType t in TaskTypeArray)
            {
                t.RemoveRule(this);
            }
        }

        //References from main task to allVars.
        public List<int> MainTaskReferences;

        /// <summary>
        /// All variables used in this rule (in main task or any subtask)
        /// </summary>
        public List<String> AllVars = new List<string>();
        public List<ConstantType> AllVarsTypes = new List<ConstantType>();

        /// The first int says to which of the rule's subtasks this applies to,
        /// the string is the name of the condition and the list of ints i the references to the variables in this rule.
        /// -1 means it must be before all the rules of this subtask.
        /// So for example for condition at(C,L1) for load(C,L1,R)  we
        /// have tuple (0,at,(0,1))
        /// </summary>
        public List<Tuple<int, String, List<int>>> posPreConditions;
        public List<Tuple<int, String, List<int>>> negPreConditions;

        /// <summary>
        /// The first int says to which of the rule's subtasks this applies to,the string is the name of the condition and the list of ints i the references to the action variables.
        /// So for example for condition at(C,L1) for load(C,L1,R)  we
        /// have tuple (0,at,(0,1))
        /// </summary>
        public List<Tuple<int, String, List<int>>> posPostConditions;
        public List<Tuple<int, String, List<int>>> negPostConditions;

        /// <summary>
        /// For between condition, we have two ints representing which actions they are related too. Then name of condition. Then lists of int representing to which variables they are related to.
        /// 
        /// So for example for condition on(R,C) between Load(C,L1,R) and Unload(C,L2,R) would be this: (0,2,on,(2,0),(2,0))
        /// </summary>
        public List<Tuple<int, int, String, List<int>>> posBetweenConditions;
        public List<Tuple<int, int, String, List<int>>> negBetweenConditions;

        public List<Tuple<int, int>> orderConditions;

        /// <summary>
        /// Represents the number of subtask that are after this particual subtask based on ordering.         /// 
        /// </summary>
        public int[] numOfOrderedTasksAfterThisTask;

        /// <summary>
        /// Same as after this task except for Before. 
        /// </summary>
        public int[] numOfOrderedTasksBeforeThisTask;

        //For each subtask this is a list of subtasks after it
        private List<int>[] listAfter;
        //For each substak this is a list of subtask before it. 
        private List<int>[] listBefore;

        private SubtaskFillingHeuristic myHeuristic;

        public Rule()
        {
            posPreConditions = new List<Tuple<int, string, List<int>>>();
            negPreConditions = new List<Tuple<int, string, List<int>>>();
            posPostConditions = new List<Tuple<int, string, List<int>>>();
            negPostConditions = new List<Tuple<int, string, List<int>>>();
            posBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            negBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
        }

        //It is given everything it wil be given. It should fill up the rest.
        //For example must fill reference list and maintaskreferences.
        internal void Finish(List<List<int>> refList)
        {
            TaskTypeActivationArray = new bool[TaskTypeArray.Length];
            TaskTypeActivationIterationArray = new int[TaskTypeArray.Length];
            TaskMinLegthArray = Enumerable.Repeat(100000, TaskTypeArray.Length).ToArray();
            minOrderedTaskPositionAfter = new int[TaskTypeArray.Length];
            minOrderedTaskPosition = new int[TaskTypeArray.Length];
            ArrayOfReferenceLists = refList.ToArray();
            if (posPreConditions == null) posPreConditions = new List<Tuple<int, string, List<int>>>();
            if (negPreConditions == null) negPreConditions = new List<Tuple<int, string, List<int>>>();
            if (posPostConditions == null) posPostConditions = new List<Tuple<int, string, List<int>>>();
            if (negPostConditions == null) negPostConditions = new List<Tuple<int, string, List<int>>>();
            if (posBetweenConditions == null) posBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            if (negBetweenConditions == null) negBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            listAfter = new List<int>[TaskTypeArray.Length];
            listBefore = new List<int>[TaskTypeArray.Length];
            myHeuristic = CreateHeuristic(ArrayOfReferenceLists, AllVars);
        }

        private SubtaskFillingHeuristic CreateHeuristic(List<int>[] ArrayOfReferenceLists, List<String> AllVars)
        {
            SubtaskFillingHeuristic heuristic;
            if (Globals.Heuristic == Globals.Heuristics.MostParameters)
            {
                return heuristic = new MostParametersHeuristic(ArrayOfReferenceLists, AllVars);
            }
            else if (Globals.Heuristic == Globals.Heuristics.LeastParameters)
            {
                return heuristic = new LeastParametersHeuristic(ArrayOfReferenceLists, AllVars);
            }
            else if (Globals.Heuristic == Globals.Heuristics.Instances)
            {
                return heuristic = new InstancesHeuristic(TaskTypeArray);
            }
            else
            {
                return heuristic = new OriginalOrderHeuristic();
            }

        }



        /// <summary>
        /// Marks itself, it's main task and all it's subtasks as reached. 
        /// </summary>
        public void MarkAsReached()
        {
            if (reachable == false)
            {
                reachable = true;
                foreach (TaskType t in TaskTypeArray)
                {
                    t.MarkAsReached();
                }
                if (!MainTaskType.reachable) mainTaskType.MarkAsReached();
            }
        }

        private void CalculateTaskMinMaxPosition()
        {
            if (orderConditions?.Any() == true)
            {
                for (int i = 0; i < listAfter.Length; i++)
                {
                    int sum = 0;
                    if (listAfter[i] != null)
                    {
                        for (int j = 0; j < listAfter[i].Count; j++)
                        {
                            sum += TaskMinLegthArray[listAfter[i][j]];
                        }
                    }
                    minOrderedTaskPositionAfter[i] = sum;
                    sum = 0;
                    if (listBefore[i] != null)
                    {
                        for (int j = 0; j < listBefore[i].Count; j++)
                        {
                            sum += TaskMinLegthArray[listBefore[i][j]];
                        }
                    }
                    minOrderedTaskPosition[i] = sum;
                }
            }
        }

        /// <summary>
        /// True means tasks after false means tasks before. 
        /// </summary>
        /// <returns></returns>
        private List<List<int>> CreateListsOfTasks(bool after)
        {
            List<List<int>> indexOfTasksAfter = new List<List<int>>(TaskTypeArray.Length); //Represents the index of tasks that are ordered with this task. Only immediate level. Meaning if I have ordering 1<2 and 2<3. Index 1 only has 2 there. 
            for (int i = 0; i < TaskTypeArray.Length; i++) indexOfTasksAfter.Add(null);
            for (int i = 0; i < TaskTypeArray.Length; i++)
            {
                List<int> tupledWith;
                if (after)
                {
                    TupleWithXFirst(i, out tupledWith);
                    indexOfTasksAfter[i] = tupledWith;
                }
                else
                {
                    TupleWithXLast(i, out tupledWith);
                    indexOfTasksAfter[i] = tupledWith;
                }
            }
            return indexOfTasksAfter;
        }

        /// <summary>
        /// Adds one partial condition, if it is not already transitively implied. 
        /// returns true if this was a new relation (not transitively implied)
        /// isExplicit determines whether this is a condition given to us by the user. 
        /// </summary>
        public bool AddPartialCondition(int first, int second, bool isExplicit)
        {
            //Unfortunately some trasnitive relations can still slip through with the wrong ordering for exmaple A < c, A<B,B<cthese wil be removed in finish partial ordering. 
            if (listAfter[first] != null && listAfter[first].Contains(second))
            {
                //This condition is implied we can ignore it. 
                return false;
            }
            else
            {
                if (listAfter[first] == null) listAfter[first] = new List<int>();
                if (!listAfter[first].Contains(second)) listAfter[first].Add(second);
                if (listBefore[second] == null) listBefore[second] = new List<int>();
                if (!listBefore[second].Contains(first)) listBefore[second].Add(first);
                if (listBefore[first] != null)
                {
                    foreach (int i in listBefore[first])
                    {
                        AddPartialCondition(i, second, false);
                    }
                }
                if (listAfter[second] != null)
                {
                    foreach (int i in listAfter[second])
                    {
                        AddPartialCondition(first, i, false);
                    }
                }
                //This was a condition representing a new relation 
                if (isExplicit)
                {
                    (orderConditions ?? (orderConditions = new List<Tuple<int, int>>())).Add(new Tuple<int, int>(first, second));
                    //If orderConditions was null it will initialize itself. 
                }
            }
            return true;

        }

        /// <summary>
        /// Returns the number of ordering tuples where this number is first.
        /// In the tupledWithList returns the indexof tasks it's ordered with. 
        /// </summary>
        /// <returns></returns>
        private int TupleWithXFirst(int index, out List<int> tupledWith)
        {
            List<Tuple<int, int>> rightTuples = orderConditions.Where(x => x.Item1 == index).ToList();
            tupledWith = rightTuples.Select(x => x.Item2).ToList();
            return tupledWith.Count;
        }

        /// <summary>
        /// Returns the number of ordering tupes where this number is first.
        /// In the tupledWithList returns the indexof tasks it's ordered with. 
        /// </summary>
        /// <returns></returns>
        private int TupleWithXLast(int index, out List<int> tupledWith)
        {
            List<Tuple<int, int>> rightTuples = orderConditions.Where(x => x.Item2 == index).ToList();
            tupledWith = rightTuples.Select(x => x.Item1).ToList();
            return tupledWith.Count;
        }

        private void CalculateActionsAfter()
        {
            numOfOrderedTasksAfterThisTask = new int[TaskTypeArray.Length];
            numOfOrderedTasksBeforeThisTask = new int[TaskTypeArray.Length];
            if (orderConditions?.Any() == true)
            {
                for (int i = 0; i < listAfter.Length; i++)
                {
                    if (listAfter[i] == null) numOfOrderedTasksAfterThisTask[i] = 0;
                    else numOfOrderedTasksAfterThisTask[i] = listAfter[i].Count;
                    if (listBefore[i] == null) numOfOrderedTasksBeforeThisTask[i] = 0;
                    else numOfOrderedTasksBeforeThisTask[i] = listBefore[i].Count();
                }
            }
        }

        /// <summary>
        /// Returns true if after activating this task the rule is ready to be used. 
        /// Int j says how many instances maximum I can fill in this rule. 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool Activate(TaskType t, int j, int iteration)
        {
            List<int> occurences = Enumerable.Range(0, TaskTypeArray.Length).Where(p => TaskTypeArray[p] == t).ToList();
            if (occurences.Count > j) return false; //I cant fill all instances of this subtask in this rule so it definitely canot be used.
            else
            {
                foreach (int i in occurences)
                {
                    if (!TaskTypeActivationArray[i]) ActivatedTasks++; //If this activated the task (as in it was not ready before) it should increase the activated task counter.
                    TaskTypeActivationArray[i] = true;
                    TaskTypeActivationIterationArray[i] = iteration; //Iterations always increases over time. So if I had a different task here in iteration 4, that's fine now I rewrite it to 6.
                    if (t.MinTaskLength < TaskMinLegthArray[i]) TaskMinLegthArray[i] = t.MinTaskLength;
                }
                if (!TaskMinLegthArray.Contains(100000))
                {
                    int sum = TaskMinLegthArray.Sum();
                    bool changed = MainTaskType.SetMinTaskLengthIfSmaller(sum);
                    if (changed) CalculateTaskMinMaxPosition();
                }
            }

            return ActivatedTasks == TaskTypeActivationArray.Length;
        }

        /// <summary>
        /// Order is fixed from the position of subtasks. This creates the orderConditions.  
        /// </summary>
        internal void CreateFullOrder()
        {
            orderConditions = new List<Tuple<int, int>>();
            for (int i = 0; i < TaskTypeArray.Length - 1; i++)
            {
                int j = i + 1;
                Tuple<int, int> t = new Tuple<int, int>(i, j);
                orderConditions.Add(t);
                listAfter[i] = CreateListFromTo(i + 1, TaskTypeArray.Length);
                listBefore[i] = CreateListFromTo(0, i);
            }
            CalculateActionsAfter();
        }

        internal void FinishPartialOrder()
        {
            for (int i = 0; i < TaskTypeArray.Count(); i++)
            {
                if (listBefore[i] != null && listAfter[i] != null)
                {
                    //There is a condition that links a subtask before and after this subtask.  But because these subtasks are before and after our it is already transitively implied.
                    foreach (Tuple<int, int> c in orderConditions.Where(x => listBefore[i].Contains(x.Item1) && listAfter[i].Contains(x.Item2)).ToList())
                    {
                        orderConditions.Remove(c);
                    }
                }
            }
            CalculateActionsAfter();
        }

        /// <summary>
        /// Creates list with int values from i(indlucing) to v (excluding).  (1,2,3,4,5)
        /// If i is bigger than v then the list goes from top to bottm (5,4,3,2,1)
        /// </summary>
        /// <param name="i"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        private List<int> CreateListFromTo(int i, int v)
        {
            List<int> l = new List<int>();
            if (v <= i)
            {
                for (int j = v - 1; j >= i; j--)
                {
                    l.Add(j);
                }
            }
            else
            {
                for (int j = i; j < v; j++)
                {
                    l.Add(j);
                }
            }
            return l;
        }

        /// <summary>
        /// Returns combination of taskInstances from task types that works with this rule.
        /// Empty rules can go through this they will not return any ruleInstance.
        /// </summary>
        public HashSet<RuleInstance> GetRuleInstances(int size, List<Constant> allConstants, int iteration, int planSize)
        {
            if (myHeuristic is InstancesHeuristic)
            {
                InstancesHeuristic instheuristic = (InstancesHeuristic)myHeuristic;
                instheuristic.Recalculate(TaskTypeArray);
                myHeuristic = instheuristic;
            }
            Constant[] nullArray = new Constant[MainTaskType.NumOfVariables];
            for (int i = 0; i < nullArray.Length; i++)
            {
                nullArray[i] = null;
            }
            Term t = new Term(MainTaskType.Name, nullArray);
            Task MainTaskInstance = new Task(t, size, MainTaskType);
            List<Constant> emptyVars = FillFromAllVars(allConstants); //Fixed constants are fixed. Also forall constant is fixed. 
            List<Tuple<Task, Task[], List<Constant>>> ruleVariants = new List<Tuple<Task, Task[], List<Constant>>>();
            for (int i = 0; i < TaskTypeActivationIterationArray.Length; i++)
            {
                if (TaskTypeActivationIterationArray[i] == iteration)
                {
                    List<Tuple<Task, Task[], List<Constant>>> newvariants = GetNextSuitableTask(TaskTypeArray[i], -1, i, emptyVars, new Task[TaskTypeArray.Length], planSize, iteration); //Trying with emptz string with all vars it has error in fill maintask //Should this be new empty string or is allvars ok?
                    ruleVariants.AddRange(newvariants);
                }
            }
            HashSet<RuleInstance> ruleInstances = new HashSet<RuleInstance>();
            if (ruleVariants != null)
            {
                foreach (Tuple<Task, Task[], List<Constant>> ruleVariant in ruleVariants)
                {
                    if (ruleVariant.Item3.Contains(null)) //This might happen in multiple ways:
                                                          //1] main task has some parameter that none of its subtasks look at. Problem one we fill by creating a task with all possible constants. 
                                                          //2] there is a forall condition in my conditions. This will not happen as in emptyvars this value is filled. 
                    {
                        List<List<Constant>> newAllVars = FillWithAllConstants(ruleVariant.Item3, AllVarsTypes, allConstants, new List<List<Constant>>());
                        newAllVars = newAllVars.Distinct().ToList();
                        foreach (List<Constant> allVar in newAllVars)
                        {
                            //Aside from making the rule instance we must also fill the main task properly here. 
                            Task t2 = FillMainTaskFromAllVars(allVar);
                            RuleInstance ruleInstance = new RuleInstance(t2, ruleVariant.Item2.ToList(), this, allVar.Select(x => x.Name).ToList(), allConstants);
                            if (ruleInstance.IsValid())
                            {
                                ruleInstances.Add(ruleInstance);
                            }
                        }
                    }
                    else
                    {
                        RuleInstance ruleInstance = new RuleInstance(ruleVariant.Item1, ruleVariant.Item2.ToList(), this, ruleVariant.Item3.Select(x => x.Name).ToList(), allConstants);
                        if (ruleInstance.IsValid()) ruleInstances.Add(ruleInstance);
                    }
                }
            }
            return ruleInstances;
        }

        /// <summary>
        /// Fills nulls in rule with all possible constant that fit the type. returns as one big list of list of strings.
        /// </summary>
        /// <param name="item3"></param>
        /// <param name="allVarsTypes"></param>
        /// <param name="allConstants"></param>
        /// <returns></returns>
        public List<List<Constant>> FillWithAllConstants(List<Constant> item3, List<ConstantType> allVarsTypes, List<Constant> allConstants, List<List<Constant>> solution)
        {
            int i = item3.IndexOf(null);
            if (i == -1)
            {
                solution.Add(item3);
                return solution;
            }
            else
            {
                ConstantType desiredType = AllVarsTypes[i];
                List<Constant> fittingConstants = allConstants.Where(x => desiredType.IsAncestorTo(x.Type)).ToList();
                foreach (Constant c in fittingConstants)
                {
                    List<Constant> newAllVars = new List<Constant>(item3)
                    {
                        [i] = c
                    };
                    List<List<Constant>> newSolutions = FillWithAllConstants(newAllVars, allVarsTypes, allConstants, solution);
                    solution.AddRange(newSolutions);
                    solution = solution.Distinct().ToList();
                    newAllVars = new List<Constant>(item3);
                }
                return solution;
            }
        }

        /// <summary>
        /// Creates empty vars from all vars. Empty vars is a list that is empty and as big as allvars but filled where rule uses constant not variable. 
        /// variables start with ?. 
        /// </summary>
        /// <returns></returns>
        private List<Constant> FillFromAllVars(List<Constant> allConstants)
        {
            List<Constant> emptyVars = new List<Constant>(new Constant[AllVars.Count]);
            for (int i = 0; i < AllVars.Count; i++)
            {
                if (!AllVars[i].StartsWith("?"))
                {
                    Constant c = allConstants.Find(x => x.Name == AllVars[i] && AllVarsTypes[i].IsAncestorTo(x.Type)); //If this is forall constant it will return null.
                    if (AllVars[i].StartsWith("!")) c = new Constant(AllVars[i], AllVarsTypes[i]);
                    emptyVars[i] = c;
                }
            }
            return emptyVars;
        }

        /// <summary>
        /// This is used only for partial ordering. The listafter should already be done at this point. 
        /// </summary>
        /// <param name="item31"></param>
        /// <param name="item32"></param>
        internal void AddOrderCondition(int item31, int item32)
        {
            if (orderConditions == null) orderConditions = new List<Tuple<int, int>>();
            Tuple<int, int> t = new Tuple<int, int>(item31, item32);
            orderConditions.Add(t);
        }

        //This finds all applicable tasks from list.
        //Tuple item1 is main task, list of subtasks and allvars.       
        //will we have the same problem with conditions being of wrong type????
        //Index refers to position in heuristic. Mapped index referes to actual position in task type. Newindex also refers to actual position in tasktype. 
        private List<Tuple<Task, Task[], List<Constant>>> GetNextSuitableTask(TaskType t, int index, int newindex, List<Constant> partialAllVars, Task[] subtasks, int planSize, int curIteration)
        {
            bool doingNewtask = false;
            int mappedIndex;
            List<Task> unusedInstances = t.Instances.Except(subtasks).Distinct().ToList();
            if (index == -1) //Tasktype must be given as the new one. 
            {
                doingNewtask = true;
                unusedInstances = unusedInstances.Where(x => x.Iteration == curIteration).ToList();
                index = newindex; //Temporarily we change the index so we don't have to change everything else and then after we switch it back to -1.
                mappedIndex = newindex; //We still want to do the new subtask first. 
            }
            else
            {
                mappedIndex = myHeuristic.Mapping(index);
            }
            if (!doingNewtask && mappedIndex < newindex)    //This ensures that if I have rule with 2 newsubtasks I wont get it twice. 
                                                            //Anything after newindex can be both new and old. 
            {
                unusedInstances = unusedInstances.Where(x => x.Iteration < curIteration).ToList();
            }
            //If index is new index we keep it the way it is because we want to go with that first. But after we go from left to right. So we start with index 0. But index 0 means I want the first subtask according to my heuristic,
            //which might be on position 5. So I keep info on position 5.

            List<int> myReferences = ArrayOfReferenceLists[mappedIndex];
            List<Tuple<Task, Task[], List<Constant>>> myResult = new List<Tuple<Task, Task[], List<Constant>>>();
            List<Tuple<Task, Task[], List<Constant>>> newMyResult = null;
            //Subtasks is used as array not list some values are simply null. 
            if (subtasks != null)
            {
                for (int oldIndex = 0; oldIndex < subtasks.Length; oldIndex++)
                {
                    if (subtasks[oldIndex] != null)
                    {
                        Task l = subtasks[oldIndex];
                        //These are tasks I already picked for this instance.                        

                        //If this is explicitly before then for TO it must be right before. 
                        if (IsExplicitlyBefore(mappedIndex, oldIndex))
                        {
                            if (Globals.TOIndicator) unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) + 1 == Math.Ceiling(l.StartIndex)).ToList();
                            else unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < Math.Ceiling(l.StartIndex)).ToList();
                        }
                        else if (IsExplicitlyBefore(oldIndex, mappedIndex))
                        {
                            if (Globals.TOIndicator) unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) == Math.Floor(l.EndIndex) + 1).ToList();
                            else unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) > Math.Floor(l.EndIndex)).ToList();
                        }
                        else if (Globals.CheckTransitiveConditions)
                        {
                            if (IsTransitivelyBefore(mappedIndex, oldIndex))
                            {
                                unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < Math.Ceiling(l.StartIndex)).ToList(); //Our task must be before this subtask so I shall only look at possible instances that end before the other starts. 
                            }
                            else if (IsTransitivelyBefore(oldIndex, mappedIndex))
                            {
                                unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) > Math.Floor(l.EndIndex)).ToList(); //My task must start after task l.
                            }
                        }
                        unusedInstances = unusedInstances.Where(x => Differs(x.GetActionVector(), l.GetActionVector())).ToList(); //NO problem on empty task becasue they return null.
                                                                                                                                  //This is not the same as the sum check later. 
                    }
                }
            }

            if (numOfOrderedTasksAfterThisTask?[mappedIndex] > 0)
            {
                unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < planSize - minOrderedTaskPositionAfter[mappedIndex]).ToList(); //assuming plan size of action number. So for plan from 0-7 plan size is 8.
            }
            if (numOfOrderedTasksBeforeThisTask?[mappedIndex] > 0)
            {
                unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) >= minOrderedTaskPosition[mappedIndex]).ToList(); //>= because if normal task is on position 0 and so is empty task and normal task must be before empty task than its 0>=(1-1)
            } //This is okay even with empty tasks as they have minlegtharray of task 0

            if (doingNewtask) index = -1;
            foreach (Task tInstance in unusedInstances)
            {
                List<Constant> newAllVars = FillMainTask(tInstance, myReferences, partialAllVars);
                if (newAllVars != null)
                {
                    Task[] newSubTasks = (Task[])subtasks.Clone();
                    newSubTasks[mappedIndex] = tInstance;
                    //We just assigned the last task. 
                    if (index == TaskTypeArray.Length - 1 || (index + 1 == myHeuristic.ReverseMapping(newindex) && myHeuristic.ReverseMapping(newindex) == TaskTypeArray.Length - 1))
                    {
                        if (myResult == null) myResult = new List<Tuple<Task, Task[], List<Constant>>>();
                        //We must fill up the main task from allVars.
                        Task newMainTask = FillMainTaskFromAllVars(newAllVars);
                        Tuple<Task, Task[], List<Constant>> thisTaskSubTaskCombo = Tuple.Create(newMainTask, newSubTasks, newAllVars);
                        myResult.Add(thisTaskSubTaskCombo);
                    }
                    else
                    {
                        //We need to skip the newindex casue we already did it. So lets assume the new index has index 2 and its mappedIndex is 1. So we must make sure that we skip index=1
                        if (index + 1 == myHeuristic.ReverseMapping(newindex) && myHeuristic.ReverseMapping(newindex) < TaskTypeArray.Length - 1)
                        {//This makes us skip the new task. because we already picked the task for the new task. 
                            newMyResult = GetNextSuitableTask(TaskTypeArray[myHeuristic.Mapping(index + 2)], index + 2, newindex, newAllVars, newSubTasks, planSize, curIteration);
                        }
                        else
                        {
                            newMyResult = GetNextSuitableTask(TaskTypeArray[myHeuristic.Mapping(index + 1)], index + 1, newindex, newAllVars, newSubTasks, planSize, curIteration);
                        }
                        myResult.AddRange(newMyResult);
                    }
                }
            }
            return myResult;
        }

        /// <summary>
        /// Returns true if task with index must be in rule before the task with oldindex.
        /// If there is no ordering between them or if oldIndex task must be first we return false. 
        /// Does not take into account transitivity which would mess up the TO ordering system.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="oldIndex"></param>
        /// <returns></returns>
        private bool IsExplicitlyBefore(int index, int oldIndex)
        {
            if (orderConditions?.Any() != true) return false; //There is no ordering. 
            foreach (Tuple<int, int> tuple in orderConditions)
            {
                if (tuple.Item1 == index && tuple.Item2 == oldIndex) return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if task with index must be in the rule before task with odlindex. Includes transitivity. 
        /// So if A < B and B<c and we ask about A and C this returns true. As opposed to normal ISBefore which would return false.  
        /// </summary>
        /// <param name="index"></param>
        /// <param name="oldIndex"></param>
        /// <returns></returns>
        private bool IsTransitivelyBefore(int index, int oldIndex)
        {
            if (listAfter != null && listAfter[index] != null && listAfter[index].Contains(oldIndex)) return true;
            return false;
        }


        /// <summary>
        /// Returns true if arrays don't contain same elements. 
        /// </summary>
        /// <param name="usedActions1"></param>
        /// <param name="usedActions2"></param>
        /// <returns></returns>
        private bool Differs(bool[] usedActions1, bool[] usedActions2)
        {
            if (usedActions1?.Any() != true) return true;
            if (usedActions2?.Any() != true) return true;
            for (int i = 0; i < usedActions1.Length; i++)
            {
                if (i < usedActions2.Length && usedActions1[i] && usedActions2[i]) return false;
            }
            return true;
        }

        public Task FillMainTaskFromAllVars(List<Constant> myAllVars)
        {
            Term term = FillMainTaskFromAllVarsReturnTerm(myAllVars);
            Task t = new Task(term, MainTaskReferences.Count, MainTaskType);
            return t;
        }

        public Term FillMainTaskFromAllVarsReturnTerm(List<Constant> myAllVars)
        {
            String taskName = MainTaskType.Name;
            Constant[] vars = new Constant[MainTaskReferences.Count];
            for (int i = 0; i < MainTaskReferences.Count; i++)
            {
                vars[i] = myAllVars[MainTaskReferences[i]]; //Here we give the type from the task but we should give it the type of the constant.                 
            }
            Term term = new Term(taskName, vars);
            return term;
        }

        /// <summary>
        /// Tries to fill the allvars in this rule. Currently will not fillthe main task variables those will be filled retrospectively if the rule filling is correct.
        /// Returns new string[] which represents new allVars adjusted. If it didn't work returns null.
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="myReferences"></param>
        /// <param name="partialMainTask"></param>
        /// <param name="allVars"></param>
        /// <returns></returns>
        private List<Constant> FillMainTask(Task t, List<int> myReferences, List<Constant> allVars)
        {
            List<Constant> newAllVars = new List<Constant>(allVars);
            for (int i = 0; i < myReferences.Count; i++)
            {
                //First check if the type fits and then if so try to fill the variable in. 
                ConstantType desiredType = AllVarsTypes[myReferences[i]];
                Constant myVariable = allVars[myReferences[i]];
                if (allVars[myReferences[i]] == null)
                {
                    if (desiredType.IsAncestorTo(t.TaskInstance.Variables[i].Type))
                    {
                        newAllVars[myReferences[i]] = t.TaskInstance.Variables[i];
                    }
                    else return null;
                }
                else if (t.TaskInstance.Variables[i].Name != myVariable.Name || !desiredType.IsAncestorTo(t.TaskInstance.Variables[i].Type)) //in all vars this variable is already assigned and its not to the same value as my variable. So this task cannot be used. 
                                                                                                                                             //we must also check if it's the right type. if not return null 
                {
                    return null;
                }
            }
            return newAllVars;
        }

        public override string ToString()
        {
            string text = "";
            if (TaskTypeArray?.Any() == true) text = string.Join(",", TaskTypeArray.Select(x => x.Name));
            string text2 = string.Join(",", AllVars);
            string text3 = string.Join(",", posPreConditions.Select(x => x.Item2)) + string.Join(",", posPreConditions.Select(x => x.Item3));
            string text4 = string.Join(",", negPreConditions.Select(x => x.Item2)) + string.Join(",", negPreConditions.Select(x => x.Item3));
            string text5 = string.Join(",", posPostConditions.Select(x => x.Item2)) + string.Join(",", posPostConditions.Select(x => x.Item3));
            string text6 = string.Join(",", negPostConditions.Select(x => x.Item2)) + string.Join(",", negPostConditions.Select(x => x.Item3));
            string text7 = string.Join(",", posBetweenConditions.Select(x => x.Item3)) + string.Join(",", posBetweenConditions.Select(x => x.Item4));
            string text8 = string.Join(",", negBetweenConditions.Select(x => x.Item3)) + string.Join(",", negBetweenConditions.Select(x => x.Item4));
            String s = "Rule: " + this.MainTaskType.Name + " subtasks " + text + " parameters " + text2 + " posPreCond" + text3 + "negPreCond " + text4 + "posPostCond " + text5 + " negPostCond " + text6 + "posBetweenCond " + text7 + "negBetweenCond" + text8;
            return s;
        }
    }
}
