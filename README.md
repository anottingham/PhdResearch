
##PhD Research - GPF+ (Experimental)

###Source Overview

</br>

####Solution Overview

The Visual Studio 2012 Solution includes the following projects:

<li><b>Client</b> - The .NET client application. Hosts Grammar and ViewTimeline.</li>

<li><b>Grammar</b> - The ANTLR / .NET DSL Compiler.</li>

<li><b>Server</b> - The CUDA / C++ Classification Server. </li>

<li><b>ViewTimeline</b> - The .NET post-processing functions.</li>

These projects are organised into separate folders in the solution.

</br>

####Executables:

<li><b>Client.exe</b> - Encapsulates .NET functions. Required for all GUI functions.</li>

<li><b>Server.exe</b> - Encapsulates C++ functions. Uses CUDA 6.5 Runtime API.</li>

</br>

####Additional Files

All additional files are hosted on Dropbox. Click the file name to download.
<ul>
<li><b><a href=https://www.dropbox.com/s/xv1sfup1rz6pjxm/Source.rar?dl=0>Source.rar</a></b> contains the source files for the solution (as an alternative to Git). </li>
<li><b><a href=https://www.dropbox.com/s/i2dt5fc16ghijew/Binary.rar?dl=0>Binary.rar</a></b> contains compiled debug and release binaries, with all required libraries.</li>
<li><b><a href=https://www.dropbox.com/s/z1uzib8c8k4exzt/Programs.rar?dl=0>Programs.rar</a></b> contains all programs used during testing (source and compiled)</li>
<li>Output Project files for Program Set A (Filter Only) for Captures A, B and C. Projects can be viewed by selecting <b>Load Existing Project</b> from the main Client form (requires Server Connection).  
<ul><li><b><a href=https://www.dropbox.com/s/x5ughqbyqgf6xp5/Capture%20A.rar?dl=0>Capture A.rar</a></b> (19 MB)</li>
<li><b><a href=https://www.dropbox.com/s/ghaw72bn61z6pqd/Capture%20B.rar?dl=0>Capture B.rar</a></b> (276 MB)</li>
<li><b><a href=https://www.dropbox.com/s/1cwekopovdykwas/Capture%20C.rar?dl=0>Capture C.rar</a></b> (329 MB)</li>
</ul>
</ul>
</br>

####Minimum Requirements

<li> x64 CPU (x86 is not currently supported)</li>

<li> Compute Capability 3.5+ Nvidia GPU (currently compiles for 3.5 and 5.0 explicitly) </li> 

</br>

####Inputs

<li> Pcap Capture File (\*.cap | \*.pcap) <i> - Required</i></li>

<li> GPF+ high-level program (\*.gpf) <i> - Optional</i></li>

</br>

####Outputs

<li> Project File (\*.gpf_project) </li>

<li> Packet Index File (\*.pidx) </li>

<li> Time Index File (\*.tidx) </li>

<li> Filter Files (\*.gpf_filter) </li>

<li> Field Files (\*.gpf_field) </li>

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

</br>

####Running Standalone Server from the Command Line

The server can execute independently of the Client through the command line. Type "Server.exe ?" to view the command line syntax.

The command line uses pre-compiled GPF+ programs for filtering. Programs can be compiled to file by clicking <b>Compile To File</b> from the main <b>Client</b> form.

</br>

####Visualiser Controls
<ul>
<li><b>Double Click</b> zooms in on highlighted region of the capture.</li>

<li><b>Backspace</b> zooms out.</li>

<li><b>M</b> marks the the highlighted region for use in distillation.</li>

<li><b>Esc</b> clears all marks from the canvas.</li>
</ul>
