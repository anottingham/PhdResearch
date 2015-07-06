#pragma once
#include "zmq.hpp"
#include "VmCommon.h"
#include <queue>



class CudaBuffer
{
	zmq::context_t * context;
	zmq::socket_t * socket;
	zmq::socket_t * ready;
	
	char * cuda_buffer;
	size_t buffer_size;
	int curr_record;
	_int64 total_packets;
	int record_length;
	int record_start;
	int packets_per_buffer;
	int total_buffers;

public:
	CudaBuffer(zmq::context_t * zmq_context, FilterOptions options);
	void Close();

	void Initialise();
	void CopyPacket(char* buffer, int length);
	void Finished();
};