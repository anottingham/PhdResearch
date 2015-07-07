
##PhD Research - GPF+ (Experimental)

###Source Overview

</br>

#####Solution Overview

The Visual Studio 2012 Solution includes the following projects:

<b>Client</b> - The .NET client application. Hosts Grammar and ViewTimeline.

<b>Grammar</b> - The ANTLR / .NET DSL Compiler.

<b>Server</b> - The CUDA / C++ Classification Server. 

<b>ViewTimeline</b> - The .NET post-processing functions.

These projects are organised into separate folders in the solution.

</br>

#####Executables:

<b>Client.exe</b> - Encapsulates .NET functions. Required for all GUI functions.

<b>Server.exe</b> - Encapsulates C++ functions. Uses CUDA 6.5 Runtime API.

</br>

#####Minimum Requirements

<li> x64 CPU (x86 is not currently supported)</li>

<li> Compute Capability 3.5+ Nvidia GPU (currently compiles for 3.5 and 5.0 explicitly) </li> 

</br>

#####Inputs

<li> Pcap Capture File (\*.cap | \*.pcap) <i> - Required</i></li>

<li> GPF+ high-level program (\*.gpf) <i> - Optional</i></li>

</br>

#####Outputs

<li> Project File (\*.gpf_project) </li>

<li> Packet Index File (\*.pidx) </li>

<li> Time Index File (\*.tidx) </li>

<li> Filter Files (\*.gpf_filter) </li>

<li> Field Files (\*gpf_field) </li>

Project files store the location of all output files relevant to a particular program.
They can be used to quickly open existing projects.

</br>


#####Using the Software

The system requires both Client and Server executables to be running during both classification and post-processing.



