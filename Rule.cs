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
        internal int minDistanceToGoalTask;
        /// <summary>
        /// Has 1 if the given task type has at least one task instance 
        /// </summary>
        bool[] TaskTypeActivationArray;
        int[] TaskTypeActivationCreationNumberArray;
        int[] TaskMinLegthArray; //This is set to fixed 100000. 
        int[] minOrderedTaskPositionAfter;
        int[] minOrderedTaskPosition;
        int ActivatedTasks;

        int[] maximumPosition;
        int[] lastReachedStandardPosition;
        int[] lastReached;
        Queue<Tuple<int, int>> frozenQueue;
        /// <summary>
        /// These are the positions that we have used in our standard iterations. So for example before we even start this rule we might have 
        /// 10A and 5B so maximum for A is 10 and for B is 2.
        /// </summary>
        int[] maximumPositions;

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

        //Refrences from main task to allVars.
        public List<int> MainTaskReferences;

        /// <summary>
        /// All variables used in this rule (in main task or any subtask)
        /// </summary>
        public List<String> AllVars = new List<string>();
        public List<ConstantType> AllVarsTypes = new List<ConstantType>();

        /// The first int says to which of the rule's subtasks this applies to,the string is the name of the condition and the list of ints i the references to the variables in this rule.
        /// -1 means it must be before the all the rules of this subtask.
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
        /// Represents the number of subtasks that are after this particual subtask based on ordering.         /// 
        /// </summary>
        public int[] numOfOrderedTasksAfterThisTask;

        /// <summary>
        /// Same as after this task except for Before. 
        /// </summary>
        public int[] numOfOrderedTasksBeforeThisTask;

        //For each substask this is a list of subtasks after it
        private List<int>[] listAfter;
        //For each substak this is a list of subtask before it. 
        private List<int>[] listBefore;

        private SubtaskFillingHeuristic myHeuristic;

        public int LastCreationNumber;

        public Rule()
        {
            posPreConditions = new List<Tuple<int, string, List<int>>>();
            negPreConditions = new List<Tuple<int, string, List<int>>>();
            posPostConditions = new List<Tuple<int, string, List<int>>>();
            negPostConditions = new List<Tuple<int, string, List<int>>>();
            posBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            negBetweenConditions = new List<Tuple<int, int, string, List<int>>>();
            LastCreationNumber = 0;
        }

        //It as given everything it wil be given. It should fill up the rest.
        //For example must fill reference list and maintaskreferences.
        internal void Finish(List<List<int>> refList)
        {
            maximumPosition = Enumerable.Repeat(-1, TaskTypeArray.Length).ToArray();
            TaskTypeActivationArray = new bool[TaskTypeArray.Length];
            TaskTypeActivationCreationNumberArray = new int[TaskTypeArray.Length];
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
        public void MarkAsReached(int dis)
        {
            if (reachable == false)
            {
                minDistanceToGoalTask = dis;
                reachable = true;
                foreach (TaskType t in TaskTypeArray)
                {
                    t.MarkAsReached(dis + 1);
                }
                if (!MainTaskType.reachable) mainTaskType.MarkAsReached(dis);
            }
            else if (minDistanceToGoalTask > dis) minDistanceToGoalTask = dis;
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
            List<List<int>> indexOfTasksAfter = new List<List<int>>(TaskTypeArray.Length); //Represents the index of tasks that are ordered with this task. Only immediate level. Meaning if I have ordering 1<2 and 2<3. INdex 1 only has 2 there. 
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
        /// isExplicit determines whether this is a condition given to us by the user.         /// 
        /// </summary>
        public bool AddPartialCondition(int first, int second, bool isExplicit)
        {
            //Unfortunately some trasnitive relations can still slip through with the wrong ordering for example A < c, A<B,B<cthese wil be removed in finish partial ordering. 
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
        public bool Activate(TaskType t, int j, int creationNumber)
        {
            List<int> occurences = Enumerable.Range(0, TaskTypeArray.Length).Where(p => TaskTypeArray[p] == t).ToList();
            if (occurences.Count > j)
            {
                foreach (int i in occurences)
                {
                    UpdateQueue(i);
                }
                return false; //I cant fill all instances of this subtask in this rule so it definitely canot be used.
            }
            else
            {
                foreach (int i in occurences)
                {
                    if (!TaskTypeActivationArray[i]) ActivatedTasks++; //If this activated the task (as in it was not ready before) it should increase the activated task counter.
                    TaskTypeActivationArray[i] = true;
                    TaskTypeActivationCreationNumberArray[i] = creationNumber; //creation number always increases over time. So if I had a different task here with creation number 4, that is fine now I rewrite it to 6.
                    if (t.MinTaskLength < TaskMinLegthArray[i]) TaskMinLegthArray[i] = t.MinTaskLength;
                    UpdateQueue(i);
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

        private void UpdateQueue(int frozenPosition)
        {
            for (int i = 0; i < maximumPosition.Count(); i++)
            {
                if (i < frozenPosition && maximumPosition[i] >= 0)
                {
                    if (frozenQueue == null) frozenQueue = new Queue<Tuple<int, int>>();
                    frozenQueue.Enqueue(new Tuple<int, int>(frozenPosition, TaskTypeArray[frozenPosition].Instances.Count() - 1));
                    return;
                }
                else if (frozenPosition == i)
                {
                    //All previous variables are stil at starting positions so for example A1 B1 and we are now doing C which is frozen and we added C4.
                    //i don't need to add this to queue because i can just iterate towards it normally. 
                    return;
                }
            }
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
        public HashSet<RuleInstance> GetRuleInstances(int size, List<Constant> allConstants, int planSize)
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
            for (int i = 0; i < TaskTypeActivationCreationNumberArray.Length; i++)
            {
                //LastCreationAttemptNumber number tells me when was the last time this rule tried to create a task.
                //Any task with a higher creation number was created after and so is new. 
                //We care about new tasks in order to not  repeat same task mutliple times. 
                if (TaskTypeActivationCreationNumberArray[i] >= LastCreationNumber)
                {
                    List<Tuple<Task, Task[], List<Constant>>> newvariants = GetNextSuitableTask(TaskTypeArray[i], -1, i, emptyVars, new Task[TaskTypeArray.Length], planSize);
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
        /// Fills nulls in rule with all possible constants that fit the type. returns as one big list of list of strings.
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
        /// Creates empty vars from all vars. Empty vars is a list that is empty and as big as allvars but filled where rule uses constants not variables. 
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
                    Constant c = allConstants.Find(x => x.Name == AllVars[i] && AllVarsTypes[i].IsAncestorTo(x.Type)); //If this is forall consatnt it will return null.
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="maximumsPosition"> This says which combinations of subtask I already tried. Specifically the maximum instance number of task I tried with all the previous versions of previous subtasks. 
        /// So if I am at A4B3 now and I have 10A instances and 5Binstances. Then maximum for A is 4 and maximum for B is 5. Cause with A3 I tried all Bs from B1 to B5. So now if I get B 6 i know i have to go to queue and catch myself up to A4B3.</param>
        /// <param name="frozenQueue"></param>
        /// <param name="maximumPosition"></param>
        /// <param name="LastReachedStandard">this is the position I last reached through normal iteration not through frozen queue. So the A4B3 mentioned above.</param>
        /// <param name="frozenQueue"> first value is position of this new istance. (like which subtask) second value is the instances number</param>
        ///<param name="lastReached"> This is last reached including frozen queueue. for example if B6 is frozen this might be A1B6.
        ///
        /// <returns></returns>
        public RuleInstance GetNextRuleInstance(out bool triedAllCombinations, int planSize)
        {
            triedAllCombinations = false;
            int[] newPosition = null;
            List<Task> subtasks = null;
            int IncreasedVariable = 0;
            while (subtasks == null)
            {
                while (newPosition == null)
                {
                    if (frozenQueue != null && frozenQueue.Count() > 0)
                    {
                        Tuple<int, int> frozen = frozenQueue.Peek();
                        newPosition = GetNextComboFreeze(lastReachedStandardPosition, lastReached, frozen.Item1, frozen.Item2);
                        if (newPosition == null)
                        {
                            frozenQueue.Dequeue();
                            maximumPosition[frozen.Item1] = frozen.Item2;
                            lastReached = null;
                        }
                        else
                        {
                            lastReached = newPosition;
                        }
                    }
                    else
                    {
                        newPosition = GetNextComboNoFreeze(lastReachedStandardPosition, ref IncreasedVariable);
                        if (newPosition == null)
                        {
                            triedAllCombinations = true;
                            maximumPosition[IncreasedVariable] = TaskTypeArray[IncreasedVariable].Instances.Count() - 1; // We tried all combinations for this rule.
                            return null;
                        }
                        else
                        {
                            if (maximumPosition[IncreasedVariable] < newPosition[IncreasedVariable]) maximumPosition[IncreasedVariable] = newPosition[IncreasedVariable] - 1; //This allows for maximum position to be negative. If we haven't finished anything but that should  be okay.
                                                                                                                                                                              //Why the if? Well lets say we previously did A4B8C1 and now after some time we do A5B2C1 well B maximum should still be at 8 not 2.                                                                                                                                               //
                            if (maximumPosition.Count() > IncreasedVariable + 1 && lastReachedStandardPosition != null && maximumPosition[IncreasedVariable + 1] < lastReachedStandardPosition[IncreasedVariable + 1]) maximumPosition[IncreasedVariable + 1] = lastReachedStandardPosition[IncreasedVariable + 1];
                            //We had A4B7C3 we moved from here to A4B8C1 because we only have 3 C this correctly updates maximum for Bs to 7
                            //but also needs to update maximum for Cs to 3 otherwise it will stay at 2. 
                            //The second part of the if is for the same reason as the if above. 
                            lastReachedStandardPosition = newPosition;
                        }
                    }
                }
                subtasks = GetSubtasks(newPosition, TaskTypeArray, planSize); //This checks constraints. Like is one instance of a task before another like its supposed to be?
                newPosition = null;
            }
            Term term = new Term(mainTaskType.Name, new Constant[0]);
            Task t = new Task(term, MainTaskReferences.Count, MainTaskType);
            RuleInstance ruleInstance = new RuleInstance(t, subtasks.ToList(), this, null, null);
            if (ruleInstance.IsValid()) return ruleInstance;

            return null;
        }


        private bool CheckConditions(List<Task> subtasks, int planSize)
        {
            //We do this based on heuristics. 
            for (int i = 0; i < subtasks.Count(); i++)
            {
                int tPosition = myHeuristic.Mapping(i);
                Task t = subtasks[tPosition];
                if (numOfOrderedTasksAfterThisTask?[tPosition] > 0)
                {
                    if (!(Math.Floor(t.EndIndex) < planSize - minOrderedTaskPositionAfter[tPosition]))
                    {
                        return false; //assuming plan size of action number. So for plan from 0-7 plan size is 8.
                    }
                }
                if (numOfOrderedTasksBeforeThisTask?[tPosition] > 0)
                {
                    if (!(Math.Ceiling(t.StartIndex) >= minOrderedTaskPosition[tPosition])) return false; //>= because if normal task is on position 0 and so is empty task and normal task must be before empty task than its 0>=(1-1)
                }
                for (int j = i + 1; j < subtasks.Count(); j++)
                {
                    int t2Position = myHeuristic.Mapping(j);
                    Task t2 = subtasks[t2Position];
                    if (t.Equals(t2)) return false;
                    if (IsExplicitlyBefore(tPosition, t2Position))
                    {
                        if (Globals.TOIndicator)
                        {
                            if (Math.Floor(t.EndIndex) + 1 != Math.Ceiling(t2.StartIndex)) return false;
                        }
                        else if (!(Math.Floor(t.EndIndex) < Math.Ceiling(t2.StartIndex))) return false;
                    }
                    else if (IsExplicitlyBefore(t2Position, tPosition))
                    {
                        if (Globals.TOIndicator)
                        {
                            if (Math.Ceiling(t.StartIndex) != Math.Floor(t2.EndIndex) + 1) return false;
                        }
                        else if (!(Math.Ceiling(t.StartIndex) > Math.Floor(t2.EndIndex))) return false;
                    }
                    else if (Globals.CheckTransitiveConditions)
                    {
                        if (IsTransitivelyBefore(tPosition, t2Position))
                        {
                            if (!(Math.Floor(t.EndIndex) < Math.Ceiling(t2.StartIndex))) return false; //Our task must be before this subtask so I shall only look at possible instances that end before the other starts. 
                        }
                        else if (IsTransitivelyBefore(t2Position, tPosition))
                        {
                            if (!(Math.Ceiling(t.StartIndex) > Math.Floor(t2.EndIndex))) return false; //My task must start after task l.
                        }
                    }
                    if (!Differs(t.GetActionVector(), t2.GetActionVector())) return false;
                }
            }
            return true;

        }

        /// <summary>
        /// This eventually needs to do all checks like before positions and so on. If checks dont fit return null;
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="taskTypeArray"></param>
        /// <returns></returns>
        private List<Task> GetSubtasks(int[] newPosition, TaskType[] taskTypeArray, int planSize)
        {

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < taskTypeArray.Count(); i++)
            {
                tasks.Add(taskTypeArray[i].Instances.ElementAt(newPosition[i]));
            }
            if (CheckConditions(tasks, planSize)) return tasks;
            return null;

        }

        private int[] GetNextComboNoFreeze(int[] lastReachedPosition, ref int IncreasedPosition)
        {
            return GetNextWithFreezeFunction(lastReachedPosition, -1, ref IncreasedPosition);
        }

        /// <summary>
        /// gets the next position assuming some value is frozen. For example M->A, B,C we have 10A, 5B and 3C currently B5 is frozen.
        /// So position 1 is frozen and the last reached position must have 5 on position 1 already.
        /// Note lastreached position is position after already freezing B5 not the standard position here we were before freezing happened.,
        /// If positionofFrozenValue=-1 then this acts like normal standard getnextcombo. 
        /// </summary>
        /// <param name="lastReachedPositionWithFrozenValue"></param>
        /// <param name="positionofFrozenValue"></param>
        /// <returns></returns>
        private int[] GetNextWithFreezeFunction(int[] lastReachedPositionWithFrozenValue, int positionofFrozenValue, ref int IncreasedPosition)
        {
            int[] newPosition;
            if (lastReachedPositionWithFrozenValue == null)
            {
                newPosition = new int[TaskTypeArray.Count()];
                IncreasedPosition = 0;
                //We dont need to iterate through this because automatically arrays of int are populated with 0.
                return newPosition;
            }
            newPosition = (int[])lastReachedPositionWithFrozenValue.Clone();
            for (int i = lastReachedPositionWithFrozenValue.Count() - 1; i >= 0; i--)
            {
                if (i != positionofFrozenValue)
                {
                    if (TaskTypeArray[i].Instances.Count() > lastReachedPositionWithFrozenValue[i] + 1)
                    {
                        newPosition[i] = newPosition[i] + 1;
                        IncreasedPosition = i;
                        return newPosition;
                    }
                    else
                    {
                        newPosition[i] = 0; //We already reached maximum at this subtask so we set it to first subtask again and jump to previous subtask.
                    }
                }
            }
            //If we are here then no subtask was increased so we reached maximum. 
            return null;
        }

        /// <summary>
        /// Return the next combination with frozen position is its not bigger than the last standard position. 
        /// </summary>
        /// <param name="lastReachedStandardPosition"></param>
        /// <param name="lastReachedPositionWithFrozenValue"></param>
        /// <param name="FrozenPosition"></param>
        /// <param name="Frozenvalue"></param>
        /// <returns></returns>
        private int[] GetNextComboFreeze(int[] lastReachedStandardPosition, int[] lastReachedPositionWithFrozenValue, int FrozenPosition, int Frozenvalue)
        {
            int IncreasedVar = 0;
            int[] newPosition = new int[lastReachedStandardPosition.Count()];
            if (lastReachedPositionWithFrozenValue == null)
            {
                for (int i = 0; i < lastReachedStandardPosition.Count(); i++)
                {
                    if (i == FrozenPosition) newPosition[i] = Frozenvalue;
                    else newPosition[i] = 0;
                }

            }
            else newPosition = GetNextWithFreezeFunction(lastReachedPositionWithFrozenValue, FrozenPosition, ref IncreasedVar);
            if (IsBigger(newPosition, lastReachedStandardPosition))
            {
                return null;
            }
            return newPosition;
        }

        /// <summary>
        /// Returns whether the first position is bigger/later than the second position. 
        /// All positions are traversed in this order A1..ALast_index and first we iterate over last variable and then first so for example:
        /// A5B2C7>A5B2C6
        /// A2B7C1>A1B8C3.
        /// If positions are exactly the same we return false./// 
        /// </summary>
        /// <param name="newPosition"></param>
        /// <param name="lastReachedStandardPosition"></param>
        /// <returns></returns>
        private bool IsBigger(int[] newPosition, int[] lastReachedStandardPosition)
        {
            if (lastReachedStandardPosition.Count() != newPosition.Count())
            {
                Console.WriteLine("Warning subtasks positions dont have same length.");
            }
            for (int i = 0; i < lastReachedStandardPosition.Count(); i++)
            {
                if (newPosition[i] > lastReachedStandardPosition[i]) return true;
                if (newPosition[i] < lastReachedStandardPosition[i]) return false;
            }
            return false;
        }


        //This finds all applicable tasks from list.
        //Tuple item1 is main task, list of subtasks and allvars.       
        //Index refers to position in heuristic. Mapped index refers to actual position in task type. Newindex also refers to actual position in tasktype. 
        private List<Tuple<Task, Task[], List<Constant>>> GetNextSuitableTask(TaskType t, int index, int newindex, List<Constant> partialAllVars, Task[] subtasks, int planSize)
        {
            bool doingNewtask = false;
            int mappedIndex;
            var unusedInstances = t.Instances.Except(subtasks).Distinct();
            if (index == -1) //Tasktype must be given as the new one. 
            {
                doingNewtask = true;
                //these unusedinstances originally had to list at the end does not seem to make much difference in speed whether its there or not.  
                unusedInstances = unusedInstances.Where(x => x.CreationNumber >= LastCreationNumber);

                index = newindex; //Temporarily we change the index so we don't have to change everything else and then after we switch it back to -1.
                mappedIndex = newindex; //We still want to do the new subtask first. 
            }
            else
            {
                mappedIndex = myHeuristic.Mapping(index);
            }
            if (!doingNewtask && mappedIndex < newindex)    //This ensures that if I have rule with 2 newsubtasks I wont get it twice. 
                                                            //Anything after newindex can be both new and old. 
                                                            //This line wont let me use last creation attempts but I have to use real number otherwise lets say I have two new tasks A, B but I tried A already. Well now I try with B only but this only allows A to use new instances after B and I still need that one instance. . 
            {
                unusedInstances = unusedInstances.Where(x => x.CreationNumber < LastCreationNumber);
            }
            //If index is new index we keep it the way it is because we want to go with that first. But after we go from left to right. So we start with index 0. But index 0 means I want the first subtask according to my heuristic, which might be on position 5. So I keep info on position 5.

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
                            if (Globals.TOIndicator) unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) + 1 == Math.Ceiling(l.StartIndex));
                            else unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < Math.Ceiling(l.StartIndex));
                        }
                        else if (IsExplicitlyBefore(oldIndex, mappedIndex))
                        {
                            if (Globals.TOIndicator) unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) == Math.Floor(l.EndIndex) + 1);
                            else unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) > Math.Floor(l.EndIndex));
                        }
                        else if (Globals.CheckTransitiveConditions)
                        {
                            if (IsTransitivelyBefore(mappedIndex, oldIndex))
                            {
                                unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < Math.Ceiling(l.StartIndex)); //Our task must be before this subtask so I shall only look at possible instances that end before the other starts. 
                            }
                            else if (IsTransitivelyBefore(oldIndex, mappedIndex))
                            {
                                unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) > Math.Floor(l.EndIndex)); //My task must start after task l.
                            }
                        }
                        unusedInstances = unusedInstances.Where(x => Differs(x.GetActionVector(), l.GetActionVector())); //NO problem on empty task because they return null.
                                                                                                                         //This is not the same as the sum check later. 
                    }
                }
            }

            if (numOfOrderedTasksAfterThisTask?[mappedIndex] > 0)
            {
                unusedInstances = unusedInstances.Where(x => Math.Floor(x.EndIndex) < planSize - minOrderedTaskPositionAfter[mappedIndex]); //assuming plan size of action number. So for plan from 0-7 plan size is 8.
            }
            if (numOfOrderedTasksBeforeThisTask?[mappedIndex] > 0)
            {
                unusedInstances = unusedInstances.Where(x => Math.Ceiling(x.StartIndex) >= minOrderedTaskPosition[mappedIndex]); //>= because if normal task is on position 0 and so is empty task and normal task must be before empty task than its 0>=(1-1)
            } //This shuld be okay even with empty tasks as they have minlegtharray of task 0

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
                        //We need to skip the newindex because we already did it. So lets assume the new index has index 2 and its mappedIndex is 1. So we must make sure that we skip index=1
                        if (index + 1 == myHeuristic.ReverseMapping(newindex) && myHeuristic.ReverseMapping(newindex) < TaskTypeArray.Length - 1)
                        {//This makes us skip the new task. because we already picked the task for the new task. 
                            newMyResult = GetNextSuitableTask(TaskTypeArray[myHeuristic.Mapping(index + 2)], index + 2, newindex, newAllVars, newSubTasks, planSize);
                        }
                        else
                        {
                            newMyResult = GetNextSuitableTask(TaskTypeArray[myHeuristic.Mapping(index + 1)], index + 1, newindex, newAllVars, newSubTasks, planSize);
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
        /// So if A < B and B<c and we ask about A and C this retruns true. As opposed to normal ISBefore which would return false.  
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
                    //allvars is empty. Does what I want to put here fit my desired type if so I just add it if not I will return null.                     
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

        public bool NullAcceptingSequenceEqual<T>(List<T> list1, List<T> list2)
        {
            if (list1 != null && list2 != null)
                return list1.SequenceEqual(list2);

            if (list1 == null && list2 == null) return true;
            else return false;
        }

        public bool NullAcceptingSequenceEqual<T>(T[] array1, T[] array2)
        {
            if (array1 != null && array2 != null)
                return array1.SequenceEqual(array2);

            if (array1 == null && array2 == null) return true;
            else return false;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Rule r = obj as Rule;
            if (r.GetHashCode() != GetHashCode()) return false;
            if (!MainTaskType.Equals(r.MainTaskType)) return false;
            return (NullAcceptingSequenceEqual(TaskTypeArray, r.TaskTypeArray) && NullAcceptingSequenceEqual(AllVars, r.AllVars) &&
                NullAcceptingSequenceEqual(posPreConditions, r.posPreConditions) &&
                NullAcceptingSequenceEqual(negPreConditions, r.negPreConditions) &&
                NullAcceptingSequenceEqual(posBetweenConditions, r.posBetweenConditions) &&
                NullAcceptingSequenceEqual(negBetweenConditions, r.negBetweenConditions) &&
                NullAcceptingSequenceEqual(posPostConditions, r.posPostConditions) &&
                NullAcceptingSequenceEqual(negPostConditions, r.negPostConditions));
        }

        public override int GetHashCode()
        {
            int hash = MainTaskType.GetHashCode();
            hash = hash * 11 + (int)(posPreConditions.Count) + (int)(negPostConditions.Count * 10) + (int)(posBetweenConditions.Count) + (int)(negBetweenConditions.Count * 10);
            if (TaskTypeArray != null)
            {
                foreach (TaskType t in TaskTypeArray)
                {
                    hash = hash + t.GetHashCode();
                }
            }
            hash = hash * 3 + AllVars.Count();
            return hash;
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
