#pragma once
#include "zmq.hpp"
#include "Common.h"


struct IndexOptions
{
	bool enabled;
	char* pidx_file;
	char* tidx_file;

	IndexOptions() : enabled(false), pidx_file(nullptr), tidx_file(nullptr) {}
};

class IndexBuffer
{
	zmq::context_t * context;
	zmq::socket_t * socket;

	_int64* index_buffer;
	_int64* time_buffer;
	int records_per_buffer;

	int curr_index_record;
	_int64 total_index_records;
	
	unsigned int current_time;
	unsigned int curr_time_record;
	_int64 total_time_records;
	_int64 start_time;
	unsigned int last_time;

public:
	IndexBuffer(zmq::context_t * zmq_context, IndexOptions options);

	void CopyIndexData(_int64 packet_index, unsigned int time_sec);

	void SetStartTime(unsigned int start_sec);

	void Finished();
};
