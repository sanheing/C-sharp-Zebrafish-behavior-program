# C-sharp-Zebrafish-behavior-program
Visual and optogenetic stimuli delivering to larval zebrafish behaving in a VR environment.
While presenting the hologram on the optowindow form, sometimes the program will crash.

## 20230713 optowindow crashing solved
This matter is caused by multiple optoThreads being opened at the same time. And when they access the pattern variance will cause the error because of cross-Thread access of one resource.

At the same time, a new module was added. This module allows control of the time length of the interval period in closed-loop obstacle interaction.
