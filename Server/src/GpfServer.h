#pragma once
#include "zmq.hpp"
#include "Processor.h"

struct ProcessOptions;

class GpfServer
{
	zmq::context_t context;

	void ClientConnect(zmq::socket_t& socket);
	void ClientProcess(zmq::socket_t& socket, zmq::socket_t& complete_filter, zmq::socket_t& complete_index);

	FileOptions GetFileOptions(zmq::socket_t& socket);
	FilterOptions GetFilterOptions(zmq::socket_t& socket);
	IndexOptions GetIndexOptions(zmq::socket_t& socket);
	void StartProcess(zmq::socket_t& socket, ProcessOptions options);

public:
	GpfServer() : context(1) {}

	void Start(int port = 5555);
	
};