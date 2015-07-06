#pragma once
#include "zmq.hpp"
//#include "tinythread.h"

//user specified
#define READ_WIDTH_KB (64)	//must be a factor of block size
#define BLOCK_SIZE_MB (5)
#define BATCH_SIZE_MB (50) //must be a multiple block size

struct FileOptions
{
	int file_count;
	char** file_names;


	FileOptions() : file_count(0), file_names(nullptr) {}
};


class FileBuffer
{
private:
	char** filenames;
	int file_count;
	int blocks;
	int curr_block;
	zmq::context_t * context;
	zmq::socket_t * socket;
	zmq::socket_t * feedback;

public:
	FileBuffer(zmq::context_t* zmq_context) : filenames(nullptr), file_count(0), blocks(0), curr_block(0) { context = zmq_context; }
	~FileBuffer();

	_int64 Initialise(FileOptions options);
	void Next(char*& data, size_t& length);
	void FakeNext(char*& data, size_t& length, bool& free);
};


