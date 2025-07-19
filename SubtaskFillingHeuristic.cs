using PlanValidation1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanValidationExe
{
    /// <summary>
    /// This is a heuristic that will provide mapping to subtasks and determine the order with which we will take subtasks and use them to fill parameters in a rule. 
    /// </summary>
    abstract class SubtaskFillingHeuristic
    {
        /// <summary>
        /// This retruns the mapping of int i. 
        /// Variable i the position in the heuristic. So i=1 will return the first subtask's position (regular position based on the domain) according to the heristic. 
        /// So for heuristic for the most parameters i=1 will return the position of the subtask with most parameters. 
        /// The position here is just position in the description of the rule this is not based on ordering on anzthing like that. 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public abstract int Mapping(int i);
        /// <summary>
        /// This is  a reverse function to mapping. If subtask on position 5 gives the first subtask according to heuristic that this will return value 1 to ReverseMapping(5).
        /// This is essentially saying how good is a subtask according to the heuristic. 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public abstract int ReverseMapping(int i);


    }

    abstract class ParameterHeuristic : SubtaskFillingHeuristic
    {
        /// <summary>
        /// This is a list ordered based on the heuristic. 
        /// SubtasksOrderReference[0] return the position of the first subatsk according to the heuristic. 
        /// </summary>
        public abstract List<int> SubtasksOrderReference
        {
            get;
        }

        /// <summary>
        /// Returns a list that keeps the information on how many parameters does each subtask have. 
        /// so the first value in array says how many parameters does the first subtask have. 
        /// </summary>
        /// <param name="TaskTypeArray"></param>
        /// <returns></returns>
        public int[] GetListWithParamNumbers(TaskType[] TaskTypeArray)
        {
            int[] numOfParameters = new int[TaskTypeArray.Count()];
            for (int i = 0; i < TaskTypeArray.Count(); i++)
            {
                numOfParameters[i] = TaskTypeArray[i].NumOfVariables;
            }
            return numOfParameters;
        }

        public int[] GetListWithParamNumbers(List<int>[] ArrayOfReferenceLists, List<String> AllVars)
        {
            int[] numOfParameters = new int[ArrayOfReferenceLists.Count()];
            for(int j=0;j<ArrayOfReferenceLists.Length;j++)
            {
                var intList = ArrayOfReferenceLists[j];
                int count = 0;
                for (int i=0;i<intList.Count;i++)
                {
                    if (AllVars[intList[i]].StartsWith("?")) count++;
                }
                numOfParameters[j] = count;
            }
            return numOfParameters;
        }

        public override int Mapping(int i)
        {
            return SubtasksOrderReference[i];
        }

        public override int ReverseMapping(int i)
        {
            return SubtasksOrderReference.IndexOf(i);
        }
    }

    /// <summary>
    /// Heuristic that orderes subtasks based on number of instances. First we do subtasks with smallest number of instances. 
    /// </summary>
    class InstancesHeuristic : SubtaskFillingHeuristic
    {
        public InstancesHeuristic(TaskType[] TaskTypeArray)
        {
            Recalculate(TaskTypeArray);
        }

        public void Recalculate(TaskType[] TaskTypeArray)
        {
            int[] numOfInstances = new int[TaskTypeArray.Count()];
            for (int j = 0; j < TaskTypeArray.Length; j++)
            {                
                numOfInstances[j] = TaskTypeArray[j].Instances.Distinct().Count();
            }
            var sorted = numOfInstances.Select((x, i) => new { Value = x, OriginalIndex = i }).OrderBy(x => x.Value).ToList();
            OrderedBasedonInstances = sorted.Select(x => x.OriginalIndex).ToList();
        }


        public List<int> OrderedBasedonInstances;

        public override int Mapping(int i)
        {
            return OrderedBasedonInstances[i];
        }

        public override int ReverseMapping(int i)
        {
            return OrderedBasedonInstances.IndexOf(i);
        }
    }


    /// <summary>
    /// This keeps the original order of subtasks. So if the subtasks is first in the list of subtasks for rule then it returns 0.
    /// </summary>
    class OriginalOrderHeuristic : SubtaskFillingHeuristic
    {
        public override int Mapping(int i)
        {
            return i;
        }

        public override int ReverseMapping(int i)
        {
            return i;
        }
    }

    class MostParametersHeuristic : ParameterHeuristic
    {
        public override List<int> SubtasksOrderReference
        {
            get
            {
                return subtasksOrderReference;
            }
        }
        private List<int> subtasksOrderReference;

        public MostParametersHeuristic(List<int>[] ArrayOfReferenceLists, List<String> AllVars)
        {
            subtasksOrderReference = new List<int>();
            //In order keeps the information of the number of variables for each subtask. 
            int[] numOfParameters = GetListWithParamNumbers(ArrayOfReferenceLists, AllVars);
            var sorted = numOfParameters.Select((x, i) => new { Value = x, OriginalIndex = i }).OrderByDescending(x => x.Value).ToList();
            subtasksOrderReference= sorted.Select(x => x.OriginalIndex).ToList();
        }        
    }

    class LeastParametersHeuristic : ParameterHeuristic
    {
        public override List<int> SubtasksOrderReference
        {
            get
            {
                return subtasksOrderReference;
            }
        }
        private List<int> subtasksOrderReference;

        public LeastParametersHeuristic(List<int>[] ArrayOfReferenceLists, List<String> AllVars)
        {
            subtasksOrderReference = new List<int>();
            //In order keeps the information of the number of variables for each subtask. 
            int[] numOfParameters = GetListWithParamNumbers(ArrayOfReferenceLists,AllVars);
            var sorted = numOfParameters.Select((x, i) => new { Value = x, OriginalIndex = i }).OrderBy(x => x.Value).ToList();
            subtasksOrderReference = sorted.Select(x => x.OriginalIndex).ToList();
        }
    }
}

