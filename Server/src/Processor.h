#pragma once
#include "ProcessOptions.h"
#include "zmq.hpp"

class ProgressBar;
class FileBuffer;
class CudaBuffer;
class IndexBuffer;



class Processor
{
	FileBuffer * file_buffer;
	ProgressBar * progress;
	CudaBuffer * cuda_buffer;
	IndexBuffer * index_buffer;

	zmq::context_t * context;
	void BeginFileRead(FileOptions file);
	void ProcessPacket();

public:
	
	ProcessOptions options;

	Processor(zmq::context_t * zmq_context, ProcessOptions opt);
	void Stop();
	
	void Start();
	
	FileBuffer * GetFileBuffer() { return file_buffer; }
	ProgressBar * GetProgressBar() { return progress; }
	CudaBuffer * GetCudaBuffer() { return cuda_buffer; }
	IndexBuffer * GetIndexBuffer() {return index_buffer; }
	
};