#pragma once
#include "zmq.hpp"

struct DistillerOptions
{
	char * capture_name;
	char * index_file;
	char * filter_file;
	char * out_name;

	bool filter;
	bool crop;

	int crop_len;
	int task_count;
	_int64 total_data;

	zmq::context_t* context;

	DistillerOptions() : capture_name(nullptr), index_file(nullptr), filter_file(nullptr), out_name(nullptr), filter(false), crop(false), crop_len(0), task_count(0), total_data(0), context(nullptr) {}
};


struct DistillTask
{
	char * buffer;
	size_t size;

	_int64 byte_start;
	_int64 index_start;
	int index_count;
	int byte_count;

	DistillTask() : byte_start(0), byte_count(0), index_start(0), index_count(0), buffer(nullptr), size(0) {}
};


void Distiller(zmq::context_t& context, zmq::socket_t& client_socket);
