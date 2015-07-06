#include "OutputBuffer.h"
#include "zmq.hpp"
#include "cuda_runtime.h"
#include "VmCommon.h"
#include "tinythread.h"

#define OUTPUT_FILTER 0
#define OUTPUT_INTEGER 1

#define OUTPUT_MAGIC 0x01234ABCD

//Header Format
//	 /-------------------------------------------------------------------------------------------------------------------\
//	|| ## MAGIC (int) ## | ## TYPE (int) ## | ## RECORDS (int64) ## | ## NAME (char[128]) ## | ## CAPTURE (char[128]) ## || --> records [8 per byte]  
//	 \-------------------------------------------------------------------------------------------------------------------/

struct OutputPtr
{
	char * data;
	int packets;
	int type;
	bool more;

	OutputPtr() : data(nullptr), packets(0), type(0), more(true) {}
};

OutputBuffer::OutputBuffer(ProcessArgs args)
{
	//context = args.context;
	//options = args.options;

	//socket = new zmq::socket_t(context, ZMQ_PAIR);
	//socket.bind("inproc://output");

	//tthread::thread * thread = new tthread::thread(OutputWriter, args);
}

OutputBuffer::~OutputBuffer()
{

}

void OutputBuffer::CopyFilterResults(char * results, size_t size)
{
	//OutputPtr * ptr = new OutputPtr();
	//ptr->data = results;
	//ptr->size = size;
	//ptr->type = OUTPUT_INTEGER;
	//ptr->more = true;

	//socket.send(ptr, sizeof(void *));
}

void OutputBuffer::Finished()
{
	//OutputPtr * ptr = new OutputPtr();
	//ptr->more = false;
	//
	//socket.send(ptr, sizeof(void *));
}

void InitialiseOutput(char** filenames, int type, int file_count, char** names, char* out_folder, char* capture)
{
	//void* buffer = malloc(272, 1);
	//
	//
	//if (file_count  > 0) 
	//{
	//	filenames = (char**)malloc(proc->filters_per_packet,  sizeof(char*));
	//	for (int k = 0 ; k < proc->filters_per_packet; k++)
	//	{
	//		filenames[k] = (char*)malloc(256, 1);
	//		sprintf(filenames[k], "%s\\%s\0", out_folder, names[k]);
	//	}
	//}

	////init empty files with header
	//for (int k = 0 ; k < proc->filters_per_packet; k++)
	//{
	//	FILE * file;
	//	fopen(filenames[k], "wb");
	//	
	//	memset(buffer, 0, 272);
	//	int magic = OUTPUT_MAGIC;
	//	_int64 records = 0;

	//	memcpy(buffer, &magic, sizeof(int));
	//	memcpy(buffer + 4, &type, sizeof(int));
	//	memcpy(buffer + 8, &records, sizeof(_int64));

	//	memcpy(buffer + 16, names[k], 128);
	//	memcpy(buffer + 144, capture, 128);

	//	fwrite(buffer, 1, 272, file);	//records unknown
	//	
	//	fclose(file);
	//}
}

void OutputWriter(void * args)
{
	//ProcessArgs * proc = static_cast<ProcessArgs*>(args);

	//zmq::socket_t socket(proc->context, ZMQ_PAIR);
	//socket.connect("inproc://output");

	//OutputPtr * ptr;
	//char** filter_filenames;
	//char** integer_filenames;

	////intitalise the output files with correct header, ready for appending results
	//InitialiseOutput(filter_filenames, OUTPUT_FILTER, proc->filters_per_packet, proc->filter_names, proc->out_folder, proc->capture_name);
	//InitialiseOutput(integer_filenames, OUTPUT_INTEGER, proc->integers_per_packet, proc->integer_names, proc->out_folder, proc->capture_name);

	//while(true)
	//{		
	//	socket.recv(ptr);
	//	if (!ptr->more) 
	//	{
	//		//get packet count
	//		break; 
	//	}

	//	if (ptr->type == OUTPUT_FILTER)
	//	{
	//		size_t filter_size_bytes = ptr->size / proc->filters_per_packet;
	//		//divide up filter output eqully between each filter
	//		for (int k = 0; k < proc->filters_per_packet; k++)
	//		{
	//			FILE * file;
	//			fopen(proc->filter_filenames[k], "ab"); //open the file in append mode
	//			fwrite(ptr->data + k * filter_size_bytes, 1, filter_size_bytes, file);	//write relevant portion of data
	//			fclose(file); //close file
	//		}
	//	}
	//	else if (ptr->type == OUTPUT_INTEGER)
	//	{
	//		size_t integer_size_bytes = ptr->size / proc->integers_per_packet;
	//		//divide up integer output eqully between each integer
	//		for (int k = 0; k < proc->integers_per_packet; k++)
	//		{
	//			FILE * file;
	//			fopen(proc->integer_filenames[k], "ab"); //open the file in append mode
	//			fwrite(ptr->data + k * integer_size_bytes, 1, integer_size_bytes, file);	//write relevant portion of data
	//			fclose(file); //close file
	//		}

	//	}

	//	free(ptr->data);
	//} 
	
}