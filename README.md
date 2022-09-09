# GreatEscape

2022-09-09 12.22
second change to the master
After the date the first change was made

A trivial Spectrum emulator made for reverse engineering purposes. The main idea is to have a full state log after each instrucion, so you can do dynamic program analysis.
So far I have a proof of concept code for sending line comments to Ghidra that are a result of all the instructions executed in one run, but not in another.


Currently working:
- basic emulator
- sending line comments to Ghidra
- naive full memory and registers loging after each instruction
- basic skoolkit sna2skool calling

Currently not working:
-not all instructions are implemented, I just run the game and add instructions as they are needed.
-also, not all flags are currently updated.


