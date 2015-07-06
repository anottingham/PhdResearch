#pragma once
#include "zmq.hpp"

struct GpfUtilSetup
{
	zmq::context_t context;
	int port;
};

void GpfViewUtil(zmq::context_t& context, int port);