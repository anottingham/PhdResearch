
##PhD Research - GPF+ (Experimental)

###README

</br>

####Solution Overview

The Visual Studio 2012 Solution includes the following projects:

<ul>
<li><b>Client</b> - The .NET client application. Hosts Grammar and ViewTimeline.</li>

<li><b>Grammar</b> - The ANTLR / .NET DSL Compiler.</li>

<li><b>Server</b> - The CUDA / C++ Classification Server. </li>

<li><b>ViewTimeline</b> - The .NET post-processing functions.</li>
</ul>

These projects are organised into separate folders in the solution.

</br>

####Executables:

<ul>
<li><b>Client.exe</b> - Encapsulates .NET functions. Required for all GUI functions.</li>

<li><b>Server.exe</b> - Encapsulates C++ functions. Uses CUDA 6.5 Runtime API.</li>
</ul>

</br>

####Additional Files

All additional files are hosted on Dropbox. Click the file name to download.
<ul>
<li><b><a href=https://www.dropbox.com/s/xv1sfup1rz6pjxm/Source.rar?dl=0>Source.rar</a></b> (17 MB) contains the source files for the solution (as an alternative to Git). </li>
<li><b><a href=https://www.dropbox.com/s/i2dt5fc16ghijew/Binary.rar?dl=0>Binary.rar</a></b> (3 MB) contains compiled debug and release binaries, with all required libraries.</li>
<li><b><a href=https://www.dropbox.com/s/z1uzib8c8k4exzt/Programs.rar?dl=0>Programs.rar</a></b> (12 KB) contains all GPF+ programs used during testing (source and compiled)</li>
</ul>
Output Project files creating using Program Set A (Filter Only) and Captures A, B and C are also available. The contained projects can be viewed by selecting <b>Load Existing Project</b> from the main <b>Client</b> form (requires Server Connection).  
<ul>
<li><b><a href=https://www.dropbox.com/s/x5ughqbyqgf6xp5/Capture%20A.rar?dl=0>Capture A.rar</a></b> (19 MB)</li>
<li><b><a href=https://www.dropbox.com/s/ghaw72bn61z6pqd/Capture%20B.rar?dl=0>Capture B.rar</a></b> (276 MB)</li>
<li><b><a href=https://www.dropbox.com/s/1cwekopovdykwas/Capture%20C.rar?dl=0>Capture C.rar</a></b> (329 MB)</li>
</ul>
</br>

####Minimum Requirements

<ul>

<li> x64 CPU (x86 is not currently supported)</li>

<li> Compute Capability 3.5+ Nvidia GPU (currently compiles for 3.5 and 5.0 explicitly) </li> 

</ul>

</br>

####Inputs

<ul>

<li> Pcap Capture File (*.cap | *.pcap) <i> - Required</i></li>

<li> GPF+ high-level program (*.gpf) <i> - Optional</i></li>

</ul>

</br>

####Outputs

<ul>

<li> Project File (*.gpf_project) </li>

<li> Packet Index File (*.pidx) </li>

<li> Time Index File (*.tidx) </li>

<li> Filter Files (*.gpf_filter) </li>

<li> Field Files (*.gpf_field) </li>

</ul>

Project files store the location of all output files relevant to a particular program.
They can be used to quickly open existing projects.

</br>


####Using the Software

The system requires both Client and Server executables to be running during classification and post-processing.

By default, both applications connect on port 5555. The Client port can be configured at runtime, while the Server port can be changed through the command line by specifying a port number as an argument (e.g. "Server.exe 1234"). 

<ol>
<li>Click <b>Connect</b> in the client to connect to the server process. </li>
<li>Click <b>Create New Project</b> to open the Processing Options form. 
<li> Add / Drag and Drop one or more identical captures (from different drives) into <b>Source List</b>. </li>
<li> Add / Drag and Drop a GPF+ high-level program into <b>Filter Program</b>, or disable filtering.</li>
<li> Set <b>Buffer Size</b> and <b>Stream Count</b> (memory utilisation is roughly the product of these values).</li>
<li> Add / Drag and Drop project output folder into <b>Project Folder</b> (drag and drop supports folders, and files from within folders)</li>
<li> Give the project a name (defaults to Project1).</li>
<li> Click <b>Start</b>.</li>
</ol>

Once the process completes, the Client will automatically launch the post-processor form.

Once a project has been created, it can be reopened by selecting <b>Load Existing Project</b> from the main <b>Client</b> form (requires Server Connection).

</br>

####Running Standalone Server from the Command Line

The server can execute independently of the Client through the command line. Type "Server.exe ?" to view the command line syntax.

The command line uses pre-compiled GPF+ programs for filtering. Programs can be compiled to file by clicking <b>Compile To File</b> from the main <b>Client</b> form.

</br>

####Using the Post-processors

The Visualiser requires a connection with the Server in order to perform filter counts. The active GPU can be changed from the GPU configuration menu.

#####Visualiser Keyboard Shortcuts:

<ul>

<li><b>Double Click</b> zooms in on highlighted region of the capture.</li>

<li><b>Backspace</b> zooms out.</li>

<li><b>M</b> marks the the highlighted region for use in distillation.</li>

<li><b>Esc</b> clears all marks from the canvas.</li>

</ul>

#####Graph Menu Functions:

<ul>

<li><b>Save Image</b> stores a PNG image of the current graph.</li>
<li><b>Distill Capture From Selection</b> creates a new capture from marked regions, optionally applying a filter.</li>
<li><b>Show Protocol Statistics</b> shows the number of packets in the capture matching each filter.</li>
<li><b>View Field Distribution</b> shows the top values of specific fields.</li>

</ul>

#####Graph Manipulation

The graph display can be manipulated using the filter list in the bottom left of the window.

<ul>

<li>Clicking the coloured square allows the selection of a new graph colour.</li>
<li>Clicking the graph's name box shows or hides the graph.</li>
<li>Clicking the gear symbol opens the graphs render options. This form can adjust the graph type and scale function.</li>
<li>Clicking the arrows changes the graph draw order.</li>
<li>Scrolling with the mouse while hovering over the list will scroll the list.</li>

</ul>


