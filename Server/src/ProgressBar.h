#pragma once
#include <ctime>
#include "zmq.hpp"

class ProgressBar
{
	unsigned char bar[51];
	_int64 cache[10];
	clock_t times[10];

	int idx;
	int space;

	_int64 total;
	_int64 curr;
	clock_t start, last_update;

	zmq::socket_t * server;

public:
	ProgressBar(zmq::context_t * context, _int64 total_elements);

	void Update(_int64 increment);
	void Finished();
};