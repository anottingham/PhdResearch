#pragma once
#include "zmq.hpp"


class GpfVm
{
	unsigned int cache[4];		 //16 byte pachet cache
	uchar4 thread_state; //stores thread state
	uchar4 transform;	 //cache state variables

	int packet_index;
	
	short2 offsets;		//stores the offsets of the program pointer and data shift register
	uchar4 proto_state;	//stores the primary state of the active protocol - id, data span and payload
		
	__device__ void ProcessPacket();
	__device__ void ProcessSet();
	__device__ void CacheLoad(const int*  __restrict__ start_offset);
	__device__ unsigned int ExtractField();

public:
	__device__ GpfVm(int stream);
	 
	__device__ void Gather();
	__device__ void Filter();

};

void GpfVmLauncher(zmq::context_t * zmq_context, FilterOptions filter_options);


class GpfTimer
{
private:
	char* filename;
	int streams;
	int gpu;
	char* buffer;
	FILE* file;

public:
	GpfTimer(char* output_file, FilterOptions* options);
	~GpfTimer();
	void Record(int packetCount, int stream, float issueTime, float packetCopy, float packetProcess, float resultCopy);

};