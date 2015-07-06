#include "IndexBuffer.h"
#include "tinythread.h"

#define BLOCK_SIZE (512 * 1024)

struct IndexerChunk
{
	_int64 * buffer;
	int count;
	int type;
	bool more;

	IndexerChunk() : type(-1), count(0), buffer(nullptr), more(true) {}
};

struct IndexWriterArgs
{
	zmq::context_t * context;
	IndexOptions options;

	IndexWriterArgs(zmq::context_t* zmq_context, IndexOptions opt)
	{
		context = zmq_context;
		options = opt;
	}
};


void IndexWriter(void* arg)
{
	
	zmq::context_t * context = static_cast<IndexWriterArgs*>(arg)->context;
	IndexOptions index = static_cast<IndexWriterArgs*>(arg)->options;
	if (!index.enabled) return;
	FILE* pidx; 
	FILE* tidx; 
	
	errno_t error = fopen_s(&pidx, index.pidx_file, "wb");
	if (error != 0) { POST_ERROR("IndexWriter Thread", "PIDX File could not be opened."); }
	
	error = fopen_s(&tidx, index.tidx_file, "wb");
	if (error != 0) { POST_ERROR("IndexWriter Thread", "TIDX File could not be opened."); }

	zmq::socket_t socket(*context, ZMQ_PAIR);
	zmq::socket_t complete(*context, ZMQ_PAIR);
	socket.connect("inproc://indexer");
	complete.connect("inproc://complete_index");
	

	_int64 zero = 0;

	fwrite(&zero, sizeof(_int64), 1, pidx);	//record count
	fwrite(&zero, sizeof(_int64), 2, tidx);//start-time & duration

	IndexerChunk chunk;

	do
	{
		socket.recv(&chunk, sizeof(IndexerChunk));

		if (chunk.count > 0)
		{
			FILE* file = chunk.type == 0 ? pidx : tidx; 
			fwrite(chunk.buffer, sizeof(_int64),  chunk.count, file);
		}

		if (chunk.buffer != nullptr) {
			free(chunk.buffer);	//free the buffer
		}
		

	} while (chunk.more);

	_int64 tmp;

	fseek(pidx, 0, SEEK_SET);
	fseek(tidx, 0, SEEK_SET);

	//write packet count
	socket.recv(&tmp, sizeof(_int64));
	fwrite(&tmp, sizeof(_int64), 1, pidx);

	//write start time
	socket.recv(&tmp, sizeof(_int64));
	fwrite(&tmp, sizeof(_int64), 1, tidx);
	
	//write duration
	socket.recv(&tmp, sizeof(_int64));
	fwrite(&tmp, sizeof(_int64), 1, tidx);
	
	fclose(pidx);
	fclose(tidx);
	
	int msg = 0;
	complete.send(&msg, sizeof(int));
	complete.close();
	socket.close();
}

IndexBuffer::IndexBuffer(zmq::context_t * zmq_context, IndexOptions options)
{
	context = zmq_context;
	socket = new zmq::socket_t(*zmq_context, ZMQ_PAIR);
	records_per_buffer = BLOCK_SIZE/sizeof(_int64);
	index_buffer = (_int64*)malloc(BLOCK_SIZE);
	time_buffer = (_int64*)malloc(BLOCK_SIZE);
		
	curr_index_record = 0;
	total_index_records = 0;
		
	start_time = 0;
	current_time = 0;
	curr_time_record = 0;
	total_time_records = 0;
	last_time = 0;

	socket = new zmq::socket_t(*context, ZMQ_PAIR);
	socket->bind("inproc://indexer");

	IndexWriterArgs * args = new IndexWriterArgs(context, options);

	tthread::thread* writer = new tthread::thread(IndexWriter, args);
}

void IndexBuffer::CopyIndexData(_int64 packet_index, unsigned int time_sec)
{
	memcpy(index_buffer + curr_index_record++, &packet_index, sizeof(_int64));
	if (curr_index_record == records_per_buffer)
	{

		IndexerChunk chunk;
		chunk.type = 0;
		chunk.buffer = index_buffer;
		chunk.count = records_per_buffer;
		chunk.more = true;
		socket->send(&chunk, sizeof(IndexerChunk));

		index_buffer = (_int64*)malloc(BLOCK_SIZE);
		curr_index_record = 0;
	}

	while(current_time < time_sec)
	{
		memcpy(time_buffer + curr_time_record++, &total_index_records, sizeof(_int64));
		if (curr_time_record == records_per_buffer)
		{

			IndexerChunk chunk;
			chunk.type = 1;
			chunk.buffer = time_buffer;
			chunk.count = records_per_buffer;
			chunk.more = true;
			socket->send(&chunk, sizeof(IndexerChunk));
				
			time_buffer = (_int64*)malloc(BLOCK_SIZE);
			curr_time_record = 0;
		}
		current_time++;
		total_time_records++;
	}
	last_time = time_sec;
	total_index_records++;
		
}

//must be written before first index is written
void IndexBuffer::SetStartTime(unsigned int start_sec)
{
	start_time = 0 + start_sec;
	current_time = start_sec - 1;
}

void IndexBuffer::Finished(_int64 eof_index)
{		
	//add terminaotr records to indexes so that last values can be read - dont need to increment totals though
	/*memcpy(index_buffer + curr_index_record++, &eof_index, sizeof(_int64));
	memcpy(time_buffer + curr_time_record++, &total_index_records, sizeof(_int64));*/
/*
	CopyIndexData(eof_index, last_time + 1);*/

	IndexerChunk chunk;

	chunk.buffer = index_buffer;
	chunk.type = 0;
	chunk.count = curr_index_record;
	chunk.more = true;
	socket->send(&chunk, sizeof(IndexerChunk));
		
	chunk.buffer = time_buffer;
	chunk.type = 1;
	chunk.count = curr_time_record;
	chunk.more = false;
	socket->send(&chunk, sizeof(IndexerChunk));

	socket->send(&total_index_records, sizeof(_int64));
	socket->send(&start_time,sizeof(_int64));
	socket->send(&total_time_records, sizeof(_int64));

	socket->close(); //o
}