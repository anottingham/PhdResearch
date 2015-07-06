
#include "zmq.hpp"
#include "thrust\device_vector.h"
#include "thrust\reduce.h"
#include "thrust\sort.h"
#include <thrust\fill.h>
#include <thrust\execution_policy.h>

#include "GpfViewUtil.cuh"
#include "Distiller.h"


__global__ void CountGlobal(int* filterSegment, int segmentSize, unsigned long long int* resultPtr)
{
	int count = (blockDim.x * blockIdx.x + threadIdx.x < segmentSize ? __popc(filterSegment[blockDim.x * blockIdx.x + threadIdx.x]) : 0);
	
	count += __shfl_xor(count, 16);
	count += __shfl_xor(count, 8);
	count += __shfl_xor(count, 4);
	count += __shfl_xor(count, 2);
	count += __shfl_xor(count, 1);

	if ((threadIdx.x & 31) == 0) atomicAdd(resultPtr, count);
}


#define VIEW_UTIL_COUNT 0
#define VIEW_UTIL_DISTILL 1
#define VIEW_UTIL_EXIT 2

void CountProcess(zmq::socket_t& socket)
{
	unsigned long long int*  count_ptrs_dev;
	int * dev_ptr;
	int total_segments;
	int max_seg_size;
	int device;
	/*int streams;
	int stream_segments;*/
		
	socket.recv(&device, sizeof(int));
	cudaSetDevice(device);
	cudaDeviceReset();

	socket.recv(&total_segments, sizeof(int));

	//if (total_segments > 128)
	//{
	//	streams = 128;
	//	stream_segments = 1 + total_segments / 128;
	//}
	//else 
	//{
	//	streams = total_segments;
	//	stream_segments = 1;
	//}

	cudaStream_t * streams = (cudaStream_t *)malloc(total_segments * sizeof(cudaStream_t));

	for (int k = 0; k < total_segments; k++) cudaStreamCreate(&(streams[k]));
	
	int count_mem_size = total_segments * sizeof(unsigned long long int);
	cudaMalloc((void **) &count_ptrs_dev, count_mem_size);
	
	socket.recv(&max_seg_size, sizeof(int));
	cudaMalloc((void**) &dev_ptr, max_seg_size * total_segments);

	cudaMemsetAsync(count_ptrs_dev, 0, count_mem_size, 0);

	int max_size_ints = max_seg_size / 4;

	for (int k = 0; k < total_segments; k++)
	{
		zmq::message_t msg;
		socket.recv(&msg);

		int * ptr = dev_ptr + k * max_size_ints;
		cudaMemcpyAsync(ptr, msg.data(), msg.size(), cudaMemcpyHostToDevice, streams[k]);

		int msg_size_int = msg.size() / 4;
		int blocks = msg_size_int / 128 + (msg_size_int % 128 == 0 ? 0 : 1);

		CountGlobal<<<blocks, 128, 0, streams[k]>>>(ptr, msg_size_int, count_ptrs_dev + k);
	}
	
	int * count_ptrs_host = (int *) malloc(count_mem_size);
	
	cudaDeviceSynchronize();
	
	cudaMemcpy(count_ptrs_host, count_ptrs_dev, count_mem_size, cudaMemcpyDeviceToHost);

	socket.send(count_ptrs_host, count_mem_size);
	
	for (int k = 0; k < total_segments; k++) cudaStreamDestroy(streams[k]);

	free(streams);
	cudaFree(dev_ptr);
	cudaFree(count_ptrs_dev);
	free(count_ptrs_host);
}


//
//void ReduceProcess(zmq::socket_t& socket)
//{
//	int total_segments;
//	int segment_size;
//
//	socket.recv(&total_segments, sizeof(int));
//	socket.recv(&segment_size, sizeof(int));
//	
//	int* keys_in_raw;
//	int* values_in_raw;
//	int* keys_out_raw;
//	int* values_out_raw;
//
//	int* values_in_host;
//	int* keys_out_host;
//	int* values_out_host;
//
//	cudaHostAlloc((void**)&values_in_host, segment_size * sizeof(int), cudaHostAllocWriteCombined);
//	cudaMallocHost((void**)&keys_out_host, segment_size * sizeof(int));
//	cudaMallocHost((void**)&values_out_host, segment_size * sizeof(int));
//
//	cudaMalloc((void**)&keys_in_raw, segment_size * sizeof(int));
//	cudaMalloc((void**)&values_in_raw, segment_size * sizeof(int));
//	cudaMalloc((void**)&keys_out_raw, segment_size * sizeof(int));
//	cudaMalloc((void**)&values_out_raw, segment_size * sizeof(int));
//
//	for (int k = 0; k < segment_size; k++)
//	{
//		values_in_host[k] = 1;
//	}
//	
//	cudaMemcpy(values_in_raw, values_in_host, segment_size * sizeof(int), cudaMemcpyHostToDevice);
//	
//	/*cudaStream_t * streams = (cudaStream_t *)malloc(total_segments * sizeof(cudaStream_t));
//	for (int k = 0; k < total_segments; k++) cudaStreamCreate(&(streams[k]));*/
//	
//	for (int k = 0; k < total_segments; k++)
//	{
//		zmq::message_t msg;
//		socket.recv(&msg);
//
//		int * data = (int*)msg.data();
//		int count = msg.size() / sizeof(int);
//
//		cudaMemcpy(keys_in_raw, msg.data(), msg.size(), cudaMemcpyHostToDevice);
//		
//		if (k + 1 == total_segments)
//		{
//			cudaMemset(keys_out_raw, 0, msg.size());
//			cudaMemset(values_out_raw, 0, msg.size());
//		}
//
//		thrust::device_ptr<int> keys_in(keys_in_raw);
//		thrust::device_ptr<int> values_in(values_in_raw);
//		thrust::device_ptr<int> keys_out(keys_out_raw);
//		thrust::device_ptr<int> values_out(values_out_raw);
//
//		thrust::sort(keys_in, keys_in + count);
//		thrust::pair<thrust::device_ptr<int>,thrust::device_ptr<int>> result = thrust::reduce_by_key(keys_in, keys_in + count, values_in, keys_out, values_out);
//		
//		thrust::copy(keys_out, result.first, keys_out_host);
//		thrust::copy(values_out, result.second, values_out_host);
//		
//		socket.send(keys_out_host, segment_size * sizeof(int));
//		socket.send(values_out_host, segment_size * sizeof(int));
//	}
//		
//	/*for (int k = 0; k < total_segments; k++) cudaStreamDestroy(streams[k]);
//	free(streams);*/
//}


void GpfViewUtil(zmq::context_t& context, int port)
{
	zmq::socket_t socket(context, ZMQ_PAIR);
	char * str = (char*)malloc(25);
	sprintf(str, "tcp://*:%d\0",port);
	socket.bind(str);
	bool running = true;
	
	while(running)
	{
		int command;
		socket.recv(&command, sizeof(int));

		switch (command)
		{
		case VIEW_UTIL_COUNT:
			CountProcess(socket);	
			break;
		case VIEW_UTIL_DISTILL:
			Distiller(context, socket);
			break;
		case VIEW_UTIL_EXIT:
			running = false;
			break;
		default: 
			break;
		}

	}
	socket.close();
	free(str);
}