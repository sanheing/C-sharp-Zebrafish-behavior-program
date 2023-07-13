# C-sharp-Zebrafish-behavior-program
Visual and optogenetic stimuli delivering to larval zebrafish behaving in a VR environment.
While presenting the hologram on the optowindow form, sometimes the program will crash.

This matter is caused by multiple optoThreads being opened at the same time. And when they access the pattern variance will cause the error because of cross-Thread access of one resource.
