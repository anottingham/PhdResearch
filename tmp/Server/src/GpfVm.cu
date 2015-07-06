#pragma once

#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "VmCommon.h"
#include <stdio.h>
#include "zmq.hpp"

#include "tinythread.h"
#include "OutputBuffer.h"

__constant__ void * packet_data;
__constant__ void * filter_mem;
//__constant__ void * integer_mem;
//__constant__ void * bool_mem;

__constant__ unsigned char program[16 * 1024];
__constant__ unsigned int value_lookup[4 * 1024];
__constant__ short record_size;
__constant__ int packet_count;
__constant__ short filter_count;
__constant__ int filter_size;

// _cache_data holds instructions for caching packet data. It contains two adjacent arrays of operation type and unshifted start offset.
// It provides for up to 16 segments (each covering up to 16 consecutive bytes) which need not be directly adjacent -> support 256 bytes per packet header (excluding optional fields)
// Arrays accessed with SEGOP and SEGX cache functions
__constant__ char _cache_data[32]; //[operation * 16 | offset * 16] 
__constant__ unsigned char _layers; //number of layers

//
//
//#pragma region Cache
//
////stores the start offset of each cache chunk
//
//#define CACHE_LANE		(state.x)
//#define CACHE_SEGOP(id)	(_cache_data[id])		//cache segment load operation (0-4)
//#define CACHE_SEGX(id)	(_cache_data[16 + id])	//unshifted offset in memory
//
//class GpfCache
//{
//	int cache[4];		 //16 byte pachet cache
//	uchar4 filter_cache; //stores upto 32 filter results concurrently and houses cache metadata
//	uchar4 state;		 //cache state variables
//
//	int first_packet;
//
//	int shift;		//only used during caching - extraction is already shifted. 
//					//Some dynamic shift is still possible inside segment, controlled by the dynamic shift
//	
//	__host__ __device__ __forceinline__ int switchByteOrder(uchar4 value)
//	{
//		return (value.x << 24) + (value.y << 16) + (value.z << 8) + value.w;
//	}
//
//	__host__ __device__ void cacheLoad4B(int segment_offset)
//	{
//		int working;
//		//load each packet
//		if (__any(shift))
//		{
//			cache[0] = __ldg(packets + segment_offset + __shfl(shift, 0, 4) + CACHE_LANE);
//			cache[1] = __ldg(packets + segment_offset + recortd_size + __shfl(shift, 1, 4) + CACHE_LANE);
//			cache[2] = __ldg(packets + segment_offset + recortd_size * 2 + __shfl(shift, 2, 4) + CACHE_LANE);
//			cache[3] = __ldg(packets + segment_offset + recortd_size * 3 + __shfl(shift, 3, 4) + CACHE_LANE);
//		}
//		else
//		{
//			cache[0] = __ldg(packets + segment_offset + CACHE_LANE);
//			cache[1] = __ldg(packets + segment_offset + recortd_size + CACHE_LANE);
//			cache[2] = __ldg(packets + segment_offset + recortd_size * 2 + CACHE_LANE);
//			cache[3] = __ldg(packets + segment_offset + recortd_size * 3 + CACHE_LANE);
//		}
//		
//		// 1,0,3,2
//		working = switchByteOrder((uchar4)cache[state.x]);
//		cache[state.x] = __shfl(working, static_cast<int>(state.x), 4);
//
//		// 3,2,1,0		
//		working = switchByteOrder((uchar4)cache[state.y]);
//		cache[state.y] = __shfl(working, static_cast<int>(state.y), 4);
//	
//		// 2,3,1,0
//		working = switchByteOrder((uchar4)cache[state.z]);
//		cache[state.z] = __shfl(working, static_cast<int>(state.z), 4);
//	}
//	
//public:
//	__host__ __device__ GpfCache()
//	{
//		//use shift as a temporary register
//		shift = threadIdx.x & 0x3;		//integer, as shift needs to be shuffled often
//
//		filter_cache.x = 0;							//filter results (32 1-bit boolean values)
//		filter_cache.y = threadIdx.x & 31;			//thread lane in warp
//		filter_cache.z = 0;							//filter segments processed (each corresponds to 8 consecutive filters
//		filter_cache.w = 3 - (filter_cache.y >> 3);	//subwarp filter transform
//
//		state.w = shift;				//thread lane
//		state.x = (5 - shift) & 0x3;	//first transform
//		state.y = 3 - shift;			//second transform
//		state.z = (2 + shift) & 0x3;	//third transform
//		shift = 0;
//	}
//
//	__host__ __device__ __forceinline__ void	resetShift()	{	shift = 0;	}
//	__host__ __device__ __forceinline__ int		getShift()		{	return shift;	}
//
//	__host__ __device__ __forceinline__ void	closeSegment(int segment_shift)	{	shift += segment_shift;	} //adjusts the base shift to incorporate shift introduced in the previous protocol
//
//	// arguments - int operation | int chunk_number (0-15)
//	__host__ __device__ void cacheFill(int segment_id)
//	{
//		int packetIdx = (blockDim.x * blockIdx.x + threadIdx.x);
//		switch(CACHE_SEGOP(segment_id))
//		{
//		case 0:
//		case 1:	
//		case 2: 
//		case 3:
//			cache[program[offsets.x]] = switchByteOrder((uchar4)(
//				__ldg(packets + packet_index * record_size + CACHE_SEGX(segment_id) + shift + CACHE_SEGOP(segment_id))
//				));
//			break;
//		case 4:					//	111...1100 -> pIdx - (pIdx % 4)
//			cacheLoad4B(packetIdx & 0xFFFFFFFC * record_size + CACHE_SEGX(segment_id));
//			break;
//		}
//	}
//	
//	//extracts a field from the cache
//	//bit_offset - the bit offset of the field from the beginning of the segment, including any local shift
//	//bit_length - the bit length of the field
//	//returns - the field
//	__host__ __device__ int extractField(const short2& offsets, const int& dynamic_shift)
//	{
//		int offset = (program[offsets.x + 1] + dynamic_shift) & 128;
//		int length = program[offsets.x + 2];
//		//load cache integer, trimming leading bits
//		int out = cache[offset >> 32] & (0xFFFFFFFF >> (offset & 31));
//	
//		//if field contained in one int, trim any trailing bits
//		if (offset & 31 + length < 33) return out >> (32 - offset & 31 - length);
//		//else merge with next chunk
//		else return (out << (offset + length) & 31) + (cache[1 + (offset >> 32)] >> (32 - (offset + length) & 31));
//	}
//	
//	__host__ __device__ void storeFilter(int& bool_reg, short2& offsets)
//	{
//		int working = __ballot((bool_reg >> (31 - program[offsets.x])) & 0x1); //gets the results for one rule in bool_reg using ballot to poll
//
//		//trim to correct byte for threads subwarp
//
//		working = (working >> filter_cache.w) & 0xFF;
//
//		filter_cache.x |= static_cast<unsigned char>(working & (threadIdx.x & 7 == program[offsets.x] & 7) ? 0xFF | 0x0);
//		if  (program[offsets.x]  & 7 == 7)//just wrote 8th rule to filter mem
//		{
//			//currently have 4 sets of results (one for each 8 thread subwarp). Each thread holds 8 results for a single filter.
//			//Each filter is spread over 4 threads in section [0-7][8-15][16-23][24-31]
//			//Filter chars can be combined into a single int and written, using 8 of the 32 threads
//
//			//fist merge [0-7] & [8-15] / [16-23]&[24-31] threads
//			//			
//			/*working = filter_cache.x << (((3 - (filter_cache.y >> 3)) & 0x1) << 3);
//			working |= _shfl_down(working, 8, 16);
//
//			working = working << (((15 - (filter_cache.y >> 2)) & 0x1) << 4);
//			working |= _shfl_down(working, 16, 32);*/
//
//			//	00   01	  02   03	04	 05	  06   07	<-- threadIdx	
//			//[0,0][1,0][2,0][3,0][4,0][5,0][6,0][7,0]	<-- Filter[filter number, offset shift]
//			//
//			//	08	 09	  10   11   12	 13	  14   15
//			//[0,1][1,1][2,1][3,1][4,1][5,1][6,1][7,1]
//			//
//			//	16	 17	  18   19	20	 21	  22   23
//			//[0,2][1,2][2,2][3,2][4,2][5,2][6,2][7,2]
//			//
//			//	24	 25	  26   27	28	 29	  30   31
//			//[0,3][1,3][2,3][3,3][4,3][5,3][6,3][7,3]
//
//			static_cast<unsigned char*>(filter_mem)[filter_size * ((filter_cache.z << 3) + (threadIdx.x & 7)) + (filter_cache.y >> 3)] = filter_cache.x;
//		
//			++filter_cache.z;	//increment base offset
//		}
//	}
//	/*__host__ __device__ void cacheSlide(int* cache, int& x)
//	{
//		int slide_start = program[x + 2];
//		int slide_width = program[x + 3];
//		for (int k = 0; k < slide_width; k++)
//		{
//			cache[k] = cache[slide_start + k];
//		}
//	}*/
//}
//
//#pragma endregion
//
//#pragma region Processor
//
////#define SET_PROTOCOL 0
////#define JUMP 0
////#define JUMP_TRUE 0
////#define JUMP_FALSE 0
//
//#define EXTRACT_FIELD_TMP 0
////#define EXTRACT_FIELD_REG 0
////
////#define STORE_TMP_REG 0
////#define STORE_TMP_GLOBAL 0
////#define STORE_REG_GLOBAL 0
////#define STORE_GLOBAL_REG 0
////#define STORE_GLOBAL_TMP 0
//
//#define STORE_FILTER 0
//
//#define RULE_COMPARISON_REG 0
//#define RULE_COMPARISON_TMP 0
//
//#define STATE_PX	(offsets.x)
//#define STATE_		(offsets.y)
//
//#define ACTIVE_SEGMENT	(_any
//#define PROTO_ID		(proto_state.x)
//#define PROTO_NEXT		(proto_state.y)
//#define PROTO_START		(proto_state.z)
//#define PROTO_LENGTH	(proto_state.w)
//
//class GpfVm
//{
//	//int int_reg[8];
//	//int bool_reg;
//	int bool_reg;
//	short2 offsets;		//stores the offsets of the program pointer and data shift register
//	uchar4 proto_state;	//stores the primary state of the active protocol - id, data span and payload
//	int tmp;			//externally accessible semi coherent working register
//	int working;		//internal non-coherent working register
//	
//	int local_shift;
//	uchar4 control;
//	GpfCache cache;
//
//public:
//
//	__host__ __device__ GpfVm() : bool_reg(0), tmp(0), working(0)
//	{
//		//state.x = 1;	//layer loop
//		//state.y = 1;	//segment loop
//		control = (uchar4)0x01010000;
//		
//		//PROTO_ID = 1; //first (root) protocol
//		//PROTO_NEXT = 0; //Data
//		//PROTO_START = 0; //data always trimmed
//		//PROTO_LENGTH unknown at this point - may be ignored if protocol is statically sized
//		proto_state =  (uchar4)0x01000000;
//	
//	}
//
//	__host__ __device__ void Process()
//	{
//		
//		while (control.x < _layers)
//		{
//			//load cache
//			cache.cacheFill(k);
//
//			//no need to skip cache loads, as the proto_id test ensures that processing terminates when 
//			//protocol goes out of bounds. as this is the same test used to determine if caching should
//			//be performed, processing will always terminate before the test would be of any value.
//
//
//			//process associated segment
//			while (control.y < _segments[control.x])
//			{
//				//warp vote to determine whether to process the segment
//				//if all threads do not match the segment, it can be skipped
//				if (__all(PROTO_NEXT - program[offsets.x]))
//				{
//					//skip over segment 
//					offsets.x += program[offsets.x + 1];
//					continue;
//				}
//				local_shift = 0;
//				++control.y;	//increment early to avoid read after write conflict
//				offsets.x += 2;
//				processSegment();
//			}
//
//			++control.x;		//increment early to avoid read after write conflict
//			if (__any(PROTO_ID) == 0) break; //escape if all warp protocols are null
//		}
//	}
//
//	__host__ __device__ void processSegment()
//	{
//		while (program[offsets.x] != 0xFF)
//		{
//			switch(program[offsets.x])
//			{
//
//				case TERMINATE_HEADER:
//					if (PROTO_ID == program[offsets.x + 1]) 
//					{
//						proto_state.x = 0; //dont write proto_id and proto_next sequentially since they are same register
//						proto_state.y = 0;
//					}
//					offsets.x += 2;
//					break;
//					
//					//Extract
//				case EXTRACT_FIELD_TMP:	//need to add local shift
//					tmp = cache.extractField(offsets, local_shift);
//					offsets.x += 3;
//					break;
//
//					//Rule Comparison
//				case RULE_COMPARISON_REG:
//					ruleComparison(int_reg[program[offsets.x + 5]]);
//					offsets.x += 6;
//					break;
//				case RULE_COMPARISON_TMP:
//					ruleComparison(tmp);
//					offsets.x += 5;
//					break;
//
//				case STORE_FILTER:
//					cache.storeFilter(bool_reg, offsets);
//			}
//		}
//
//				//	//Protocl
//				//case SET_PROTOCOL:
//				//	if (PROTO_ID == program[x]) 
//				//	{
//				//		PROTO_ID = PROTO_NEXT;
//				//		PROTO_NEXT = 0;
//				//	}
//				//	break;
//				//case JUMP:
//				//	break;
//				//case JUMP_FALSE:
//				//	extractBool(bool_reg, x, working);
//				//	if (__all(working)) //!=0
//				//	{
//				//	}
//				//	else //dont jump - process decisional code
//				//	{
//				//	}
//				//	break;
//				//case JUMP_TRUE:
//				//	extractBool(bool_reg, x, working);
//				//	if (__all(working) == 0) 
//				//	{
//				//	}
//				//	else
//				//	{
//				//	}
//				//	break;
//				//case SHIFT_DATA:
//				//	extractBool(bool_reg, x, working);
//				//	if (PROTO_ID == program[x]) 
//				//	{
//				//		protocol _id = PROTO_NEXT;
//				//		PROTO_NEXT = -1;
//				//	}
//				//	break;
//				//case EXTRACT_FIELD_REG:
//				//	int_reg[program[x + 4]] = extractField(cache, x);
//				//	break;
//				//	//Store INT
//				//case STORE_TMP_REG:
//				//	int_reg[program[x + 2]] = tmp;
//				//	break;
//				//case STORE_TMP_GLOBAL:
//				//	integer_mem[program[x + 2] * packet_count + (blockDim.x * blockIdx.x) + threadIdx] = tmp;
//				//	break;
//				//case STORE_REG_GLOBAL:
//				//	integer_mem[program[x + 2] * packet_count + (blockDim.x * blockIdx.x) + threadIdx] = int_reg[program[x + 3]];
//				//	break;
//				//case STORE_GLOBAL_REG:
//				//	int_reg[program[x + 2]] = integer_mem[program[x + 3] * packet_count + (blockDim.x * blockIdx.x) + threadIdx];
//				//	break;
//				//case STORE_GLOBAL_TMP:
//				//	tmp = integer_mem[program[x + 2] * packet_count + (blockDim.x * blockIdx.x) + threadIdx];
//				//	break;
//				//case STORE_FILTER:
//				//	switch(program[x + 2])
//				//	{
//				//	case 0:
//				//	}
//				//	break;
//				//	
//				//	//BOOL Comparison
//				//case BOOL_COMPARISON_REG:
//				//	ruleComparison(int_reg[program[x + 2]], bool_reg, x);
//				//	break;
//				//case BOOL_COMPARISON_TMP:
//				//	ruleComparison(tmp, bool_reg, x);
//				//	break;
//				//}
//	}
//	
//	#define COMPARISON_EQU 0
//	#define COMPARISON_NEQ 1
//	#define COMPARISON_LES 2
//	#define COMPARISON_GRT 3
//	#define COMPARISON_LEQ 4
//	#define COMPARISON_GEQ 5
//
//	//	Compares the value of in to a value in program memory, 
//	__host__ __device__ void RuleComparison(int* field)
//	{
//		working = 0x1 << (31 - program[x + 4]);
//		switch(program[offsets.x + 2])
//		{
//			case COMPARISON_EQU: 
//				bool_reg = (PROTO_ID == program[offsets.x + 1]) && (*field == program[x + 3]) ? bool_reg | working : bool_reg & ~working;
//				break;
//			case COMPARISON_NEQ: 
//				bool_reg = (PROTO_ID == program[offsets.x + 1]) && (*field != program[x + 3]) ? bool_reg | working : bool_reg & ~working;
//				break;
//			case COMPARISON_LES:
//				bool_reg = (PROTO_ID == program[offsets.x + 1]) && (*field < program[x + 3]) ? bool_reg | working : bool_reg & ~working;
//				break;
//			case COMPARISON_GRT: 
//				bool_reg = (PROTO_ID == program[offsets.x + 1]) && (*field > program[x + 3]) ? bool_reg | working : bool_reg & ~working;
//				break;
//			case COMPARISON_LEQ: 
//				bool_reg = (PROTO_ID == program[offsets.x + 1]) && (*field <= program[x + 3]) ? bool_reg | working : bool_reg & ~working;
//				break;
//			case COMPARISON_GEQ:
//				bool_reg = (PROTO_ID == program[offsets.x + 1]) && (*field >= program[x + 3]) ? bool_reg | working : bool_reg & ~working; 
//				break;
//		}
//
//	}
//	//
//	//__device__ inline void extractBool(int& reg, int& x, int& out)
//	//{
//	//	out = (reg >> program[x + 2]) & 0x1; 
//	//}
//
//}
//
//__global__ void GpfProcessor(int stream_no)
//{
//	
//}
//
//#pragma endregion

#pragma region Host

void Process(void * args);

void Setup(FilterOptions& options)
{
	//cudaMalloc((void**) &packet_data, options.packet_buffer_size());
	//cudaMalloc((void**) &filter_mem, options.filter_memory_size());

}

static void GpfVmLauncher(zmq::context_t * zmq_context, FilterOptions filter_options)
{
	/*ProcessArgs * args = static_cast<ProcessArgs*>(malloc(sizeof(ProcessArgs)));
	args->context = zmq_context;
	args->options = filter_options;
	tthread::thread* proc = new tthread::thread(ProcessFilter, (void*) args);*/
}

void ProcessFilter(void * args)
{
	//ProcessArgs * proc = static_cast<ProcessArgs*>(args);
	//zmq::socket_t buffer(proc->context, ZMQ_PAIR);
	//buffer.connect("inproc://gpfbuffer");

	//OutputBuffer output(proc->context, proc->options);

	//CudaBufferPointer ptr;

	//FilterOptions options = proc->options;
	//Setup(options);	//setup the vm memory regions (once off)
	//
	////create streams
 //   cudaStream_t *streams = (cudaStream_t*) malloc(options.streams * sizeof(cudaStream_t));
 //   for(int i = 0; i < options.streams; i++) {
 //       checkCudaErrors( cudaStreamCreate(&(streams[i])) );
 //   }

	//int curr_stream = 0;
	//_int64 packet_count = 0;
	//do
	//{
	//	buffer.recv(&ptr, sizeof(CudaBufferPointer));	//get next full write-combined buffer

	//	if (ptr.size != options.packet_buffer_stream_size()) //last pointer - use default streams
	//	{
	//		cudaMemcpyAsync(packet_data, ptr.buffer, ptr.size, cudaMemcpyHostToDevice, 0);
	//		GpfProcessor<<<options.blocks(), options.threads>>>(0);
	//		
	//		if (options->filter_results()) cudaHostAlloc((void**) &filter_results,  options->filter_memory_size(), cudaHostAllocDefault); 
	//		if (options->integer_results()) cudaHostAlloc((void**) &integer_results, options->integer_memory_size(), cudaHostAllocDefault); 
	//	}
	//	//copy packet buffer to device memory
	//	for (int k = 0; k < options.streams; k++)
	//	{
	//		//copy records to the device - async
	//		cudaMemcpyAsync(packet_data + k * options.packets_per_stream(), 
	//						ptr.buffer + k * options.packets_per_stream(), 
	//						options.packet_buffer_stream_size(ptr.size, k), 
	//						cudaMemcpyHostToDevice, 
	//						streams[k]);

	//		//process stream contents - async
	//		/*GpfProcessor<<<options.blocks(), options.threads, 0, streams[k]>>>(k);*/
	//	}
	//	char* filter_results;
	//	int* integer_results;

	//	//retreive results - async
	//	if (options->filter_results()) cudaHostAlloc((void**) &filter_results, options->filter_memory_size(), cudaHostAllocDefault); 
	//	if (options->integer_results()) cudaHostAlloc((void**) &integer_results, options->integer_memory_size(), cudaHostAllocDefault); 

	//	if (options->filter_results()) 
	//	{
	//		cudaMemcpy(filter_results, filter_mem, options->filter_memory_size(), cudaMemcpyDeviceToHost, 0); //use defaults stream
	//		output.CopyFilterResults(filter_results, options->filter_memory_size());
	//	}
	//	if (options->integer_results()) 
	//	{
	//		cudaMemcpy(integer_results, integer_mem, options->integer_memory_size(), cudaMemcpyDeviceToHost, 0); //use defaults stream
	//		output.CopyFilterResults(integer_results, options->integer_memory_size());
	//	}

	//} while (ptr.more);
	////complete

}

#pragma endregion