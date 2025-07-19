using PlanValidation1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanValidationExe
{
    class HeuristicStructure
    {        
        private SearchHeuristic h;
        private HashSet<Rule>[] array;
        //We use heuritics where the heuristic with lowest number is the one we want. In this int we remember what is the current Minimum so we can just jump into the position. 
        private int currentMin;
        /// <summary>
        /// Number of rules inside this structure. 
        /// </summary>
        int count;

        public HeuristicStructure(SearchHeuristic h, int count)
        {
            this.h = h;            
            array = new HashSet<Rule>[count];
            count = 0;
        }

        /// <summary>
        /// Pops a rule based on the heuristics. 
        /// This functions as a deletemin at the moment as we used minimal heuristics. 
        /// </summary>
        /// <returns></returns>
        public Rule Pop()
        {
            if (array[currentMin]!=null && array[currentMin].Count>0)
            {
                Rule r=array[currentMin].First();
                array[currentMin].Remove(r);
                count--;
                return r;
            } else if (currentMin<array.Count()-1)
            {
                currentMin++;
                return Pop();
            } else
            {
                //There is no rule left.
                if (count != 0) Console.WriteLine(" Warning: The heuristic structure does not work properly. According to count there should be a rule left but according to Poping system there is not. ");
                return null;
            }
        }

        public void AddRange(HashSet<Rule> rules)
        {            
            foreach (Rule r in rules)
            {
                Add(r);
            }
        }

        public void Add(Rule r)
        {
            int i = h.GetValue(r);
            if (array.Count() <= i) array=DoubleArraySize(array);
            if (array[i]==null)
            {
                array[i] = new HashSet<Rule>();
            }
            if (!array[i].Contains(r)) { array[i].Add(r);
                                        count++;
            }
            if (i < currentMin) currentMin = i;            
        }

        private HashSet<Rule>[] DoubleArraySize(HashSet<Rule>[] array)
        {
            HashSet<Rule>[] array2 = new HashSet<Rule>[array.Count() * 2];
            for (int i=0; i<array.Count(); i++)
            {
                array2[i] = array[i];
            }               
            return array2;
        }

        /// <summary>
        /// Allows direct removal of a rule from the list. Can be used for exmaple for empty rules. 
        /// </summary>
        /// <param name="rules"></param>
        public void RemoveRange(List<Rule> rules)
        {
            foreach(Rule r in rules)
            {
                Remove(r);
            }
        }

        public void Remove(Rule r)
        {
            int value = h.GetValue(r);
            if (array[value] != null && array[value].Count > 0 && array[value].Contains(r))
            {
                array[h.GetValue(r)].Remove(r);
                count--;
            }
        }

        internal bool IsEmpty()
        {
            if (count <= 0) return true;
            return false;
        }
    }
}
