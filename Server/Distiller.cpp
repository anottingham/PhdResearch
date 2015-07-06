#include "zmq.hpp"
#include "tinythread.h"

//user specified
#define DISTILL_READ_WIDTH_KB 64	//must be a factor of block size
#define DISTILL_BLOCK_SIZE_MB 64

#define DISTILL_READ_WIDTH (READ_WIDTH_KB * 1024)
#define DISTILL_BLOCK_SIZE (BLOCK_SIZE_MB * 1024 * 1024)
#define DISTILL_READS_PER_BLOCK (BLOCK_SIZE / READ_WIDTH)

#define DISTILL_BUFFERS 6


void DistillReader(void* distillOptions)
{
	DistillerOptions* options = static_cast<DistillerOptions>(distillOptions);
	zmq::context_t* context = options->context;
	zmq::socket_t writer_socket(*context, ZMQ_PAIR);

	reader_socket.bind("inproc://distill_raw");
	
	zmq::socket_t empty_socket(context, ZMQ_PAIR);
	empty_socket.bind("inproc://empty");

	zmq::socket_t task_socket(context, ZMQ_PAIR);
	task_socket.connect("inproc://task");

	
	FILE * file;
	errno_t error = fopen_s(&file, args->options.capture_name, "rb"); //open file
	if (error != 0) { POST_ERROR("Distiller Reader Thread", "File could not be opened."); }	
	
	char* header;
	empty.recv(&header, sizeof(char*));
	fread(header, sizeof(char), 24, file);

	writer_socket.send(&header, sizeof(DistillerChunk));
	
	char* data;
	for (int k = 0; k < options->task_count; k++)
	{
		DistillTask task;
		task_socket.recv(&task, sizeof(DistillTask));
		
		_fseeki64(file, task.byte_start, SEEK_SET);

		size_t  read_size = task.byte_count;
		empty.recv(&data, sizeof(char*));
		size_t bytes_read = fread(data, sizeof(char), read_size, file);

		if (bytes_read != read_size)
		{
			if (feof(file))
			{
				POST_ERROR("Distiller Reader Thread", "Early EOF"); 
			}
			if (ferror(file)) //handle
			{
				POST_ERROR("Distiller Reader Thread", "File read error"); 
			}
		}
		
		task.buffer = data;
		task.size = bytes_read;

		writer_socket.send(&task, sizeof(DistillerTask));
	}
	
	//destory empty buffers
	for (int k = 0; k < DISTILL_BUFFERS; k++)
	{
		char * buf;
		empty_socket.recv(&buf, sizeof(char*));
		free(buf);
	}

	
	task_socket.close();
	reader_socket.close();
	empty_socket.close();
}


void DistillWriter(void* distillOptions)
{
	DistillerOptions* options = static_cast<DistillerOptions>(distillOptions);
	zmq::context_t* context = options->context;

	ProgressBar bar(options->context, options->total_data);
		
	zmq::socket_t reader_socket(context, ZMQ_PAIR);
	reader_socket.connect("inproc://distill_raw");

	zmq::socket_t used_socket(context, ZMQ_PAIR);
	used_socket.connect("inproc://empty");
	
	//send empty buffers
	for (int k = 0; k < DISTILL_BUFFERS; k++)
	{
		char * buf = (char*)malloc(DISTILL_BLOCK_SIZE);
		used_socket.send(&buf, sizeof(char*));
	}

	//create output file
	FILE * out_file;
	errno_t error = fopen_s(&out_file, args->options.out_name, "wb"); //open file
	if (error != 0) { POST_ERROR("Distiller Writer Thread", "Output file could not be opened."); }	

	//get file header from reader thread
	char * header;
	reader_socket.recv(&header, sizeof(char*));
	_fwrite_nolock(header, sizeof(char), 24, out_file);	//records unknown
	empty_socket.send(&header, sizeof(char*));
	
	IndexReader index(options);
	PcapWriter writer(options);

	_int64 processedData = 0;
	_int64 currChunk = 0;

	for (int k = 0; k < options->task_count; k++)
	{
		DistillerTask task;
		reader_socket.recv(&task, sizeof(DistillerTask));
		
		writer.SetIndex(task.index_start, task.index_count);
		index.LoadRange(task.index_start, task.index_count);
		
		for (int j = 0; j < task.index_count; j++)
		{
			char * packet_ptr = task.buffer + (index.GetNext() - task.byte_start);
			unsigned int packet_length;
			memcpy(&packet_length, packet_ptr + 12, sizeof(unsigned int));

			writer.AddPacket(packet_ptr, packet_length);
		}

		bar.Update(task.size);
		used_socket.send(task.buffer, sizeof(char*));
	}
	
	progress_socket.close();
	reader_socket.close();
	used_socket.close();
}


void Distiller(zmq::context_t& context, zmq::socket_t& client_socket)
{
	zmq::socket_t progress_socket(context, ZMQ_PAIR);
	progress_socket.bind("inproc://progress");

	zmq::socket_t task_socket(context, ZMQ_PAIR);
	task_socket.bind("inproc://task");

	//build distiller options
	DistillerOptions options;
	
	zmq::message_t file;

	//get capture
	client_socket.recv(&file);
	options.capture_name = (char*) malloc(file.size() + 1);
	memcpy(options.capture_name, file.data(), file.size());
	options.capture_name[file.size()] = 0;

	//get index 
	client_socket.recv(&file);
	options.index_file = (char*) malloc(file.size() + 1);
	memcpy(options.index_file, file.data(), file.size());
	options.index_file[file.size()] = 0;
	
	//get outfile 
	client_socket.recv(&file);
	options.out_name = (char*) malloc(file.size() + 1);
	memcpy(options.out_name, file.data(), file.size());
	options.out_name[file.size()] = 0;

	//get filer 
	bool filter = false;
	client_socket.recv(&filter, sizeof(bool));
	if (filter)
	{
		options.filter = filter;
		client_socket.recv(&file);
		options.filter_file = (char*) malloc(file.size() + 1);
		memcpy(options.filter_file, file.data(), file.size());
		options.filter_file[file.size()] = 0;
	}

	client_socket.recv(&options.task_count, sizeof(int));
	client_socket.recv(&options.total_data, sizeof(_int64));

	options.context = &context;

	//create reader thread
	tthread::thread * reader = new tthread::thread(DistillReader, &options);

	//create writer thread
	tthread::thread * writer = new tthread::thread(DistillWriter, &options);

	//issue tasks
	for (int k = 0; k < task_count; k++)
	{
		DistillTask task;
		//index index info
		client_socket.recv(&task.index_start, sizeof(_int64));
		client_socket.recv(&task.index_count, sizeof(int));
		//start and end byte
		client_socket.recv(&task.byte_start, sizeof(_int64));
		client_socket.recv(&task.byte_count, sizeof(int));

		task_socket.send(&task, sizeof(DistillTask)); 
	}

	//poll progress
	while (progress < options.total_data)
	{
		_int64 progress;
		progress_socket.recv(&progress, sizeof(_int64));
		client_socket.send(&progress, sizeof(_int64));
	}

	//close

	progress_socket.close();
	task_socket.close();
}