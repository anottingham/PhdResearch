#include "CudaBuffer.h"
#include "cuda_runtime.h"
#include "Common.h"

CudaBuffer::CudaBuffer(zmq::context_t * zmq_context, FilterOptions options)
{
	context = zmq_context;
	curr_record = 0;
	record_length = options.record_length;
	record_start = options.record_start;
	packets_per_buffer = options.packets_per_buffer;
	total_packets = 0;

	buffer_size = packets_per_buffer * record_length;

	socket = new zmq::socket_t(*context, ZMQ_PAIR);
	socket->bind("inproc://gpfbuffer");

	cudaHostAlloc((void**) &cuda_buffer, buffer_size, cudaHostAllocWriteCombined);
}

CudaBuffer::~CudaBuffer()
{
	socket->close();
}

void CudaBuffer::CopyPacket(char* buffer, int packet_length)
{
	int cropped_length = packet_length - record_start;

	if (record_length <= cropped_length) 
	{
		memcpy(cuda_buffer + (curr_record++ * record_length), buffer + record_start, record_length);
	}
	else
	{
		memcpy(cuda_buffer + (curr_record   * record_length), buffer + record_start, cropped_length);	//copy all packet data
		memset(cuda_buffer + (curr_record++ * record_length) + cropped_length, 0, record_length - cropped_length);	//set remaining bytes to zero
	}

	if (curr_record == packets_per_buffer) //if buffer full
	{
		CudaBufferPointer ptr;
		ptr.buffer = cuda_buffer;
		ptr.size = curr_record * record_length;
		ptr.more = true;

		socket->send(&ptr, sizeof(CudaBufferPointer));

		cudaHostAlloc((void**) &cuda_buffer, buffer_size, cudaHostAllocWriteCombined);
		curr_record = 0;
	}
}

void CudaBuffer::Finished()
{
	CudaBufferPointer ptr;
	ptr.buffer = cuda_buffer;
	ptr.size = curr_record * record_length;
	ptr.more = false;

	socket->send(&ptr, sizeof(CudaBufferPointer));
}