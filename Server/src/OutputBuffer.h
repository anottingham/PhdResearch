#pragma once

#include "zmq.hpp"
#include "VmCommon.h"

class OutputBuffer
{
private:
	char * filter_results;
	char * integer_results;

	zmq::context_t * context;
	zmq::socket_t * socket;

	zmq::socket_t * field_return;
	zmq::socket_t * filter_return;

	FilterOptions options;

public:
	OutputBuffer(ProcessArgs args);
	~OutputBuffer();

	int* GetFieldBuffer();
	int* GetFilterBuffer();

	void CopyFilterResults(int * results, size_t size, int packets);
	void CopyIntegerResults(int * results, size_t size, int packets);
	//void CopyIntegerResults(int * results, size_t size);

	void Finished(_int64 packet_count);
};

