#include "OutputBuffer.h"
#include "zmq.hpp"
#include "cuda_runtime.h"
#include "VmCommon.h"
#include "tinythread.h"
#include "Common.h"

#define OUTPUT_FILTER 0
#define OUTPUT_INTEGER 1

#define OUTPUT_MAGIC 0x1234ABCD

//Header Format
//	 /-------------------------------------------------------------------------------------------------------------------\
//	|| ## MAGIC (int) ## | ## TYPE (int) ## | ## RECORDS (int64) ## | ## NAME (char[128]) ## | ## CAPTURE (char[128]) ## || --> records [8 per byte]  
//	 \-------------------------------------------------------------------------------------------------------------------/

void OutputWriter(void * args);
void WriteFileHeader(char* filename, int type, char* capture);
void InitialiseOutput(char** &filter_filenames, char** &integer_filenames, FilterOptions options);

OutputBuffer::OutputBuffer(ProcessArgs args)
{
	context = args.context;
	options = args.options;

	socket = new zmq::socket_t(*context, ZMQ_PAIR);
	field_return = new zmq::socket_t(*context, ZMQ_PAIR);
	filter_return = new zmq::socket_t(*context, ZMQ_PAIR);

	socket->bind("inproc://output");
	field_return->bind("inproc://output_field_ret");
	filter_return->bind("inproc://output_filter_ret");

	tthread::thread * thread = new tthread::thread(OutputWriter, &args);
	

}

OutputBuffer::~OutputBuffer()
{

}

int* OutputBuffer::GetFieldBuffer()
{
	int* tmp;
	field_return->recv(&tmp, sizeof(int*));
	return tmp;
}

int* OutputBuffer::GetFilterBuffer()
{
	int* tmp;
	filter_return->recv(&tmp, sizeof(int*));
	return tmp;
}

void OutputBuffer::CopyFilterResults(int * results, size_t size, int packets)
{
	OutputPtr ptr;
	ptr.data = results;
	ptr.size = size;
	ptr.type = OUTPUT_FILTER;
	ptr.packets = packets;
	ptr.more = true;

	socket->send(&ptr, sizeof(OutputPtr));
}

void OutputBuffer::CopyIntegerResults(int * results, size_t size, int packets)
{
	OutputPtr ptr;
	ptr.data = results;
	ptr.size = size;
	ptr.type = OUTPUT_INTEGER;
	ptr.packets = packets;
	ptr.more = true;

	socket->send(&ptr, sizeof(OutputPtr));
}

void OutputBuffer::Finished(_int64 packet_count)
{
	OutputPtr ptr;
	ptr.more = false;
	
	socket->send(&ptr, sizeof(OutputPtr));

	socket->send(&packet_count, sizeof(_int64));

	//destroy filter buffers
	for (int k = 0; k < options.streams * 3; k++)
	{
		int* tmp = GetFilterBuffer();
		cudaFreeHost(tmp);
	}

	//destroy field buffers
	for (int k = 0; k < options.streams * 3; k++)
	{
		int* tmp = GetFieldBuffer();
		cudaFreeHost(tmp);
	}

	socket->close();
	field_return->close();
	filter_return->close();

}

void WriteFileHeader(char* filename, int type, char* capture)
{
	char* buffer = (char*)malloc(272);
	FILE * file;
	errno_t err = fopen_s(&file, filename, "wb");
	if (err != 0)
	{
		POST_ERROR("FileWriterThread", "File creation failed.");
		getchar();
	}
	memset(buffer, 0, 272);
	int magic = OUTPUT_MAGIC;
	_int64 records = 0;
		
	memcpy(buffer, &magic, sizeof(int));
	memcpy(buffer + 4, &type, sizeof(int));
	memcpy(buffer + 8, &records, sizeof(_int64));

	memcpy(buffer + 16, filename, strlen(filename));
	memcpy(buffer + 144, capture, strlen(capture));

	_fwrite_nolock(buffer, 1, 272, file);	//records unknown
		
	fclose(file);
	free(buffer);
}

void FinalizeFile(char* filename, _int64 packet_count)
{
	FILE * file;
	errno_t err = fopen_s(&file, filename, "rb+");
	if (err != 0)
	{
		POST_ERROR("FileWriterThread", "File creation failed.");
		getchar();
	}

	fseek(file, 8, SEEK_SET);
	_fwrite_nolock(&packet_count, 8, 1, file);
	
	fclose(file);
}

void InitialiseOutput(char** &filter_filenames, char** &integer_filenames, FilterOptions options)
{
	
	if (options.filters_per_packet  > 0) 
	{
		filter_filenames = (char**)malloc(options.filters_per_packet * sizeof(char*));
		for (int k = 0 ; k < options.filters_per_packet; k++)
		{
			char * tmp = (char*)malloc(256);
			sprintf(tmp, "%s%s.gpf_filter\0", options.out_folder, options.filter_names[k]);
			filter_filenames[k] = tmp;
		}
	}
		
		
	if (options.integers_per_packet  > 0) 
	{
		integer_filenames = (char**)malloc(options.integers_per_packet * sizeof(char*));
		for (int k = 0 ; k < options.integers_per_packet; k++)
		{
			char * tmp = (char*)malloc(256);
			sprintf(tmp, "%s%s.gpf_field\0", options.out_folder, options.integer_names[k]);
			integer_filenames[k] = tmp;
		}
	}

	int type;
	//init empty files with header
	for (int k = 0 ; k < options.filters_per_packet; k++) WriteFileHeader(filter_filenames[k], OUTPUT_FILTER, options.capture_name);
	
		
	//init empty files with header
	for (int k = 0 ; k < options.integers_per_packet; k++) WriteFileHeader(integer_filenames[k], OUTPUT_INTEGER, options.capture_name);
	
	

}
void WriteSegment(char* filename, void * buffer, size_t size)
{
	FILE * file;
	errno_t err = fopen_s(&file, filename, "ab"); //open the file in append mode
				
	if (err != 0)
	{
		POST_ERROR("FileWriterThread", "File creation failed.");
		getchar();
	}

	fwrite(buffer, 1, size, file);	//write relevant portion of data
	fclose(file); //close file

}

void OutputWriter(void * args)
{
	ProcessArgs * proc = static_cast<ProcessArgs*>(args);
	FilterOptions options = proc->options;
	zmq::socket_t socket(*proc->context, ZMQ_PAIR);
	zmq::socket_t complete(*proc->context, ZMQ_PAIR);
	zmq::socket_t field_ret(*proc->context, ZMQ_PAIR);
	zmq::socket_t filter_ret(*proc->context, ZMQ_PAIR);

	socket.connect("inproc://output");
	complete.connect("inproc://complete_filter");

	field_ret.connect("inproc://output_field_ret");
	filter_ret.connect("inproc://output_filter_ret");

	OutputPtr ptr;
	char** filter_filenames = nullptr;
	char** integer_filenames = nullptr;

	//intitalise the output files with correct header, ready for appending results
	InitialiseOutput(filter_filenames, integer_filenames, options);

	
	//allocate filter buffers
	for (int k = 0; k < options.streams * 3; k++)
	{
		int* tmp;
		cudaHostAlloc((void**) &tmp, options.filter_memory_size(), cudaHostAllocDefault); 
		//CheckError("Error allocating host filter output buffers.");
		filter_ret.send(&tmp, sizeof(int*));

		cudaHostAlloc((void**) &tmp, options.integer_memory_size(), cudaHostAllocDefault); 
		//CheckError("Error allocating host filter output buffers.");
		field_ret.send(&tmp, sizeof(int*));
	}

	while(true)
	{		
		socket.recv(&ptr, sizeof(OutputPtr));
		if (!ptr.more) break;
		
		int count;
		size_t size_bytes;
		size_t relevant;
		char ** filenames;
		zmq::socket_t * ret_socket = nullptr;

		switch (ptr.type)
		{
		case OUTPUT_FILTER:
			count = options.filters_per_packet;
			relevant = ptr.packets / 8 + (ptr.packets % 8 == 0 ? 0 : 1);
			filenames = filter_filenames;
			ret_socket = &filter_ret;
			break;
		case OUTPUT_INTEGER:
			count = options.integers_per_packet;
			relevant = ptr.packets * sizeof(int);
			filenames = integer_filenames;
			ret_socket = &field_ret;
			break;
		}

		size_bytes = ptr.size / count;
		//divide up filter output eqully between each filter
		for (int k = 0; k < count; k++)
		{
			WriteSegment(filenames[k], (char*)ptr.data + k * size_bytes, relevant);
		}
		
		ret_socket->send(&ptr.data, sizeof(int*));

		//cudaFreeHost(ptr.data);
	} 
	
	//write packet count to all three files
	_int64 packet_count;
	socket.recv(&packet_count, sizeof(_int64));
	
	for (int k = 0; k < options.filters_per_packet; k++) FinalizeFile(filter_filenames[k], packet_count);
	for (int k = 0; k < options.integers_per_packet; k++) FinalizeFile(integer_filenames[k], packet_count);

	int msg = 0;
	complete.send(&msg, sizeof(int));

	socket.close();
	complete.close();
	field_ret.close();
	filter_ret.close();


}