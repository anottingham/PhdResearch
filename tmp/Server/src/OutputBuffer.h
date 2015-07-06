#pragma once

#include "zmq.hpp"
#include "VmCommon.h"

class OutputBuffer
{
	char * filter_results;
	char * integer_results;

	zmq::context_t * context;
	zmq::socket_t * socket;

	FilterOptions options;

public:
	OutputBuffer(ProcessArgs args);
	~OutputBuffer();

	void CopyFilterResults(char * results, size_t size);
	//void CopyIntegerResults(int * results, size_t size);

	void Finished();
};

