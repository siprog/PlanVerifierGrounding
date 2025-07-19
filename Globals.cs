using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanValidationExe
{
    /// <summary>
    /// List of global constants and variables. 
    /// </summary>
    public static class Globals
    {
        public enum Heuristics { MostParameters = 0, LeastParameters = 1,Original=2, Instances=3}
        /// <summary>
        /// Number used to reference constants for action. 
        /// </summary>
        public const int ConstReferenceNumber = -10;
        public const String NotInterleavingS = "ni";
        public const String KnownGoalTaskS = "g";
        public const String KnownGoalStateS = "gs";
        public const String IgnoreCaseArgS = "ic";
        public const String SometimeBeforeS = "sb";


        /// <summary>
        /// The program changes this automatically. The default value must  be true and if any task does not have full ordering it will change this to false. 
        /// </summary>
        public static bool TOIndicator = true;
        /// <summary>
        /// Is true if tasks are allowed to interleave. 
        /// </summary>
        public static bool Interleaving = true;
        /// <summary>
        /// The plan is only valid if given root task decomposes into the plan not just any task. 
        /// Automatically set on false, argument sets it to true. 
        /// </summary>
        public static bool KnownRootTask = true;
        /// <summary>
        /// If this is set to true we must check the goal state in order for the plan to be valid. 
        /// Automatically set on false, arguments set it to true. 
        /// </summary>
        public static bool CheckGoalState = true;

        /// <summary>
        /// If set to true when we create new rules we also check whether subtasks are transitively in the right order not just explicitly. 
        /// Automatically false. 
        /// </summary>
        public static bool CheckTransitiveConditions = false;

        /// <summary>
        /// Determines what heuristic is used when creating a new rule from subtasks. 
        /// </summary>
        public static Heuristics Heuristic = Heuristics.Instances;

        /// <summary>
        /// Ignores casing for anything (methods, actions...)
        /// Automatically set on false, argument sets it to true. 
        /// </summary>
        public static bool IgnoreCase = false;

        /// <summary>
        /// As default we use immediatelly before conditions which means that for method B if it has a preconddition it must be true in the state before the first action of B. so if it start on action 5 then state 5. 
        /// Sometimes before conditions means that it must be true sometimes before but after any preceesing subtask fro some method that decomposes into B so for method M->A, B it must be true sometimes before end of A and start of B.
        /// This is only related to method preconditions actions always use immediatelly before conditions. 
        /// </summary>
        public static bool SometimeBeforeCond = false;

        /// <summary>
        /// Returns null or value of a key in doctionary. 
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static V NullLookUp<K, V>(Dictionary<K, V> dict, K key) where V : class
        {
            if (dict.ContainsKey(key)) return dict[key];
            else return null;
        }
    }
}
