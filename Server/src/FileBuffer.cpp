//
#include "tinythread.h"
#include <cassert>
#include <string>
#include <iostream>
#include <cmath>
#include <unordered_map>
//#include "zmq.hpp"
#include "zhelpers.hpp"
#include "FileBuffer.h"

#include "Common.h"


#define READ_WIDTH (READ_WIDTH_KB * 1024)
#define BLOCK_SIZE (BLOCK_SIZE_MB * 1024 * 1024)
#define BATCH_SIZE (BATCH_SIZE_MB/BLOCK_SIZE_MB)
#define READS_PER_BLOCK (BLOCK_SIZE / READ_WIDTH)

#define BUFFERS_PER_FILE (BATCH_SIZE * 3)

void Distributer(void *arg);
void Worker (void *arg);
void Collector(void* arg);
void ReadSingleFile(void* arg);

void FakeWorker (void *arg);
void FakeReadSingleFile(void *arg);
void FakeReadSingleFileLB(void *arg);

//setup context for worker processes
struct WorkerContext
{
	zmq::context_t * context;
	char * filename;
	int blocks;
	int id;

	WorkerContext(int thread_id, zmq::context_t * zmq_context, char * file_name, int total_blocks)
	{
		id = thread_id;
		context = zmq_context;
		filename = file_name;
		blocks = total_blocks;
	}
};

//contains ptr to data for transport to calling thread
struct DataChunk
{
	char * buffer;	//ptr to buffer
	size_t size;	//size of the buffer
	int seq;	//sequence number

	DataChunk() : buffer(nullptr), size(0), seq(-1) {}

	DataChunk(char* buffer_ptr, size_t buf_size, int sequence_no)
	{
		seq = sequence_no;
		buffer = buffer_ptr;
		size = buf_size;
	}
};

//setup context for gather process
struct BufferContext
{
	zmq::context_t * context;
	FileOptions options; 
	_int64 file_size;
	int blocks;
	int batch_count;

	BufferContext(zmq::context_t * zmq_context, FileOptions opt, _int64 size)
	{
		context = zmq_context;
		options = opt;
		file_size = size;
		blocks = (size / BLOCK_SIZE) + (size % BLOCK_SIZE == 0 ? 0 : 1);
		batch_count = blocks / BATCH_SIZE + (blocks % BATCH_SIZE == 0 ? 0 : 1);
	}
};

void FileBuffer::Close()
{
	socket->close();
	feedback->close();
	buffer_return->close();
	for (int k = 0; k < file_count; k++)
	{
		if (filenames[k] != nullptr) free (filenames[k]);
	}
	if (filenames != nullptr) free (filenames);

}

void FileBuffer::ReturnBuffer(char* buffer)
{
	ZMQ_SAFECALL("ReturnBuffer.BufferReturn.Send[Buffer]", buffer_return->send(&buffer, sizeof(char*)));
}

_int64 FileBuffer::Initialise(FileOptions options)
{
	file_count = options.file_count;
	filenames = options.file_names;

	socket = new zmq::socket_t(*context, ZMQ_PAIR);
	feedback = new zmq::socket_t(*context, ZMQ_PAIR);
	buffer_return = new zmq::socket_t(*context, ZMQ_PAIR);

	ZMQ_SAFECALL("Initialise.Socket.Bind", socket->bind("inproc://buffer"));
	ZMQ_SAFECALL("Initialise.Feedback.Bind", feedback->bind("inproc://feedback"));
	ZMQ_SAFECALL("Initialise.BufferReturn.Bind", buffer_return->bind("inproc://file_buffer_return"));

	FILE * file;
	errno_t error = fopen_s(&file, filenames[0], "rb"); //open file
	if (error != 0) { POST_ERROR("FileBuffer Constructor", "File could not be opened."); }
		
	_fseeki64(file, 0, SEEK_END);
	_int64 size = _ftelli64(file);

	fclose(file);
	
	for (int k = 1; k < file_count; k++)
	{
		errno_t error = fopen_s(&file, filenames[k], "rb"); //open file
		if (error != 0) { POST_ERROR("FileBuffer Constructor", "Mirror file could not be opened."); }
		
		_fseeki64(file, 0, SEEK_END);
		_int64 tmp = _ftelli64(file);

		if (tmp != size) 
		{
			POST_ERROR("FileBuffer Constructor", "Size of file %d does not match size of first file.\n\n");
			exit(1);
		}
		fclose(file);
	}
	
	BufferContext * args = new BufferContext(context, options, size);

	blocks = args->blocks;


	zmq::message_t msg;
	tthread::thread * process;
	if (file_count == 1) 
	{
		process = new tthread::thread(ReadSingleFile, args);
		buffer_count = 3;
	}
	else
	{
		process = new tthread::thread(Distributer, args);
		buffer_count = BUFFERS_PER_FILE * file_count;
	}

	for (int k = 0; k < buffer_count; k++)
	{
		char* buffer = (char*) malloc(BLOCK_SIZE);
		buffer_return->send(&buffer, sizeof(char*));
	}

	ZMQ_SAFECALL("Initialise.Socket.Receive[SinkReady]", socket->recv(&msg)); //receive signal from sink
	if (file_count > 1) ZMQ_SAFECALL("Initialise.Feedback.Send[StartSignal]", feedback->send(msg)); //signal gatherer that sink is ready

	return size;
}


void FileBuffer::Next(char*& data, size_t& length)
{
	if (curr_block != blocks)
	{
		DataChunk chunk;
		ZMQ_SAFECALL("Next.Socket.Receive[Chunk]", socket->recv(&chunk, sizeof(DataChunk)));
	
		data = chunk.buffer;
		length = chunk.size;

		curr_block++;
	}
	else
	{
		data = nullptr;
		length = 0;
	}
}


void Distributer(void *arg)
{
	BufferContext * args = static_cast<BufferContext*>(arg);

	FileOptions file_opt = args->options;
	zmq::context_t * context = args->context;
	_int64 file_size = args->file_size;

	int file_count = file_opt.file_count;
	char** file_names = file_opt.file_names;
	int blocks = args->blocks;
	int batch_count = args->batch_count;
	zmq::message_t signal;

	zmq::socket_t ** channels = (zmq::socket_t**) malloc(file_count * sizeof(zmq::socket_t*)); 
	zmq::pollitem_t * items = (zmq::pollitem_t*) malloc(sizeof(zmq::pollitem_t) * file_count);
	
	char * str = (char*) malloc(32);

	for (int k = 0; k < file_count; k++)
	{
		channels[k] = new zmq::socket_t(*context, ZMQ_PAIR);
		sprintf(str,"inproc://dist%d\0",k);
		channels[k]->bind(str);

		items[k].socket = *channels[k];
		items[k].fd = 0;
		items[k].events = ZMQ_POLLIN;
		items[k].revents = 0;
	}
	free(str);

	zmq::socket_t feedback(*context, ZMQ_PAIR);
	zmq::socket_t empty(*context, ZMQ_PAIR);
	ZMQ_SAFECALL("Distributer.Feedback.Connect", feedback.connect("inproc://feedback"));
	ZMQ_SAFECALL("Distributer.Empty.Connect", empty.connect("inproc://file_buffer_return"));
	
	tthread::thread* sink_thread = new tthread::thread(Collector, arg);

	ZMQ_SAFECALL("Distributer.Feedback.Receive[StartSignal]", feedback.recv(&signal)); //receive signal that everything is ready

	for (int k = 0; k < file_count; k++)
	{
		WorkerContext * setup = new WorkerContext(k, context, file_names[k], blocks);
		tthread::thread * worker_thread= new tthread::thread(Worker, setup);
	}

	int curr = 0;
	while(curr < blocks)
	{
		zmq::poll(&items[0], file_count, -1);

		for (int k = 0; k < file_count; k++)
		{
			if (items[k].revents & ZMQ_POLLIN)
			{
				int count = curr + BATCH_SIZE >= blocks ? blocks - curr : BATCH_SIZE;
				ZMQ_SAFECALL("Distributer.Publisher.Receive[Ready]", channels[k]->recv(&signal));

				ZMQ_SAFECALL("Distributer.Publisher.Send[SequenceNo]", channels[k]->send(&curr, sizeof(int)));
				ZMQ_SAFECALL("Distributer.Publisher.Send[SequenceNo]", channels[k]->send(&count, sizeof(int)));

				for (int j = 0; j < count; j++)
				{
					char* ptr;
					empty.recv(&ptr, sizeof(char*));
					channels[k]->send(&ptr, sizeof(char*));
				}
				//send all buffers to fill
				curr+=BATCH_SIZE;
			}
		}
	}

	//kill threads
	for (int k = 0; k < file_count; k++)
	{
		int end = -1;
		ZMQ_SAFECALL("Distributer.Publisher.Receive[Ready]", channels[k]->recv(&signal));
		
		ZMQ_SAFECALL("Distributer.Publisher.Send[End]", channels[k]->send(&end, sizeof(int)));
	}
	

	for (int k = 0; k < BUFFERS_PER_FILE * file_count; k++)
	{
		char * tmp;
		empty.recv(&tmp, sizeof(char*));
		free(tmp);
	}
	empty.close();
	feedback.close();
	
	for (int k = 0; k < file_count; k++)
	{
		channels[k]->close();
	}
}

void Worker (void *arg) 
{
	WorkerContext * state = static_cast<WorkerContext*>(arg);

	zmq::context_t * context = state->context;
	char* filename = state->filename;
	int total_blocks = state->blocks;
	int id = state->id;
//	
//	delete state;
	//  Bind to inproc: endpoint, then start upstream thread
	zmq::socket_t distributer (*context, ZMQ_PAIR);   
	zmq::socket_t sender (*context, ZMQ_PAIR);

	char* str = (char*) malloc(32);
	sprintf(str,"inproc://dist%d\0",id);
	distributer.connect(str);
	
	sprintf(str,"inproc://coll%d\0",id);
	sender.connect(str);
	
	free(str);

	////signal setup complete
	zmq::message_t signal;
	sender.send(signal);
	//state file handels for reading
	FILE * file;
	errno_t error = fopen_s(&file, filename, "rb"); //open file
	if (error != 0) { POST_ERROR("Worker Thread", "File could not be opened."); }

	//job loop
	while(true)
	{
		ZMQ_SAFECALL("Worker.Distributer.Send[Ready]", distributer.send(signal)); //send request
		int seq, count; //sequence number to load 
		//distributer.recv(&tmp, sizeof(int));
		ZMQ_SAFECALL("Worker.Distributer.Receive[SequenceNo]", distributer.recv(&seq, sizeof(int))); //receive seq
		
		if (seq == -1) break; // end of jobs
		
		ZMQ_SAFECALL("Worker.Distributer.Receive[SequenceNo]", distributer.recv(&count, sizeof(int))); //receive seq
		//receive buffer

		//only seek once per batch
		_fseeki64(file, (_int64)(seq) * BLOCK_SIZE, SEEK_SET); //position file pointer to the start of the job
		
		for (int k = 0; k < count; k++)
		{

			char* buffer;
			ZMQ_SAFECALL("Worker.Distributer.Receive[Buffer]", distributer.recv(&buffer, sizeof(char*)));


			size_t total_bytes_read = 0;
			for (int j = 0; j < READS_PER_BLOCK; j++)
			{
				size_t bytes_read = fread(buffer + j * READ_WIDTH, sizeof(char), READ_WIDTH, file);
				total_bytes_read += bytes_read;

				if (bytes_read != READ_WIDTH)
				{
					//eof or err?
					if (feof(file) && seq + k + 1 < total_blocks) //not the last block
					{
						POST_ERROR("Worker Thread", "EOF reached before jobs complete");
					}
					else if (ferror(file)) //handle
					{
						POST_ERROR("Worker Thread", "File read error"); 
						
					}
					else 
					{
						break; //eof - send chunk
					}
				}
			}
			
			DataChunk chunk(buffer, total_bytes_read, seq + k);
			//sender.send(&id, sizeof(int), ZMQ_SNDMORE);
			ZMQ_SAFECALL("Worker.Sender.Send[Chunk]",sender.send(&chunk, sizeof(DataChunk)));
		}
	}
	distributer.close();
	sender.close();
	fclose(file);
}


void Collector(void* arg)
{
	BufferContext * args = static_cast<BufferContext*>(arg);

	zmq::context_t * context = args->context;
	int block_count = args->blocks;
	int file_count = args->options.file_count;
	int curr = 0;
	zmq::message_t signal;
	std::unordered_map<int, DataChunk> chunk_map(10000);

	/*zmq::pollitem_t items []= {	
		{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},
		{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0},{nullptr, 0, ZMQ_POLLIN, 0}};*/
	zmq::pollitem_t * items = (zmq::pollitem_t*) malloc(sizeof(zmq::pollitem_t) * file_count);
	zmq::socket_t ** channels = (zmq::socket_t**) malloc(sizeof(zmq::socket_t*) * file_count);

	char* str = (char*)malloc(32);
	for (int k = 0; k < file_count; k++)
	{
		channels[k] = new zmq::socket_t(*context, ZMQ_PAIR);
		sprintf(str,"inproc://coll%d\0",k);
		channels[k]->bind(str);

		items[k].socket = *channels[k];
		items[k].fd = 0;
		items[k].events = ZMQ_POLLIN;
		items[k].revents = 0;
	}
	free(str);

	zmq::socket_t buffer (*context, ZMQ_PAIR);

	ZMQ_SAFECALL("Collector.Buffer.Connect",buffer.connect("inproc://buffer"));
	
	ZMQ_SAFECALL("Collector.Buffer.Send[SinkReady]", buffer.send(signal)); //signal buffer to indicate sink ready

	for (int k = 0; k < file_count; k++)
	{
		channels[k]->recv(&signal);
	}

	while (curr < block_count)
	{

		//first check hashmap for chunk
		if (chunk_map.count(curr) > 0)
		{
			//chunk is already in hashmap - remove and send to buffer
			DataChunk chunk = chunk_map[curr];
			chunk_map.erase(curr);
			ZMQ_SAFECALL("Collector.Buffer.Send[Chunk]", buffer.send(&chunk, sizeof(DataChunk)));
			curr++;
		}
		else //keep receiving and storing chunks until the correct chunk arrives
		{
			//pull next chunk
			DataChunk chunk;
			zmq::poll(&items[0], file_count, -1);
			//while(1);
			for (int k = 0; k < file_count; k++)
			{
				if (items[k].revents & ZMQ_POLLIN)
				{
					ZMQ_SAFECALL("Collector.Collector.Receive[Chunk]", channels[k]->recv(&chunk, sizeof(DataChunk)));

					if (chunk.seq == curr)
					{
						ZMQ_SAFECALL("Collector.Buffer.Send[Chunk]", buffer.send(&chunk, sizeof(DataChunk)));
						curr++;
					}
					else
					{
						//add data chunk into hash table
						chunk_map.insert(std::make_pair(chunk.seq, chunk));
					}
				}
			}
		}
	}
	
	
	for (int k = 0; k < file_count; k++) channels[k]->close();
	buffer.close();
}

void ReadSingleFile(void *arg)
{
	BufferContext * args = static_cast<BufferContext*>(arg);

	zmq::context_t * context = args->context;
	//int thread_count = args->options.file_count;
	_int64 file_size = args->file_size;
	int total_blocks = args->blocks;
	zmq::message_t signal;

	zmq::socket_t buffer (*context, ZMQ_PAIR);
	buffer.connect("inproc://buffer");
	
	zmq::socket_t empty(*context, ZMQ_PAIR);
	ZMQ_SAFECALL("ReadSingleFile.Empty.Connect", empty.connect("inproc://file_buffer_return"));


	ZMQ_SAFECALL("Collector.Buffer.Send[SinkReady]", buffer.send(signal)); //signal buffer to indicate sink ready
	FILE * file;
	errno_t error = fopen_s(&file, args->options.file_names[0], "rb"); //open file
	if (error != 0) { POST_ERROR("Single Thread", "File could not be opened."); }	
	
	for (int k = 0; k < total_blocks; k++)
	{
		char* data;
		empty.recv(&data, sizeof(char*));
		size_t bytes_read = fread(data, sizeof(char), BLOCK_SIZE, file);
		if (bytes_read != BLOCK_SIZE && k + 1 != total_blocks)
		{
			if (feof(file))
			{
				POST_ERROR("Single Thread", "Early EOF"); 
			}
			if (ferror(file)) //handle
			{
				POST_ERROR("Single Thread", "File read error"); 
			}
		}
				
		DataChunk chunk(data, bytes_read, k);
		buffer.send(&chunk, sizeof(DataChunk));
	}

	for (int k = 0; k < 5; k++)
	{
		char * tmp;
		empty.recv(&tmp, sizeof(char*));
		free(tmp);
	}
	empty.close();
	buffer.close();
	fclose(file);

}

//Fakes
//
//void FakeWorker (void *arg) 
//{
//	WorkerContext * state = static_cast<WorkerContext*>(arg);
//
//	zmq::context_t * context = state->context;
//	char* filename = state->filename;
//	int total_blocks = state->blocks;
//	int id = state->id;
////	
////	delete state;
//	//  Bind to inproc: endpoint, then start upstream thread
//	zmq::socket_t distributer (*context, ZMQ_PAIR);   
//	zmq::socket_t sender (*context, ZMQ_PAIR);
//
//	char* str = (char*) malloc(32);
//	sprintf(str,"inproc://dist%d\0",id);
//	distributer.connect(str);
//	
//	sprintf(str,"inproc://coll%d\0",id);
//	sender.connect(str);
//	
//	free(str);
//
//	////signal setup complete
//	zmq::message_t signal;
//	sender.send(signal);
//
//	//job loop
//	while(true)
//	{
//		ZMQ_SAFECALL("Worker.Distributer.Send[Ready]", distributer.send(signal)); //send request
//		int seq, count; //sequence number to load 
//		//distributer.recv(&tmp, sizeof(int));
//		ZMQ_SAFECALL("Worker.Distributer.Receive[SequenceNo]", distributer.recv(&seq, sizeof(int))); //receive seq
//		
//		if (seq == -1) break; // end of jobs
//		
//		ZMQ_SAFECALL("Worker.Distributer.Receive[SequenceNo]", distributer.recv(&count, sizeof(int))); //receive seq
//		//only seek once per batch
//		
//		
//		for (int k = 0; k < count; k++)
//		{
//
//			char* buffer = (char*) malloc(BLOCK_SIZE);
//			
//			
//			DataChunk chunk(buffer, BLOCK_SIZE, seq + k);
//			//sender.send(&id, sizeof(int), ZMQ_SNDMORE);
//			ZMQ_SAFECALL("Worker.Sender.Send[Chunk]",sender.send(&chunk, sizeof(DataChunk)));
//			
//		}
//	}
//	distributer.close();
//	sender.close();
//}
//
//void FakeReadSingleFileLB(void *arg)
//{
//	BufferContext * args = static_cast<BufferContext*>(arg);
//
//	zmq::context_t * context = args->context;
//	int thread_count = args->options.file_count;
//	_int64 file_size = args->file_size;
//	int total_blocks = args->blocks;
//	zmq::message_t signal;
//
//	zmq::socket_t buffer (*context, ZMQ_PAIR);
//	buffer.connect("inproc://buffer");
//	
//	ZMQ_SAFECALL("Collector.Buffer.Send[SinkReady]", buffer.send(signal)); //signal buffer to indicate sink ready
//
//	for (int k = 0; k < total_blocks; k++)
//	{
//		char * data = (char*) malloc(BLOCK_SIZE);
//				
//		DataChunk chunk(data, BLOCK_SIZE, k);
//		buffer.send(&chunk, sizeof(DataChunk));
//	}
//	buffer.close();
//
//}
