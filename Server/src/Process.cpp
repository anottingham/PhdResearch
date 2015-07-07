#include "zmq.hpp"
//#include "FileBuffer.h"
//#include "ProgressBar.h"
#include <ctime>
#include "GpfServer.h"
#include "ProcessTimer.h"


void executable(int argc, char* argv[], zmq::context_t& context)
{
	//run in executable mode (no client)
	int k = 3;
	int streams = 8;
	int bufferSize = 32;
	int gpuIndex = 0;
	int fileCount = 1;
	char** filenames = nullptr;
	
	bool indexing_enabled = false;
	bool filtering_enabled = false;
	char* pidx_file;
	char* tidx_file;

	bool timing = false;
	ProcessTimer * timer;

	char* filter_program;

	ProcessOptions options;

	while (k < argc)
	{
		if (strcmp(argv[k],"-s") == 0)
		{
			streams = atoi(argv[k + 1]);
			k += 2;
		}
		else if (strcmp(argv[k],"-b") == 0)
		{
			bufferSize = atoi(argv[k + 1]);
			k += 2;
		}
		else if (strcmp(argv[k],"-i") == 0)
		{
			options.SetIndexOptions(argv[k + 1], argv[k + 2]);

			//options.index.pidx_file = (char*)malloc(strlen(argv[k + 1] + 1));
			//strcpy(options.index.pidx_file, argv[k + 1]);

			//options.index.tidx_file = (char*)malloc(strlen(argv[k + 2] + 1));
			//strcpy(options.index.tidx_file, argv[k + 2]);
			k += 3;
		}
		else if (strcmp(argv[k],"-f") == 0)
		{
			filter_program = argv[k+1];
			filtering_enabled = true;
			k += 2;
		}
		else if (strcmp(argv[k],"-g") == 0)
		{
			gpuIndex = atoi(argv[k + 1]);
			k += 2;
		}
		else if (strcmp(argv[k],"-a") == 0)
		{
			int count = atoi(argv[k + 1]);
			fileCount = 1 + count;

			filenames = (char**)malloc(fileCount * sizeof(char*));
			filenames[0] = (char*)malloc(strlen(argv[1]) + 1);
			strcpy(filenames[0], argv[1]);

			for (int j = 0; j < count; j++)
			{
				filenames[j + 1] = (char*)malloc(1 + strlen(argv[k + 2 + j]));
				strcpy(filenames[j + 1], argv[k + 2 + j]);
			}
			k += 2 + count;
		}
		else if (strcmp(argv[k],"-t") == 0)
		{
			timing = true;
			timer = new ProcessTimer(argv[k + 1]);
			k += 2;
		}
	}
	
	if (fileCount == 1)
	{
		filenames = (char**)malloc(sizeof(char**));
		filenames[0] = (char*)malloc (strlen(argv[1]) + 1);
		strcpy(filenames[0], argv[1]);
	}

	//set file options
	options.SetFileOptions(fileCount, filenames);

	if (filtering_enabled)	options.filter.get_program(filter_program, argv[1], argv[2], gpuIndex, bufferSize, streams);

	zmq::socket_t progress(context, ZMQ_PAIR);
	progress.bind("inproc://progress");
	
	zmq::socket_t complete_filter(context, ZMQ_PAIR);
	zmq::socket_t complete_index(context, ZMQ_PAIR);
	complete_filter.bind("inproc://complete_filter");
	complete_index.bind("inproc://complete_index");

	Processor * proc = new Processor(&context, options);
	
	if (timing) timer->Start();
	
	proc->Start();

	_int64 curr = 0;
	_int64 total;
	progress.recv(&total, sizeof(_int64));	//get blocks fro progress bar		

	while (curr != total)
	{
		progress.recv(&curr, sizeof(_int64));
	}

	
	int response;
		
	if (filtering_enabled) complete_filter.recv(&response, sizeof(int)); //msg from output writer to signal completion
	if (indexing_enabled) complete_filter.recv(&response, sizeof(int)); //msg from output writer to signal completion

	if (timing) timer->Stop();

	progress.close();
	complete_filter.close();
	complete_index.close();
	proc->Stop();
	
}

int main (int argc, char* argv[]) 
{
	//Wait for signal
	if (argc < 2)
	{
		
		GpfServer server;
		server.Start();
	}
	
	else if (argc == 2)
	{
		if (argv[1][0] == '?')
		{
			printf("\nUsage (Server Mode):\n\t%s (<portNumber>)\n",argv[0]);
			printf("\nUsage (Executable Mode):\n\t%s <captureFile> <destinationFolder> (-s <streamCount> | -b <bufferSize>| -i <indexFile> <timeFile> |\n\t -f <filterProgram> | -g <gpuIndex> | -a # <file1> ... | -t <outfile>)\n\n",argv[0]);
			printf("-s\t: The number of streams to use. Default is 8.\n");
			printf("-b\t: The buffer size in MB. Three buffers are allocated per stream. Default size is 32.\n");
			printf("-i\t: Enables Indexing, specifying the index and time file names.\n");
			printf("-f\t: Specifies the compiled gpf program. Extension `.gpf_c'.\n");
			printf("-g\t: Specifies the gpu to use. Default is 0.\n");
			printf("-a\t: Additional copies of the main capture file. Used to increase read speed.\n\t  Requires an indication of the number of additional files (1+) and the filename of each.\n\t  e.g. c:\\a.cap -a 3 d:\\a.cap e:\\a.cap f:\\a.cap ...\n\n");
			printf("-t\t: Perform timing, appending results to <outfile>.\n\n");
			exit(0);			
		}
		else
		{
			int port = atoi(argv[1]);
			GpfServer server;
			server.Start(port);
		}
	}
	zmq::context_t context(1);
	executable(argc, argv, context);
	

	exit(0); //context close hangs - force exit until fixed
	//context.close();
	//return 0;

}