# PlanVerifierGrounding
This is implementation of plan verifier described in the article:  On the impact of grounding on htn plan verification via parsing by Simona Ondrčková, Roman Barták, Pascal Bercher, and Gregor Behnke ICTAI 2023
This implementation of the described verifier is created by Simona Ondrčková.

We provide three versions: the BFS with implementation improvements (main branch), DFS One by One and DFSAtOnce (in their respective branches). For more information on how they differ see the article. 

We also provide an example of a 2-regulated and grounded domain. 

To run the program build it and then you can use the arguments: 

ni - disallow interleaving of tasks (Interleaving = false)

g - require the specified root task to decompose into the plan (KnownRootTask = true)

gs - enforce goal-state validation (CheckGoalState = true)

ic - ignore case for all names and checks (IgnoreCase = true)

h <0|1|2|3> choose subtask ordering heuristic: 0 = MostParameters 1 = LeastParameters 2 = Original 3 = Instances (for example h0)


The program will automatically look for these files in the same folder as the exe file: 

domain: domain.lisp

plan: plan.txt

problem: problem

You can alternatively specify the path to the domain,problem and plan fields in arguments. If so then these have to be the first 3 arguments in this order: domain,problem,plan. The plan file must have the .txt file extension. 
