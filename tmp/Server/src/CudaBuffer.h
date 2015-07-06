#pragma once
#include "zmq.hpp"
#include "VmCommon.h"


class CudaBuffer
{
	zmq::context_t * context;
	zmq::socket_t * socket;

	char * cuda_buffer;
	size_t buffer_size;
	int curr_record;
	int record_length;
	int record_start;
	int packets_per_buffer;
	_int64 total_packets;

public:
	CudaBuffer(zmq::context_t * zmq_context, FilterOptions options);
	~CudaBuffer();
	void CopyPacket(char* buffer, int length);
	void Finished();
};