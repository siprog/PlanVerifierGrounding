using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanValidation1
{
    /// <summary>
    /// Simply a name with variables used to describe almost everything from constant to tasks. 
    /// </summary>
    class Term
    {
        public String Name { get; }
        public Constant[] Variables { get; } //maybe readonly cannot be changed because of hashcode

        public Term(String name, Constant[] variables)
        {
            Name = name;
            Variables = variables;
        }

        public Term(String name, int i)
        {
            Name = name;
            Variables = new Constant[i];
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            Term cond2 = obj as Term;
            if (Name.Equals(cond2.Name))
            {
                if (Variables.Length != cond2.Variables.Length) return false;
                for (int i = 0; i < cond2.Variables.Length; i++)
                {
                    if (Variables[i] != null && cond2.Variables[i] != null)
                    {
                        if (!Variables[i].Equals(cond2.Variables[i])) return false;
                    }
                }
                return true;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            int hash = Name.GetHashCode();
            foreach (Constant s in Variables)
            {
                hash = hash * 7 + s.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            string vars = string.Join(",", Variables.Select(x => x.Name));
            String s = "Term: " + this.Name + "variables " + vars;
            return s;
        }

        //Returns true if two terms are equal in every variable except the one where there is null. 
        //Allows different lengths. 
        public bool EqualOrNull(Term t)
        {
            if (t.Name != this.Name) return false;
            int maxCount = Math.Max(this.Variables.Length, t.Variables.Length);
            for (int i = 0; i < maxCount; i++)
            {
                if (i >= this.Variables.Length || i >= t.Variables.Length) return true;
                if (this.Variables[i] != null && t.Variables[i] != null)
                {
                    if (this.Variables[i] != t.Variables[i]) return false;
                }
            }
            return true;
        }
    }
}
