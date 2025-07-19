using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Represent the actual task. 
    /// </summary>
    class Task
    {
        public Term TaskInstance;
        private bool[] ActionVector;
        public TaskType TaskType { get; }
        public double StartIndex; //For tasks that have subtasks this is just first one in ActionVector. For empty subtasks this is the slot, on which it's true-0,5. 
        public double EndIndex;
        public int Iteration;
        public bool isSubtaskSomewhere; //Only used for analysis not for normal program. If a task is a subtask to some other task then this is true. 

        /// <summary>
        /// This is the index of the last state before this task that satisfies the before condition. This is only relevant for sometime before conditions.
        /// </summary>
        public int BufferZoneIndex;

        public bool[] GetActionVector()
        {
            return ActionVector;
        }

        public void SetActionVector(bool[] actionvector)
        {
            ActionVector = actionvector;
            UpdateStartIndex();
            UpdateEndIndex();
        }

        private void UpdateEndIndex()
        {
            for (int i = ActionVector.Length - 1; i >= 0; i--)
            {
                if (ActionVector[i])
                {
                    EndIndex = i;
                    break;
                }
            }
        }
        private void UpdateStartIndex()
        {
            for (int i = 0; i < ActionVector.Length; i++)
            {
                if (ActionVector[i])
                {
                    StartIndex = i;
                    break;
                }
            }
        }

        public Task(Task t)
        {
            this.TaskInstance = t.TaskInstance;
            this.ActionVector = t.ActionVector;
            this.TaskType = t.TaskType;
        }

        public Task(Term taskInstance, int size, TaskType type)
        {
            TaskInstance = taskInstance;
            ActionVector = new bool[size];
            TaskType = type;
        }

        /// <summary>
        /// This is used for creating empty tasks. 
        /// </summary>
        /// <param name="taskInstance"></param>
        /// <param name="size"></param>
        /// <param name="type"></param>
        /// <param name="StartIndex"></param>
        /// <param name="EndIndex"></param>
        public Task(Term taskInstance, int size, TaskType type, double StartIndex, double EndIndex)
        {
            TaskInstance = taskInstance;
            ActionVector = new bool[size];
            TaskType = type;
            this.StartIndex = StartIndex;
            this.EndIndex = EndIndex;
            if (EndIndex < StartIndex)
            {
                Console.WriteLine("Error: Endindex is smaller than startindex");
            }
        }

        /// <summary>
        /// Compares whether two tasks are equal. they are equal if they have the same name and variable  and stat and end index, but they may have different bufferzone values. 
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool isEqualTo(Task t)
        {
            return (t.TaskInstance.Equals(this.TaskInstance) && GetActionVector().SequenceEqual(t.GetActionVector()) && GetStartIndex() == t.GetStartIndex() && GetEndIndex() == t.GetEndIndex());
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Task t = obj as Task;
            return (TaskInstance.Equals(t.TaskInstance) && GetActionVector().SequenceEqual(t.GetActionVector()) && GetStartIndex() == t.GetStartIndex() && GetEndIndex() == t.GetEndIndex());
        }

        public override int GetHashCode()
        {
            int hash = TaskInstance.GetHashCode();
            //Start index times 10 so that it is a whole number. (0,5->5). 
            //negative hashcodes are okay. 
            hash = hash * 11 + (int)(GetStartIndex() * 10) + (int)(GetEndIndex() * 10);
            int i = 1;
            foreach (bool a in ActionVector)
            {
                if (a) hash = hash + 7 * i; //for action vector 0 1 0 1 we add 7*2 and 7*4.
                i++;
            }
            return hash;
        }


        public Task(Term taskInstance, bool[] vector, TaskType type)
        {
            TaskInstance = taskInstance;
            ActionVector = vector;
            TaskType = type;
            UpdateEndIndex();
            UpdateStartIndex();
        }

        public Task(Term taskInstance, bool[] vector, TaskType type, double StartIndex, double EndIndex)
        {
            TaskInstance = taskInstance;
            ActionVector = vector;
            TaskType = type;
            this.StartIndex = StartIndex;
            this.EndIndex = EndIndex;
        }

        public Task(Term taskInstance, bool[] vector, TaskType type, double StartIndex, double EndIndex, int iteration, int bufferZoneNumber)
           : this(taskInstance, vector, type, StartIndex, EndIndex)
        {
            Iteration = iteration;
            BufferZoneIndex = bufferZoneNumber;
        }

        public Task(Action a, int size, TaskType type)
        {
            TaskInstance = a.ActionInstance;
            ActionVector = new bool[size];
            TaskType = type;
        }

        /// <summary>
        /// Adds this task to instances of this task type and returns the task type
        /// </summary>
        /// <returns></returns>
        internal void AddToTaskType()
        {
            this.TaskType.AddInstance(this);
        }

        public double GetEndIndex()
        {
            return EndIndex;
        }

        public double GetStartIndex()
        {
            return StartIndex;
        }

        public override string ToString()
        {
            string text = string.Join(",", TaskInstance.Variables.Select(x => x.Name).ToList());
            string vector = string.Join(",", ActionVector);
            String s = "Task:" + TaskInstance.Name + " Variables " + text + " TaskType " + TaskType.Name + " ActionVector " + vector + " StartIndex " + StartIndex + " EndIndex " + EndIndex;
            return s;
        }
    }
}
