
#pragma once

#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include "VmCommon.h"
#include <stdio.h>
#include "zmq.hpp"

#include "tinythread.h"
#include "OutputBuffer.h"

#include "GpfVm.cuh"

#pragma region Defines

//#define GPF_DEBUG

//#define PERFORMANCE_TESTING
//thread count and streams in VmCommon
#define ITERATIONS				32		//number of iterations before filtering - not user configurable

#define GPF_WARPS				(GPF_THREADS/32)

#define PACKETS_PER_BLOCK		(GPF_THREADS * ITERATIONS)
#define PACKETS_PER_WARP		(PACKETS_PER_BLOCK / GPF_WARPS)

#define PROGRAM_X				offsets.x		//the program counter base offset
#define PROTOCOL_X				offsets.y		//the start byte offset of the current protocol

#define PROGRAM(x)				rule_program[PROGRAM_X + x] //the value in program memory x indeces past the program counter
#define FILTER_PROGRAM(x)		fltr_program[PROGRAM_X + x]

#define PROTO_ID				proto_state.x //Identifies the protocol
#define PROTO_NEXT				proto_state.y	//Identifies the next protocol
#define PROTO_LENGTH			proto_state.z	//Protocol length (Bytes)
#define PROTO_CURR				proto_state.w	//Identifies the active protocol (may or may not be relevant to current thread, but will be relevant to a thread in the warp)
#define PROTO_LEN_BITS			(proto_state.z << 3)


#define WARP_ID					thread_state.x		//threads warp index
#define STREAM					thread_state.y
#define ITERATION				thread_state.z		//current iteration
#define ALIGNMENT				thread_state.w		//byte alignment in integer [0:3]

#define CACHE_LANE				transform.w
#define PACKET_COUNT			_packet_count

#define COMPARISON_EQU 0
#define COMPARISON_NEQ 1
#define COMPARISON_LES 2
#define COMPARISON_GRT 3
#define COMPARISON_LEQ 4
#define COMPARISON_GEQ 5

#define EXPR_READ_TMPMEM	0
#define EXPR_READ_SYSREG	1
#define EXPR_READ_LOOKUP	2

#define EXPR_WRITE_TMPMEM	0
#define EXPR_WRITE_SYSREG	1

#define PRED_WRITE_TMPMEM	0
#define PRED_WRITE_FILTER	1

#define PRED_READ_TMPMEM	0
#define PRED_READ_FILTER	1



//each block has 512 threads - each processing 32 packets per rule - so each rule requires 512 ints to store
//each block writes 512 / 32 = 8  ints per iteration
//each warp writes a single int at the index equivalent to their warp id after the start of the iteration
//#define WORKING_WRITE(rule)			working_mem[_working_per_block * blockIdx.x + rule * GPF_THREADS + ITERATION * (GPF_THREADS >> 5) + WARP_ID]
//#define WORKING_READ(rule)			working_mem[_working_per_block * blockIdx.x + rule * GPF_THREADS + threadIdx.x]


#define WORKING_WRITE(rule)			working_mem[STREAM * _working_per_stream + _working_per_block * blockIdx.x + _working_per_warp * WARP_ID + rule * 32 + ITERATION]
#define WORKING_READ(rule)			working_mem[STREAM * _working_per_stream + _working_per_block * blockIdx.x + _working_per_warp * WARP_ID + rule * 32 + (threadIdx.x & 31)]

#define INTEGER_RW(index)			integer_mem[STREAM * _integer_per_stream + _integer_mem_size * index + packet_index]

//#define FILTER_RW(index)			filter_mem[_filter_mem_size * (_bool_result_count * STREAM  + index) + blockDim.x * blockIdx.x + threadIdx.x]
#define FILTER_RW(index)			filter_mem [STREAM * _filter_per_stream  + _filter_mem_size * index + blockDim.x * blockIdx.x + threadIdx.x]
#pragma endregion

#pragma region Constant Memory

__constant__ int * packet_data;		//memory region containing trimmed packet data
__constant__ int * filter_mem;		//memory region containing 1-bit filter results
__constant__ int * integer_mem;		//memory region containing 32-bit integer results
__constant__ int * working_mem;

__constant__ unsigned char rule_program[14 * 1024];		//the main (rule) program
__constant__ unsigned char fltr_program[2 * 1024];		//1-bit filter evaluation program (post-process)
__constant__ int lookup[255];							//32-bit integer lookup memory
__constant__ int _packet_count;							//number of packets per streamed batch

__constant__ int _record_size;					//the size of a trimmed/padded packet record in ints
__constant__ int _filter_mem_size;				//size of filter mem per result in ints
__constant__ int _integer_mem_size;				//size of integer mem per result in ints

__constant__ int _bool_result_count;			//number of unique boolean results per packet
__constant__ int _int_result_count;				//number of unique integer results per packet

__constant__ int _layer_count;					//number of layers in the rule program
__constant__ int _rules_per_packet;				//number of rules per packet in working memory

__constant__ char _root_proto;					//the root protocol (ethernet, 802.11 etc)
__constant__ char _start_offset;				//the number of bits between the start of the packet and the first referenced field (used to account for trimming of the first protocol)

__constant__ int _working_per_block;
__constant__ int _working_per_warp;

__constant__ int _working_per_stream;
__constant__ int _filter_per_stream;
__constant__ int _integer_per_stream;

#pragma endregion

#pragma region Device Methods

__device__ GpfVm::GpfVm(int stream)
{
	////breifly use shared memory to broadcast the working memory pointer to all threads in block
	//__shared__ int * tmp;
	//	
	////use shift as a temporary register

	////use shared memory to broadcast working memory pointer
	//if (threadIdx.x == 0)
	//{
	//	//_working_per_block = _rules_per_packet * blockDim.x (GPF_THREADS)
	//	// working memory organsied as a set of contiguous rule results (512 ints -> 16384 packets). iteration is index for each rule (32 total)
	//	tmp = (int*) malloc(_working_per_block);
	//	if (tmp == nullptr) printf("null working in block %d for some reason..\n", blockIdx.x);
	//}
	//__syncthreads();
	
	//sanitize
	

	WARP_ID = threadIdx.x >> 5;			//warp number
	CACHE_LANE = threadIdx.x & 3;			//thread lane in group
	ITERATION = 0;							//iterations			
	STREAM = stream;
	
	//if (blockIdx.x  == 30 && threadIdx.x == 192)
	//{
	//	printf("Warp ID = %d\nPPW = %d\nProduct = ",WARP_ID,PACKETS_PER_WARP,WARP_ID * PACKETS_PER_WARP);
	//}
	packet_index = blockIdx.x * PACKETS_PER_BLOCK + WARP_ID * PACKETS_PER_WARP + (threadIdx.x & 31);

	transform.x = (5 - CACHE_LANE) & 0x3;	//first transform
	transform.y = 3 - CACHE_LANE;			//second transform
	transform.z = (2 + CACHE_LANE) & 0x3;	//third transform	

	
	//0 each iteration of WPP rules with a single command
	for (int k = 0; k < _rules_per_packet; k++)
	{
		//ensure thread only handles its own local memory - warps are not guarenteed to exeute in unison
		//early warp could start writing rules before later warp has opertunity to zero memory
		WORKING_READ(k) = 0;
	}//0 each iteration of WPP rules with a single command
}
	
#pragma region Processor

__device__ void GpfVm::Gather()
{
#pragma unroll 32
	for (int k = 0; k < 32; k++)
	{
	/*	if (blockIdx.x  == 27 && threadIdx.x == 0)
		{
			printf("Iteration = %d\n",ITERATION);
		}*/
		
		PROGRAM_X = 0;	//reset program
		PROTOCOL_X = 0;

		PROTO_ID = 0;
		PROTO_NEXT =  packet_index < PACKET_COUNT ? _root_proto : 0; //physical layer specified in capture header
		PROTO_LENGTH = 0;
		PROTO_CURR = 0;
		ALIGNMENT = 0;

		//sanitise integer mem
		for (int j = 0; j < _int_result_count; j++)
		{
			INTEGER_RW(j) = 0;
		}
		
		if (__ballot(PROTO_NEXT > 0) == 0)
		{
			break; //break out if all protocols are out of range
		}

		ProcessPacket();
		
		packet_index += 32;	//blockDim.x packets per iteration
		++ITERATION; 
	}
}

__device__ void GpfVm::Filter()
{
	//process filters

	if (GPF_THREADS * blockIdx.x + threadIdx.x < (PACKET_COUNT >> 5) + (PACKET_COUNT & 31 == 0 ? 0 : 1)) //in range (32 packets per thread)
	{
		/*if (threadIdx.x == 437 && blockIdx.x == 7)
		{
			printf("something here");
		}*/

		//__syncthreads();
		PROGRAM_X = 4; //adjustment due to constant cache problem. Remove once resolved.
		uchar4 loop_counter = make_uchar4(0,0,0,0);
		uchar4 loop_boundry;

	/*	if (blockIdx.x == 0 && threadIdx.x == 0) 
		{
			printf("Filter Memory:\n\t");
			for (int k = 0; k < 16; k++) printf(" %d",  FILTER_PROGRAM(k));
			printf("\n");
		}*/

		while (loop_counter.x < _bool_result_count)
		{
			++loop_counter.x;
			/*if (blockIdx.x == 0 && threadIdx.x == 0) 
			{
				printf("Filter Memory:\n\t");
				for (int k = 0; k < 8; k++) printf(" %d",  FILTER_PROGRAM(k));
				printf("\n");
			}*/

			loop_counter.y = 0;
			loop_boundry.y = FILTER_PROGRAM(0);	//transaction count
			++PROGRAM_X;

			while (loop_counter.y < loop_boundry.y)
			{
				loop_counter.z = 0;
				loop_boundry.z = FILTER_PROGRAM(0);	//or count
				++PROGRAM_X;
				int ans = 0;
				
				while (loop_counter.z < loop_boundry.z)
				{
					loop_counter.w = 0;
					loop_boundry.w = FILTER_PROGRAM(0);	//and count
					++PROGRAM_X;
					int and = 0xFFFFFFFF;

					while (loop_counter.w < loop_boundry.w)
					{
						
						int value = FILTER_PROGRAM(0) == 1
							?	~WORKING_READ(FILTER_PROGRAM(1))
							:	WORKING_READ(FILTER_PROGRAM(1))
							;

						/*if (blockIdx.x == 0 && threadIdx.x < 16) 
						{
							printf("Packet Group %d:\n\tTransaction Count = %d\n\tOr Count = %d\n\tAnd Count = %d\n\tValue = %x\n\tWorking = %d\n", GPF_THREADS * blockIdx.x + threadIdx.x, loop_boundry.y, loop_boundry.z, loop_boundry.w, value, WORKING_READ(FILTER_PROGRAM(1)));
						}*/
						and &= value;
						/*if (blockIdx.x == 0 && threadIdx.x < 4) 
						{
							printf("Packet Group %d:\n\tAnd Value = %x\n\tValue = %x\n", GPF_THREADS * blockIdx.x + threadIdx.x, and, value);
						}*/
						++loop_counter.w;
						PROGRAM_X += 2;
					}
					
					++loop_counter.z;
					ans |= and;
				}
				++loop_counter.y;
				
				/*if (blockIdx.x == 0 && threadIdx.x < 4) 
				{
					printf("Packet Group %d:\n\tTransaction Count = %d\tProgram_x = %d\n", GPF_THREADS * blockIdx.x + threadIdx.x, loop_boundry.y, PROGRAM_X);
				}*/

				if (FILTER_PROGRAM(0) == PRED_WRITE_TMPMEM)
				{
					WORKING_READ(FILTER_PROGRAM(1)) = ans;
				}
				//case: PRED_WRITE_FILTER:
				else
				{	
					/*if (ans == 0xFFFFFFFF && FILTER_PROGRAM(1) == 0)
					{
						printf("something here");
					}*/
					FILTER_RW(FILTER_PROGRAM(1)) = __byte_perm(__brev(ans), 0, 0x0123);
					/*if (blockIdx.x == 22 && threadIdx.x < 16) 
					{
						printf("Packet Group %d:\n\tAns = %x\n\Filter[Ans] = %x\n", GPF_THREADS * blockIdx.x + threadIdx.x, ans, FILTER_RW(FILTER_PROGRAM(1)));
					}*/
				}
				PROGRAM_X += 2;
			}
		}
	}
		
	//clear working memory after all threads have completed
	/*__syncthreads();

	if (threadIdx.x == 0)
	{
		free(working_mem);
	}*/
}

__device__ void GpfVm::ProcessPacket()
{
	//if (this == nullptr || ITERATION > 31)
	//{
	//	printf("SOmething is wrong...");
	//	return;
	//}

	uchar4 loop_control = make_uchar4(0,0,0,0);

	while (loop_control.x < _layer_count)
	{
		loop_control.y = 0;
		loop_control.z = PROGRAM(0);

		PROGRAM_X += 1;
		//test protocols for relevance / perform protocol setup if relevant
		while (loop_control.y++ < loop_control.z)
		{
			if (PROGRAM(0) == PROTO_NEXT)
			{
				PROTO_ID = PROTO_NEXT;
				PROTO_NEXT = 0;
				PROTOCOL_X += PROTO_LENGTH;
				PROTO_LENGTH = PROGRAM(1);
			}
			PROGRAM_X += 2;
		}
		
		
		if (__all(PROTO_NEXT) != 0) {
			++loop_control.x;		//increment early to avoid read after write conflict
			PROGRAM_X += PROGRAM(0);
			continue; //all packets out of headers - break out of layer loop
		}
		

		
		//if (blockIdx.x == 0 && threadIdx.x < 4) 
		//{
		//	printf("Packet %d Layer %d :\n\tProto = %d\n\tLength = %d\n\tX = %d\n\tProgram X = %d\n", packet_index, loop_control.x, PROTO_ID, PROTO_LENGTH, PROTOCOL_X, PROGRAM_X);
		//}
			
		//segemt count
		uchar4 inner_loop;
		inner_loop.x = 0;
		inner_loop.y = PROGRAM(1);
		PROGRAM_X += 2;

	/*	if (blockIdx.x == 0 && threadIdx.x < 4) 
		{
			printf("Packet %d Layer %d :\n\tSegment Count = %d\n\tProgram_x = %d\n", packet_index, loop_control.x, PROGRAM(0),  PROGRAM_X);
		}*/

		//no need to skip cache loads, as the proto_id test ensures that processing terminates when 
		//protocol goes out of bounds. as this is the same test used to determine if caching should
		//be performed, processing will always terminate before the test would be of any value.


		//process associated segment
		while (inner_loop.x < inner_loop.y)
		{
			++inner_loop.x;	//increment early to avoid read after write conflict
			
			inner_loop.z = 0;
			
		/*	if (blockIdx.x == 0 && threadIdx.x < 4) 
			{
				printf("Packet %d Layer %d :\n\tSet Count = %d\n\tCache Load = %d\n", packet_index, loop_control.x, PROGRAM(0), PROGRAM(1));
			}*/

			//fill cache for the segment
			CacheLoad(packet_data + (STREAM * PACKET_COUNT + packet_index & 0xFFFFFFFC) * _record_size + CACHE_LANE);
			
			inner_loop.w = PROGRAM(0);
			++PROGRAM_X;


			//no cooperation required after this point

			while(inner_loop.z < inner_loop.w)
			{
				++inner_loop.z;
				
				/*if (blockIdx.x == 0 && threadIdx.x < 4) 
				{
					printf("Packet %d Layer %d :\n\tProto = %d\n\tProgram = %d\n\tNext = %d\n", packet_index, loop_control.x, PROTO_ID, PROGRAM(0), PROGRAM(1));
				}*/
				
				
				/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
				{
					printf("Packet %d :\n\tProgram_x = %d\n", packet_index,  PROGRAM_X);
				}*/
				//warp vote to determine whether to process the segment
				//if all threads fail to match the segment, it can be skipped
				if (__all(PROTO_ID - PROGRAM(0)))
				{
					/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
					{
						printf("Packet %d Skipped:\n\tAll = \tProgram_x = %d\n\tStart = %d\n", packet_index, __all(PROTO_ID - PROGRAM(0)), PROGRAM_X);
					}*/
					//skip over segment 
					PROGRAM_X += PROGRAM(1);
					continue;
				}

				PROTO_CURR = PROGRAM(0);
				PROGRAM_X += 2;
				/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
				{
					printf("Packet %d :\n\tProgram_x = %d\n", packet_index,  PROGRAM_X);
				}*/
				ProcessSet();
			}
		}

		++loop_control.x;		//increment early to avoid read after write conflict
		if (__any(PROTO_NEXT) == 0) break; //escape if all warp protocols are null
		PROTO_ID = 0;
	}
}
	


//processes a set for a specifc protocol
__device__ void GpfVm::ProcessSet()
{
	uchar4 set_mem;
	set_mem.x = 0;				//current set
	set_mem.y = PROGRAM(0);		//set count

	unsigned int value;

	++PROGRAM_X;

	/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
	{
		printf("Packet %d :\n\tProgram_x = %d\n", packet_index,  PROGRAM_X);
	}*/
	//each set contains one or more fields, which may in tern contain one or more filters
	//iterate through each field, extracting it and filtering it
	while (set_mem.x < set_mem.y)
	{
		//iterator not used again so can increment early
		++set_mem.x;
		value = ExtractField();

		//store field value if it is needed as an integer result
		if (PROGRAM(0) != 0xFF)
		{
			INTEGER_RW(PROGRAM(0)) = PROTO_ID == PROTO_CURR ? value : 0;
		}

		set_mem.z = 0;
		set_mem.w = PROGRAM(1);
		PROGRAM_X += 2;
			
		while (set_mem.z < set_mem.w)
		{
			++set_mem.z;
			bool ans = false;
			switch (PROGRAM(0))
			{
			case COMPARISON_EQU:
				ans = PROTO_ID == PROTO_CURR && value == lookup[PROGRAM(1)];
				break;
			case COMPARISON_NEQ:
				ans = PROTO_ID == PROTO_CURR && value != lookup[PROGRAM(1)];
				break;
			case COMPARISON_LES:
				ans = PROTO_ID == PROTO_CURR && value < lookup[PROGRAM(1)];
				break;
			case COMPARISON_GRT:
				ans = PROTO_ID == PROTO_CURR && value > lookup[PROGRAM(1)];
				break;
			case COMPARISON_LEQ:
				ans = PROTO_ID == PROTO_CURR && value <= lookup[PROGRAM(1)];
				break;
			case COMPARISON_GEQ:
				ans = PROTO_ID == PROTO_CURR && value >= lookup[PROGRAM(1)];
				break;
			}

			/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
			{
				if (ans) printf("Packet %d :\n\t Value = %d\n\tCompValue = %d\n\tAns = true\n", packet_index, value, lookup[PROGRAM(1)]);
				else printf("Packet %d :\n\t Value = %d\n\tCompValue = %d\n\tAns = false\n", packet_index, value, lookup[PROGRAM(1)]);
			}*/

			if (PROGRAM(2) > 0 && ans) PROTO_NEXT = PROGRAM(2);
			if (PROGRAM(3) != 0xFF)
			{
				
				int tmp = __ballot(ans ? 1 : 0);
				if ((threadIdx.x & 31) == 0)
				{			
					WORKING_WRITE(PROGRAM(3)) |= tmp;

					/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 16) 
					{
						printf("Packet %d + 32 :\n\t Working Store = %x\n\tWorking Index = %d\n", packet_index, WORKING_READ(PROGRAM(3)), PROGRAM(3));
					}*/
				}
			}
			PROGRAM_X += 4;
		}


		//prepare to iterate through transactions, storing n-1 of them in tmp memory
		set_mem.w = PROGRAM(0);
			
		PROGRAM_X += 1;
		
		if (set_mem.w == 0) continue;

		set_mem.z = 0;

		//DO NOT use dynamic memory -> causes exponential decrease in performance
		//int* tmp_mem = (int*) malloc((set_mem.w) * sizeof(int));
		while (set_mem.z < set_mem.w)
		{
			++set_mem.z;
				
			uchar4 trans_mem;
			trans_mem.x = 0;
			trans_mem.y = PROGRAM(0);
			++PROGRAM_X;

			int ans = 0;
		
			while (trans_mem.x < trans_mem.y)
			{
				trans_mem.z = 0;
				trans_mem.w = PROGRAM(0);
				++PROGRAM_X;
				int mult = 1;

				trans_mem.x++;

				while (trans_mem.z < trans_mem.w)
				{
					trans_mem.z++;

					switch (PROGRAM(0))
					{
				//Currently unsupported
					/*case EXPR_READ_TMPMEM:
						mult *= tmp_mem[PROGRAM(1)];
						break;*/
					case EXPR_READ_SYSREG:
						//0 = length, 1 = value
						mult *= PROGRAM(1) == 0 ? (int)PROTO_LENGTH : value;
						break;
					case EXPR_READ_LOOKUP:
						mult *= lookup[PROGRAM(1)];
						break;
					}
					PROGRAM_X += 2;
				}
				ans += mult;
			}
				
			if (PROGRAM(0) == 0) // == EXPR_WRITE_SYSREG (1) - do for all threads
			{
				//Currently unsupported
				//tmp_mem[PROGRAM(1)] = ans;
			}
			else if (PROTO_ID == PROTO_CURR)// && PROGRAM(0) == EXPR_WRITE_TMPMEM (0)) - do for legitimate threads only
			{
				//can only write to length register
				PROTO_LENGTH = ans;
			}
				
			PROGRAM_X += 2;
		}
		//free(tmp_mem);
	}
}
#pragma endregion

#pragma region Cache


__device__ void GpfVm::CacheLoad(const  int* __restrict__ start_offset)
{
	//assume offset does not lie on integer lines
	
	int working = PROTOCOL_X + PROGRAM(0); //byte offset of current protocol + byte offset from start of protocol
	//FFFC = 11111111 11111100
		
	//int* start_offset = packet_data + (STREAM * PACKET_COUNT + packet_index & 0xFFFFFFFC) * _record_size  + CACHE_LANE; //group offset
	ALIGNMENT = working & 3;	//set byte alignment offset for extractions
	working = working >> 2; //get integer index in record

	//if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
	//{
	//	printf("Packet\n"); //%d :\n\tByte Offset = %d\n\tAlignment = %d\n\tIndex in Record = %d\n", packet_index, PROTOCOL_X + PROGRAM(0), ALIGNMENT, working);
	//}
	/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
	{
		printf("\nPacket %d :\n\tGroup Offset = %d\n\tRecord Size = %d\n\tCache Lane = %d\nWorking = %d\n", packet_index, group_offset,_record_size, CACHE_LANE, working);
	}*/
	
	++PROGRAM_X;
	
	cache[0] = __ldg(start_offset + __shfl(working, 0, 4));
	cache[1] = __ldg(start_offset + _record_size + __shfl(working, 1, 4));
	cache[2] = __ldg(start_offset + _record_size * 2 + __shfl(working, 2, 4));
	cache[3] = __ldg(start_offset + _record_size * 3 + __shfl(working, 3, 4));
		
	// 1,0,3,2
	working = __byte_perm(cache[transform.x], 0, 0x0123);
	cache[transform.x] = __shfl(working, static_cast<int>(transform.x), 4);

	// 3,2,1,0		
	working = __byte_perm(cache[transform.y], 0, 0x0123);
	cache[transform.y] = __shfl(working, static_cast<int>(transform.y), 4);

	// 2,3,0,1
	working = __byte_perm(cache[transform.z], 0, 0x0123);
	cache[transform.z] = __shfl(working, static_cast<int>(transform.z), 4);

	cache[CACHE_LANE] = __byte_perm(cache[CACHE_LANE], 0, 0x0123);
	/*if (blockIdx.x == 0 && threadIdx.x < 4 && ITERATION < 4) 
	{
		printf("\nPacket %d :\n\tCache = [ %x | %x | %x | %x ]\n", packet_index, cache[0], cache[1], cache[2], cache[3]);
	}*/
}
	
//extracts a field from the cache
//bit_offset - the bit offset of the field from the beginning of the segment, including any local shift
//bit_length - the bit length of the field
//returns - the field
__device__ unsigned int GpfVm::ExtractField()
{
	short2 field_offsets;
	field_offsets.x = (ALIGNMENT<<3) + PROGRAM(0); //bits to start of cache + bits from start of cache to field
	field_offsets.y = PROGRAM(1);
	PROGRAM_X += 2;

	//load cache integer, trimming leading bits
	unsigned int value = cache[field_offsets.x >> 5] & (0xFFFFFFFF >> (field_offsets.x & 31));
	
	//if field contained in one int, trim any trailing bits
	if ((field_offsets.x & 31) + field_offsets.y < 33) {
		value = value >> (32 - (field_offsets.x & 31) - field_offsets.y);
	}
	//else merge with next chunk
	else 
	{
		field_offsets.y = (field_offsets.x + field_offsets.y) & 31; 
		value = (value << field_offsets.y) + (cache[1 + (field_offsets.x >> 5)] >> (32 - field_offsets.y)); //shift off end bits
	}
	return value;
}


#pragma endregion


#pragma endregion


__global__ void GpfProcessor(int stream_no)
{
	GpfVm vm(stream_no);
	vm.Gather();
	vm.Filter();
}

#pragma region Host

__host__ void CheckError(const char* string)
{

#ifdef GPF_DEBUG
	cudaDeviceSynchronize();
	cudaError_t error = cudaGetLastError();
	if (error != cudaSuccess)
	{
		printf("%s : %s\n\n", string, cudaGetErrorString(error));
		getchar();
		exit(1);
	}
#endif

}

void ConstantSetup(FilterOptions options)
{
	int tmp;
	/*printf("\nFiltermem host: %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d %d\n\n",
			options.filter_program[0], options.filter_program[1], options.filter_program[2], options.filter_program[3], 
			options.filter_program[4], options.filter_program[5], options.filter_program[6], options.filter_program[7],
			options.filter_program[8], options.filter_program[9], options.filter_program[10], options.filter_program[11], 
			options.filter_program[12], options.filter_program[13], options.filter_program[14], options.filter_program[15]);*/
	
	//unsigned char * dummy = (unsigned char*)malloc(16 + 20);
	//memset(dummy, 0, 3);
	//memcpy(dummy + 3, options.filter_program, 16);
	cudaMemcpyToSymbol(fltr_program, /*dummy*/ options.filter_program, options.filter_program_size);
	cudaMemcpyToSymbol(rule_program, options.rule_program, options.rule_program_size);
	cudaMemcpyToSymbol(lookup, options.lookup_memory, options.lookup_memory_size);
	
	cudaMemcpyToSymbol(_packet_count, (void*) &options.packets_per_buffer, sizeof(int));

	tmp = options.record_length / 4 + (options.record_length % 4 == 0 ? 0 : 1 );
	cudaMemcpyToSymbol(_record_size, (void*) &tmp, sizeof(int));

	cudaMemcpyToSymbol(_start_offset, (void*) &options.record_start, sizeof(int));
		
	cudaMemcpyToSymbol(_rules_per_packet, (void*) &options.rules_per_packet, sizeof(int));
	cudaMemcpyToSymbol(_bool_result_count, (void*) &options.filters_per_packet, sizeof(int));
	cudaMemcpyToSymbol(_int_result_count, (void*) &options.integers_per_packet, sizeof(int));


	cudaMemcpyToSymbol(_layer_count, (void*) &options.layer_count, sizeof(int));
	cudaMemcpyToSymbol(_root_proto, (void*) &options.root_protocol, sizeof(int));
	
	tmp = options.rules_per_packet * options.packets_per_buffer / 32; //in ints
	cudaMemcpyToSymbol(_working_per_stream, (void*) &tmp, sizeof(int));

	tmp = options.rules_per_packet * GPF_THREADS; //in ints
	cudaMemcpyToSymbol(_working_per_block, (void*) &tmp, sizeof(int));

	tmp = options.rules_per_packet * 32; //in ints
	cudaMemcpyToSymbol(_working_per_warp, (void*) &tmp, sizeof(int));

	tmp = options.packets_per_buffer / 32;
	cudaMemcpyToSymbol(_filter_mem_size, (void*) &tmp, sizeof(int));		//size of filter mem per result in ints for a single result
	cudaMemcpyToSymbol(_integer_mem_size, (void*) &options.packets_per_buffer, sizeof(int));		//size of integer mem per result in ints for a single integer

	tmp = options.filter_memory_size() >> 2; 
	cudaMemcpyToSymbol(_filter_per_stream, (void*) &tmp, sizeof(int));
	tmp = options.integer_memory_size() >> 2; 
	cudaMemcpyToSymbol(_integer_per_stream, (void*) &tmp, sizeof(int));

}

void BeginProcess(void * args)
{
	int * packet_data_dev;
	int * filter_mem_dev;
	int * integer_mem_dev;
	int * working_mem_dev;
	
	ProcessArgs * proc = static_cast<ProcessArgs*>(args);
	FilterOptions options = proc->options;
	
	cudaSetDevice(options.gpu_index);
	CheckError("Error setting device.");

	int device;
	cudaGetDevice(&device);
	printf("\n\nDevice: %d\n\n", device);
	//connect to the cuda buffer object
	zmq::socket_t buffer(*proc->context, ZMQ_PAIR);
	buffer.connect("inproc://gpfbuffer");

	zmq::socket_t empty(*proc->context, ZMQ_PAIR);
	empty.connect("inproc://gpfbuffer_return");

	
	for (int k = 0; k < options.total_stream_buffers * options.streams; k++)
	{
		char* tmp;
		cudaHostAlloc((void**) &tmp, options.packet_buffer_size(), cudaHostAllocWriteCombined);
		empty.send(&tmp, sizeof(char*));
	}

	CudaBufferPointer ptr;

	//create an output buffer for vm results
	OutputBuffer output(*proc);

	
	//malloc device memory
	cudaMalloc((void**) &packet_data_dev, options.packet_buffer_size() * options.streams);
	cudaMalloc((void**) &filter_mem_dev, options.filter_memory_size() * options.streams);
	cudaMalloc((void**) &integer_mem_dev, options.integer_memory_size() * options.streams);
	cudaMalloc((void**) &working_mem_dev, options.working_memory_size() * options.streams);
	
	/*int * test = (int *)malloc(options.filter_memory_size() * options.streams);
	unsigned char val[2];
	val[0] = 0x0F;
	val[1] = 0xCC;

	for (int k = 0; k < options.streams;k++)
	{
		for (int j = 0; j < options.filters_per_packet;j++)
		{
			int count = options.packets_per_buffer/8;
			int offset = k * options.filter_memory_size() + j * count;
			memset((char*)test + offset, val[j], count);
		}
	}
	cudaMemcpy(filter_mem_dev, test, options.filter_memory_size() * options.streams, cudaMemcpyHostToDevice);*/
	
	CheckError("Error allocating device memory.");

	//copy pointers to constant memory
	cudaMemcpyToSymbol(packet_data, &packet_data_dev, sizeof(int *));		
	cudaMemcpyToSymbol(filter_mem, &filter_mem_dev, sizeof(int *));	
	cudaMemcpyToSymbol(integer_mem, &integer_mem_dev, sizeof(int *));	
	cudaMemcpyToSymbol(working_mem, &working_mem_dev, sizeof(int *));	

	CheckError("Error copying device memory pointers.");

	ConstantSetup(options);
	
	CheckError("Error during constant setup.");

	cudaDeviceSetCacheConfig(cudaFuncCachePreferL1);
	//cudaDeviceSetLimit(cudaLimitMallocHeapSize, 128 * 1024 * 1024);
	cudaDeviceSetLimit(cudaLimitPrintfFifoSize, 64 * 1024 * 1024);
	
	CheckError("Error during device heap setup.");

	_int64 packet_count = 0;

	int filter_size = options.filter_memory_size();
	int integer_size = options.integer_memory_size();
	
	//create streams
    cudaStream_t *streams = (cudaStream_t*) malloc(options.streams * sizeof(cudaStream_t));
    
	for(int k = 0; k < options.streams; k++) 
	{
        cudaStreamCreateWithFlags(&(streams[k]), cudaStreamNonBlocking);
    }
	CheckError("Error creating streams.");
	
	int** filter_results = (int**) malloc(options.streams * sizeof(int*));
	int** integer_results = (int**) malloc(options.streams * sizeof(int*));

	size_t* stream_size = (size_t*)malloc(options.streams * sizeof(size_t));
	int* packets = (int*)malloc(options.streams * sizeof(int));
	char ** packet_ptrs = (char**)malloc(options.streams * sizeof(char*));

//#ifdef PERFORMANCE_TESTING
//	cudaEvent_t start;
//	cudaEvent_t* events = (cudaEvent_t*)malloc(sizeof(cudaEvent_t) * options.streams * 4);
//	GpfTimer timer("H:\\testing.csv", &options);
//
//	for (int k = 0; k < options.streams * 4; k++)
//	{
//		cudaEventCreate(&events[k]);
//	}
//
//	cudaEventCreate(&start);
//	cudaEventRecord(start, 0);
//
//#endif

	do
	{
		
		int usedStreams = 0;
		for (int k = 0; k < options.streams; k++)
		{
			buffer.recv(&ptr, sizeof(CudaBufferPointer));	//get next full write-combined buffer

			if (ptr.size == 0) break; //skip if stream is empty
			
			usedStreams++;
			stream_size[k] = ptr.size;
			packet_ptrs[k] = ptr.buffer;
			packets[k] = ((int)ptr.size) / options.record_length;

			packet_count += packets[k];
		
			//if nonstandard packet count, update constant memory (final iteration)
			if (packets[k] != options.packets_per_buffer)	
			{
				cudaMemcpyToSymbolAsync(_packet_count, &packets[k], sizeof(int), 0, cudaMemcpyHostToDevice, 0); //copy in default stream so other kernels finish first
				//CheckError("Error resetting const memory.");
			}
			
			char * dst = ((char*) packet_data_dev) + k * options.packet_buffer_size();
			
//#ifdef PERFORMANCE_TESTING
//			//issue
//			cudaEventRecord(events[k * 4], streams[k]);
//#endif
			//copy records to the device - async
			cudaMemcpyAsync(dst, ptr.buffer, ptr.size, cudaMemcpyHostToDevice, streams[k]);
			//CheckError("Error streaming packet data.");
		
			
			int blocks = static_cast<int>(ceil(static_cast<double>(packets[k]) / PACKETS_PER_BLOCK));
				
//#ifdef PERFORMANCE_TESTING
//			//load
//			cudaEventRecord(events[k * 4 + 1], streams[k]);
//#endif
			
			//process stream contents - async
			GpfProcessor<<<blocks, GPF_THREADS, 0, streams[k]>>>(k);
			//CheckError("Error in vm.");
						
			cudaStreamSynchronize(streams[k]); //shouldnt be necessary? but seems to prevent corruption on smaller captures
//#ifdef PERFORMANCE_TESTING
//			//process
//			cudaEventRecord(events[k * 4 + 2], streams[k]);
//#endif
			int* tmp;
			if (options.filters_per_packet > 0)  
			{	
				tmp = output.GetFilterBuffer();

				/*cudaHostAlloc((void**) &tmp, filter_size, cudaHostAllocDefault); 
				CheckError("Error allocating host filter output buffers.");*/
				
				cudaMemcpyAsync(tmp, (char*)(filter_mem_dev) + k * filter_size, filter_size, cudaMemcpyDeviceToHost, streams[k]);
				//CheckError("Error copying host filter output buffers.");


				filter_results[k] = tmp;
			}
			if (options.integers_per_packet > 0) 
			{
				tmp = output.GetFieldBuffer();
				/*cudaHostAlloc((void**) &tmp, integer_size, cudaHostAllocDefault); 
				CheckError("Error allocating host integer output buffers.");*/

				cudaMemcpyAsync(tmp, (char*)(integer_mem_dev) + k * integer_size, integer_size, cudaMemcpyDeviceToHost, streams[k]);
				//CheckError("Error copying host integer output buffers.");

				integer_results[k] = tmp;
			}
										
//#ifdef PERFORMANCE_TESTING
//			//return
//			cudaEventRecord(events[k * 4 + 3], streams[k]);
//#endif
			if (!ptr.more) break;
		}			
		
		for (int k = 0; k < usedStreams; k++)
		{
			cudaStreamSynchronize(streams[k]);
			//CheckError("Error synchronizing stream.");
			//free the packet buffer as it is no longer needed
			char* tmp = packet_ptrs[k];
			empty.send(&tmp, sizeof(char*));

			if (options.filters_per_packet > 0) output.CopyFilterResults(filter_results[k], filter_size, packets[k]);
			//CheckError("Error copying to host filter output buffers.");

			if (options.integers_per_packet > 0) output.CopyIntegerResults(integer_results[k], integer_size, packets[k]);
			//CheckError("Error copying to host integer output buffers.");
													
//#ifdef PERFORMANCE_TESTING
//			//return
//			float issue;
//			float load;
//			float process;
//			float conclude;
//
//			cudaEventElapsedTime(&issue, start, events[k * 4]);
//			cudaEventElapsedTime(&load, events[k * 4], events[k * 4 + 1]);
//			cudaEventElapsedTime(&process, events[k * 4 + 1], events[k * 4 + 2]);
//			cudaEventElapsedTime(&conclude, events[k * 4 + 2], events[k * 4 + 3]);
//
//			timer.Record(packets[k], k, issue, load, process, conclude);
//#endif

		}

	} while (ptr.more);
	//complete

	output.Finished(packet_count);

	//malloc device memory
	cudaFree((void*)packet_data_dev);
	cudaFree((void*)filter_mem_dev);
	cudaFree((void*)integer_mem_dev);

	
	for(int k = 0; k < options.streams; k++) 
	{
        cudaStreamDestroy(streams[k]);
    }

	free(stream_size);
	free(packets);
	free(packet_ptrs);

	free(streams);

	empty.close();
	buffer.close();
}

//launches a new vm thread with the prescribed filter options
void GpfVmLauncher(zmq::context_t * zmq_context, FilterOptions filter_options)
{
	//create argument for vm thread
	ProcessArgs * args = static_cast<ProcessArgs*>(malloc(sizeof(ProcessArgs)));
	args->context = zmq_context;
	args->options = filter_options;

	//issue thread
	tthread::thread* proc = new tthread::thread(BeginProcess, (void*) args);
}


#pragma endregion


//GpfTimer::GpfTimer(char* output_file, FilterOptions* options)
//{
//	streams = options->streams;
//	gpu = options->gpu_index;
//	filename = output_file;
//	bool exists;
//
//	if (FILE *file = fopen(filename, "r")) {
//        fclose(file);
//        exists = true;
//    } else exists = false;
//    
//	fopen_s(&file, filename, "a");
//
//	if (exists)	fprintf(file, "Packet Count,Stream ID,Issue Time,Host To Device,Classification,Device To Host\n");
//	
//	fprintf(file, "\nStream Count,%d\nGPU Index,%d\n",streams,gpu);
//}
//GpfTimer::~GpfTimer()
//{
//	fflush(file);
//	fclose(file);
//	//free(filename);
//}
//void GpfTimer::Record(int packet_count, int stream_id, float issueTime, float packetCopy, float packetProcess, float resultCopy)
//{
//	fprintf(file, "%d,%d,%f,%f,%f,%f\n", packet_count, stream_id, issueTime, packetCopy, packetProcess, resultCopy);
//}