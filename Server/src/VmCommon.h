#pragma once

#include "zmq.hpp"
#include "cuda_runtime.h"


#define GPF_THREADS				128		//number of threads per block (static)
#define MAX_STREAMS				16

struct FilterOptions
{
	int total_stream_buffers; //buffer enough data for 3 full sets of streams

	bool enabled;
	char* capture_name;
	char* out_folder;			//folder to write filter results to - must be created externally

	int record_start;			//byte start offset of record in packet data
	int record_length;			//byte length of record in packet data
	
	int packets_per_buffer;		//number of records contained in each buffer (uncropped to block size)
	int layer_count;

	int rules_per_packet;
	int filters_per_packet;		//gross filters per packet record (not all are executed)
	int integers_per_packet;	//gross number of integer values copied from each packet (not all are necessarily used in each packet)

	int root_protocol;
	int streams;				//number of streams to divide each buffer over
	int gpu_index;

	char ** filter_names;
	char ** integer_names;

	char * rule_program;
	char * filter_program;
	int * lookup_memory;
	int * static_memory;

	int rule_program_size;
	int filter_program_size;
	int lookup_memory_size;

	FilterOptions() : 
		enabled					(false),
		
		capture_name			(nullptr), 
		out_folder				(nullptr), 
		filter_names			(nullptr),
		integer_names			(nullptr),

		rule_program			(nullptr),
		filter_program			(nullptr),
		lookup_memory			(nullptr),
		static_memory			(nullptr),


		rule_program_size			(0),
		filter_program_size			(0),
		lookup_memory_size			(0),


		record_start				(0), 
		record_length				(0), 
		root_protocol				(0),

		packets_per_buffer			(0),
		layer_count					(0),

		rules_per_packet			(0),
		filters_per_packet			(0),
		integers_per_packet			(0),

		streams						(4),
		gpu_index					(0),
		total_stream_buffers		(3)
	{}


	size_t packet_buffer_size() { return packets_per_buffer * record_length; }

	size_t filter_memory_size() { return static_cast<int>(ceil(static_cast<double>(packets_per_buffer) / 8) * filters_per_packet); } //1-bit per filter

	size_t integer_memory_size() { return packets_per_buffer * 4 * integers_per_packet; }
	
	size_t working_memory_size() { return static_cast<int>(ceil(static_cast<double>(packets_per_buffer) / 8) * rules_per_packet); }

		
	int blocks(int threads) { return packets_per_buffer / (threads  * 32); }


	void get_program(zmq::socket_t& socket)
	{
		zmq::message_t msg;

		//enable filtering
		enabled = true;
		
		//step 0 - get selected gpu index
		socket.recv(&msg);
		memcpy(&gpu_index, msg.data(), sizeof(int));
		
		//step 1 - get selected target mem size
		int target_mem_size;
		socket.recv(&msg);
		memcpy(&target_mem_size, msg.data(), sizeof(int));
		
		//step 2 - get selected stream count
		socket.recv(&msg);
		memcpy(&streams, msg.data(), sizeof(int));
		
		//step 3 - receive capture name
		socket.recv(&msg);

		capture_name = (char*) malloc(msg.size() + 1);
		memcpy(capture_name, msg.data(), msg.size());
		capture_name[msg.size()] = 0;

		//step 4 - receive destination folder
		socket.recv(&msg);

		out_folder = (char*) malloc(msg.size() + 1);
		memcpy(out_folder, msg.data(), msg.size());
		out_folder[msg.size()] = 0;

		//step 5 - receive array of global 32-bit integer constant values
		socket.recv(&msg);
		if (msg.size() != 8 * sizeof(int)) return;
		static_memory = (int*) malloc(msg.size());
		memcpy(static_memory, msg.data(), msg.size());

		
#define STATIC_DATA_START	0
#define STATIC_DATA_LENGTH	1
#define STATIC_RULE_COUNT	2
#define STATIC_FILTER_COUNT	3
#define STATIC_INT_COUNT	4
#define STATIC_LOOKUP_SIZE	5
#define STATIC_LAYER_COUNT	6
#define STATIC_ROOT_PROTO	7

		record_start = static_memory[STATIC_DATA_START];			//byte start offset of record in packet data
		record_length = static_memory[STATIC_DATA_LENGTH];			//byte length of record in packet data
		rules_per_packet = static_memory[STATIC_RULE_COUNT];
		filters_per_packet = static_memory[STATIC_FILTER_COUNT];		//gross filters per packet record (not all are executed)
		integers_per_packet = static_memory[STATIC_INT_COUNT];
		lookup_memory_size = static_memory[STATIC_LOOKUP_SIZE];
		layer_count = static_memory[STATIC_LAYER_COUNT];
		root_protocol = static_memory[STATIC_ROOT_PROTO];

		//step 6 - receive rule program
		socket.recv(&msg);
		rule_program_size = msg.size();
		rule_program = (char*) malloc(msg.size());
		memcpy(rule_program, msg.data(), msg.size());

		//step 7 - receive filter program
		// have to shift up 4 bytes, due to bizarre filter issue where first 3 bytes of constant
		// mem are always zero. Can be removed once cause of this problem is found and resolved.
		// note: if removed, must also remove relevant adjustments in kernel
		socket.recv(&msg);
		filter_program_size = msg.size() + 4;
		filter_program = (char*) malloc(filter_program_size);
		memset(filter_program, 0, 4);
		memcpy(filter_program + 4, msg.data(), msg.size());

		
		//step 8 - receive lookup program
		socket.recv(&msg);
		lookup_memory_size = msg.size();
		lookup_memory = (int*) malloc(msg.size());
		memcpy(lookup_memory, msg.data(), msg.size());

		//step 9 - get filter names
		if (filters_per_packet > 0)
		{
			filter_names = (char **)malloc(filters_per_packet * sizeof(char*));
			for (int k = 0; k < filters_per_packet; k++)
			{
				socket.recv(&msg);
				char * tmp = (char*)malloc(msg.size() + 1);
				memcpy(tmp, msg.data(), msg.size());
				tmp[msg.size()] = 0;
				filter_names[k] = tmp;
			}
		}

		//step 10 - get integer names
		if (integers_per_packet > 0)
		{
			integer_names = (char **)malloc(integers_per_packet * sizeof(char*));
			for (int k = 0; k < integers_per_packet; k++)
			{
				socket.recv(&msg);
				char * tmp = (char*)malloc(msg.size() + 1);
				memcpy(tmp, msg.data(), msg.size());
				tmp[msg.size()] = 0;
				integer_names[k] = tmp;
			}
		}


		size_t mem_per_thread = sizeof(int) * (32 * integers_per_packet + filters_per_packet) + 32 * record_length;
		mem_per_thread = static_cast<int>(ceil(static_cast<double>(mem_per_thread) / 32));

		int mask;
		switch (GPF_THREADS)
		{
		case 64:	mask = 0x7FFFF800; break; //nearest 2k'th packet
		case 128:	mask = 0x7FFFF000; break; //nearest 4k'th packet
		case 256:	mask = 0x7FFFE000; break; //nearest 8k'th packet
		case 512:	mask = 0x7FFFC000; break; //nearest 16k'th packet
		case 1024:	mask = 0x7FFF8000; break; //nearest 32k'th packet
		}
		
		packets_per_buffer = static_cast<int>(ceil(static_cast<double>(target_mem_size * 1024 * 1024) / mem_per_thread)) & mask;
	}
	
	void get_program(char* filename, char* capture, char* destFolder, int gpuIndex, int bufferSize, int streamCount)
	{
		//enable filtering
		enabled = true;
		
		//step 0 - get selected gpu index
		gpu_index = gpuIndex;
		streams = streamCount;
				
		//step 3 - receive capture name
		capture_name = (char*) malloc(strlen(filename) + 1);
		memcpy(capture_name, filename, strlen(filename));
		capture_name[strlen(filename)] = 0;

		//step 4 - receive destination folder
		out_folder = (char*) malloc(strlen(destFolder) + 1);
		memcpy(out_folder, destFolder, strlen(destFolder));
		out_folder[strlen(destFolder)] = 0;


		//step 5 - receive array of global 32-bit integer constant values
		
		FILE * file;
		errno_t error = fopen_s(&file, filename, "rb");

		static_memory = (int*) malloc(8 * sizeof(int));
		fread(static_memory, sizeof(int), 8, file);

		
#define STATIC_DATA_START	0
#define STATIC_DATA_LENGTH	1
#define STATIC_RULE_COUNT	2
#define STATIC_FILTER_COUNT	3
#define STATIC_INT_COUNT	4
#define STATIC_LOOKUP_SIZE	5
#define STATIC_LAYER_COUNT	6
#define STATIC_ROOT_PROTO	7

		record_start = static_memory[STATIC_DATA_START];			//byte start offset of record in packet data
		record_length = static_memory[STATIC_DATA_LENGTH];			//byte length of record in packet data
		rules_per_packet = static_memory[STATIC_RULE_COUNT];
		filters_per_packet = static_memory[STATIC_FILTER_COUNT];		//gross filters per packet record (not all are executed)
		integers_per_packet = static_memory[STATIC_INT_COUNT];
		lookup_memory_size = static_memory[STATIC_LOOKUP_SIZE];
		layer_count = static_memory[STATIC_LAYER_COUNT];
		root_protocol = static_memory[STATIC_ROOT_PROTO];

		//step 6 - receive rule program
		fread(&rule_program_size, sizeof(int), 1, file);
		//rule_program_size = atoi(readBuffer);
		rule_program = (char*) malloc(rule_program_size);
		fread(rule_program, sizeof(char), rule_program_size, file);

		//step 7 - receive filter program
		// have to shift up 4 bytes, due to bizarre filter issue where first 3 bytes of constant
		// mem are always zero. Can be removed once cause of this problem is found and resolved.
		// note: if removed, must also remove relevant adjustments in kernel
		fread(&filter_program_size, sizeof(int), 1, file);
		//filter_program_size = atoi(readBuffer);
		filter_program = (char*) malloc(filter_program_size + 4);
		memset(filter_program, 0, 4);
		fread(filter_program + 4, sizeof(char), filter_program_size, file);
		filter_program_size += 4;

		
		//step 8 - receive lookup program
		fread(&lookup_memory_size, sizeof(int), 1, file);
		//lookup_memory_size = atoi(readBuffer);
		lookup_memory = (int*) malloc(lookup_memory_size);
		fread(lookup_memory, sizeof(int), lookup_memory_size/4, file);
		
		int size;

		//step 9 - get filter names
		if (filters_per_packet > 0)
		{
			filter_names = (char **)malloc(filters_per_packet * sizeof(char*));
			for (int k = 0; k < filters_per_packet; k++)
			{
				fread(&size, sizeof(int), 1, file);
				//int size = atoi(readBuffer);
						
				char * tmp = (char*)malloc(size + 1);
				fread(tmp, sizeof(char), size, file);

				tmp[size] = 0;
				filter_names[k] = tmp;
			}
		}

		//step 10 - get integer names
		if (integers_per_packet > 0)
		{
			integer_names = (char **)malloc(integers_per_packet * sizeof(char*));
			for (int k = 0; k < integers_per_packet; k++)
			{
				
				fread(&size, sizeof(int), 1, file);
		
				char * tmp = (char*)malloc(size + 1);
				fread(tmp, sizeof(char), size, file);

				tmp[size] = 0;
				integer_names[k] = tmp;
			}
		}

		size_t mem_per_thread = sizeof(int) * (32 * integers_per_packet + filters_per_packet) + 32 * record_length;
		mem_per_thread = static_cast<int>(ceil(static_cast<double>(mem_per_thread) / 32));

		int mask;
		switch (GPF_THREADS)
		{
		case 64:	mask = 0x7FFFF800; break; //nearest 2k'th packet
		case 128:	mask = 0x7FFFF000; break; //nearest 4k'th packet
		case 256:	mask = 0x7FFFE000; break; //nearest 8k'th packet
		case 512:	mask = 0x7FFFC000; break; //nearest 16k'th packet
		case 1024:	mask = 0x7FFF8000; break; //nearest 32k'th packet
		}

		packets_per_buffer = static_cast<int>(ceil(static_cast<double>(bufferSize * 1024 * 1024) / mem_per_thread)) & mask;
		
	}

};


struct OutputPtr
{
	int * data;
	size_t size;
	int type;
	int packets;
	bool more;

	OutputPtr() : data(nullptr), size(0), type(0), packets(0), more(true) {}
};

//generic structure for passing buffer pointers
struct CudaBufferPointer
{
	char* buffer;
	size_t size;
	bool more;

	CudaBufferPointer() : buffer(nullptr), size(0), more(true) {}
};

struct ProcessArgs
{
	zmq::context_t * context;
	FilterOptions options;
};

