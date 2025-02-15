=== intro ===
Hello there! #speaker:Dr. Green #portrait:dr_green_neutral #layout:left #voice:dr_green_greeting #emoji:smile
-> main

=== main ===
How are you feeling today? #voice:dr_green_question
    +[Happy]
        That makes me feel <color=#f8ff30>happy</color> as well! #portrait:dr_green_happy #voice:dr_green_happy_response #emoji:grin
    +[Sad]
        Oh, well that makes me <color=#5b81ff>sad</color> too. #portrait:dr_green_sad #voice:dr_green_sad_response #emoji:cry

- Don't trust him, he's <color=#ff1e35>not</color> a real doctor! #speaker:Ms. Yellow #portrait:ms_blue_neutral #layout:right #voice:ms_yellow_warning #emoji:angry

Well, do you have any more questions? #speaker:Dr. Green #portrait:dr_green_neutral #layout:left #voice:dr_green_followup #emoji:neutral
    +[Yes]
        -> main
    +[No]
        Goodbye then! #voice:dr_green_goodbye #emoji:wink
        -> END
