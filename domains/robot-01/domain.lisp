(define (domain rc)
  (:predicates (prloc_c_)
               (parmempty__)
               (pclosed_d02_)
               (pin_o1_r2_)
               (mclosed_d02_)
               (prloc_r1_)
               (prloc_r2_)
               (pholding_o1_)
               (pin_o1_r1_)
  )

  (:task x__top__ :parameters ())
  (:task achievemgoals__ :parameters ())
  (:task move_abstract__ :parameters ())
  (:task open_abstract__ :parameters ())
  (:task achievemgoalsmpickup_splitted_1_o1_ :parameters ())
  (:task pickup_abstract_o1_ :parameters ())
  (:task release__ :parameters ())
  (:task releasemputdown_abstract_splitted_2_o1_ :parameters ())
  (:task putdown_abstract__ :parameters ())
  (:task x___intermediate_task_method_8_0__ :parameters ())
  (:task x___intermediate_task_method_11_0__ :parameters ())
  (:task x___intermediate_task_method_11_1__ :parameters ())

  (:method x__top_method_0
     :parameters ()
     :task (x__top__)
     :ordered-subtasks (and
        (achievemgoals__)
     )
  )
  (:method finished_1
     :parameters ()
     :task (achievemgoals__)
     :ordered-subtasks (and
     )
  )
  (:method achievemgoalsmmove_2
     :parameters ()
     :task (achievemgoals__)
     :ordered-subtasks (and
        (move_abstract__)
        (achievemgoals__)
     )
  )
  (:method newMethod24_3
     :parameters ()
     :task (move_abstract__)
     :ordered-subtasks (and
        (move_c_r1_d01_)
     )
  )
  (:method newMethod24_4
     :parameters ()
     :task (move_abstract__)
     :ordered-subtasks (and
        (move_c_r2_d02_)
     )
  )
  (:method newMethod24_5
     :parameters ()
     :task (move_abstract__)
     :ordered-subtasks (and
        (move_r2_c_d02_)
     )
  )
  (:method achievemgoalsmopen_6
     :parameters ()
     :task (achievemgoals__)
     :ordered-subtasks (and
        (open_abstract__)
        (achievemgoals__)
     )
  )
  (:method newMethod25_7
     :parameters ()
     :task (open_abstract__)
     :ordered-subtasks (and
        (open_c_r2_d02_)
     )
  )
  (:method x_splitting_method_achievemgoalsmpickup_splitted_1_8
     :parameters ()
     :task (achievemgoalsmpickup_splitted_1_o1_)
     :precondition (and 
         (pin_o1_r2_)
         (prloc_r2_)
     )
     :ordered-subtasks (and
     )
  )
  (:method newMethod22_9
     :parameters ()
     :task (pickup_abstract_o1_)
     :ordered-subtasks (and
        (pickup_o1_r2_)
     )
  )
  (:method x_splitting_method_releasemputdown_abstract_splitted_2_10
     :parameters ()
     :task (releasemputdown_abstract_splitted_2_o1_)
     :precondition (and 
         (prloc_r1_)
     )
     :ordered-subtasks (and
     )
  )
  (:method newMethod23_11
     :parameters ()
     :task (putdown_abstract__)
     :ordered-subtasks (and
        (putdown_o1_r1_)
     )
  )
  (:method releasemmove_12
     :parameters ()
     :task (release__)
     :ordered-subtasks (and
        (move_abstract__)
        (release__)
     )
  )
  (:method releasemopen_13
     :parameters ()
     :task (release__)
     :ordered-subtasks (and
        (open_abstract__)
        (release__)
     )
  )
  (:method achievemgoalsmpickup_14
     :parameters ()
     :task (achievemgoals__)
     :ordered-subtasks (and
        (achievemgoalsmpickup_splitted_1_o1_)
        (x___intermediate_task_method_8_0__)
     )
  )
  (:method achievemgoalsmpickup_1_15
     :parameters ()
     :task (x___intermediate_task_method_8_0__)
     :ordered-subtasks (and
        (pickup_abstract_o1_)
        (release__)
     )
  )
  (:method releasemputdown_abstract_16
     :parameters ()
     :task (release__)
     :precondition (and 
         (pholding_o1_)
     )
     :ordered-subtasks (and
        (x___intermediate_task_method_11_0__)
     )
  )
  (:method releasemputdown_abstract_1_17
     :parameters ()
     :task (x___intermediate_task_method_11_0__)
     :ordered-subtasks (and
        (releasemputdown_abstract_splitted_2_o1_)
        (x___intermediate_task_method_11_1__)
     )
  )
  (:method releasemputdown_abstract_2_18
     :parameters ()
     :task (x___intermediate_task_method_11_1__)
     :ordered-subtasks (and
        (putdown_abstract__)
        (achievemgoals__)
     )
  )

  (:action move_c_r1_d01_
     :parameters ()
     :precondition (and 
         (prloc_c_)
     )
     :effect (and 
         (prloc_r1_)
         (not(prloc_c_))
     )
  )

  (:action move_c_r2_d02_
     :parameters ()
     :precondition (and 
         (mclosed_d02_)
         (prloc_c_)
     )
     :effect (and 
         (prloc_r2_)
         (not(prloc_c_))
     )
  )

  (:action move_r2_c_d02_
     :parameters ()
     :precondition (and 
         (mclosed_d02_)
         (prloc_r2_)
     )
     :effect (and 
         (prloc_c_)
         (not(prloc_r2_))
     )
  )

  (:action open_c_r2_d02_
     :parameters ()
     :precondition (and 
         (pclosed_d02_)
         (prloc_c_)
     )
     :effect (and 
         (mclosed_d02_)
         (not(pclosed_d02_))
     )
  )

  (:action pickup_o1_r2_
     :parameters ()
     :precondition (and 
         (pin_o1_r2_)
         (parmempty__)
         (prloc_r2_)
     )
     :effect (and 
         (pholding_o1_)
         (not(parmempty__))
         (not(pin_o1_r2_))
     )
  )

  (:action putdown_o1_r1_
     :parameters ()
     :precondition (and 
         (prloc_r1_)
         (pholding_o1_)
     )
     :effect (and 
         (pin_o1_r1_)
         (parmempty__)
         (not(pholding_o1_))
     )
  )
)
