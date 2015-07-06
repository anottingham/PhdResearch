#pragma once

#include "zmq.hpp"

struct FilterOptions
{
	bool enabled;
	bool copy_integers;
	bool compress_results;
	char* capture_name;
	char* out_folder;			//folder to write filter results to - must be created externally

	int record_start;			//byte start offset of record in packet data
	int record_length;			//byte length of record in packet data
	
	int packets_per_buffer;		//number of records contained in each buffer - should be a power of 2

	int filters_per_packet;		//gross filters per packet record (not all are executed)
	int integers_per_packet;	//gross number of integer values copied from each packet (not all are necessarily used in each packet)

	int streams;				//number of streams to divide each buffer over
	int threads;				//number of threads per thread block

	char ** filter_names;
	char ** integer_names;


	FilterOptions() : 
		enabled					(false),
		copy_integers			(false),
		compress_results		(false), 

		out_folder				(nullptr), 
		filter_names			(nullptr),
		integer_names			(nullptr),
		capture_name			(nullptr), 

		record_start			(0), 
		record_length			(0), 
		packets_per_buffer		(0),
		filters_per_packet		(0),
		integers_per_packet		(0),
		streams					(4),
		threads					(256)
	{}

	size_t packet_buffer_size() { return packets_per_buffer * record_length; }

	size_t filter_memory_size() { return (packets_per_buffer / 8) * filters_per_packet; } //1-bit per filter

	size_t integer_memory_size() { return packets_per_buffer * integers_per_packet; }

	bool filter_results() { return filters_per_packet > 0; }
	bool integer_results() { return integers_per_packet > 0; }
	int packets_per_stream() { return packets_per_buffer / streams; }

	int packet_buffer_stream_size(size_t& buffer_size, int& stream) 
	{ 
		if (buffer_size == packet_buffer_size()) return buffer_size / streams; 
		else
		{
			int tmp = packets_per_stream() * stream;
			if (tmp > buffer_size) return 0;
			if (tmp + packets_per_stream() < buffer_size) return packet_buffer_size() / streams;
			else return buffer_size % packets_per_stream();

		}
	}

	

	int blocks() { return packets_per_stream() / threads; }

};

//generic structure for passing buffer pointers
struct CudaBufferPointer
{
	char* buffer;
	size_t size;
	bool more;

	CudaBufferPointer() : buffer(nullptr), size(0), more(true) {}
};

struct ProcessArgs
{
	zmq::context_t * context;
	FilterOptions options;
};