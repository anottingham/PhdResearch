#pragma once
#include "zmq.hpp"
#include "Processor.h"

struct ProcessOptions;

class GpfServer
{
	zmq::context_t context;

	void ClientConnect(zmq::socket_t& socket);
	void ClientProcess(zmq::socket_t& socket);

	FileOptions GetFileOptions(zmq::socket_t& socket);
	FilterOptions GetFilterOptions(zmq::socket_t& socket);
	IndexOptions GetIndexOptions(zmq::socket_t& socket);
	void Poll(zmq::socket_t& socket, ProcessOptions options);

public:
	GpfServer() : context(1) {}

	void Start(int port = 5555);
};