c $8228 Wait routine, BC Is parameter
c $822e Clear attributes, clear screen
c $8251 
c $a550 Could be start Of the fireworks 
c $a5cf inc_a5ce_and_delay
c $a807 0x6 If zero means we have no fragments
c $a80b If zero, jump To inca5ce_and_delay, probably adjusting the time To be similar To fragment anim.

c $a877 
c $aa31 
c $aa95
c $aabc Read ROM into DE, self modify instruction at aabd 
c $ab0d Go Up one screen line
c $ab1e Go Down one screen line
c $ab9a Cudna operacija sa HL nakon toga Or-a C na (HL)
c $abad Delete from (HL)

   
