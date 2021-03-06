#include "Processor.h"
#include "Common.h"
#include "FileBuffer.h"
#include "ProgressBar.h"
#include "CudaBuffer.h"
#include "IndexBuffer.h"
#include "tinythread.h"
#include "GpfVm.cuh"

#define BLOCK_SIZE (BLOCK_SIZE_MB * 1024 * 1024)

void Process(void* args);
void FakeProcess(void* arg);

Processor::Processor(zmq::context_t * zmq_context, ProcessOptions opt)
{ 
	context = zmq_context;
	options = opt;
	
	file_buffer = new FileBuffer(context);
	cuda_buffer = new CudaBuffer(context, options.filter);
	
	if (options.filter.enabled)
	{
		GpfVmLauncher(context, options.filter);
		cuda_buffer->Initialise();
	}
	if (options.index.enabled)
	{
		index_buffer = new IndexBuffer(context, options.index); 
	}
}
void Processor::Stop()
{
	file_buffer->Close();
	if (options.filter.enabled) cuda_buffer->Close();
}


void Processor::BeginFileRead(FileOptions file)
{
	_int64 size = file_buffer->Initialise(file);
	progress = new ProgressBar(context, size);
}

void Processor::Start()
{
	BeginFileRead(options.file);
	tthread::thread * proc = new tthread::thread(Process, this);
}

void Process(void* arg)
{
	Processor * processor = static_cast<Processor*>(arg);
	ProcessOptions options = processor->options;

	FilterOptions filter = options.filter;
	IndexOptions index = options.index;

	ProgressBar * progress = processor->GetProgressBar();
	FileBuffer * file_buffer = processor->GetFileBuffer();
	CudaBuffer * cuda_buffer = processor->GetCudaBuffer();
	IndexBuffer * index_buffer = processor->GetIndexBuffer();


	char* buffer;
	char* overflow;

	bool reversed = false;
	bool partial_record = false;
	unsigned int snap_len;
	unsigned int start_time = 0;
	
	_int64 global_index = 24;	//the global byte index position in the file
	unsigned int local_index = 0;		//local byte index in the current buffer
	unsigned int buffer_count = 0;		//number of buffers already processed
	unsigned int length = 0;			//length of current packet
	unsigned int curr_time = 0;
	unsigned int used_bytes = 16 + (filter.enabled ? filter.record_start + filter.record_length : 0);	//the number of bytes used in each packet
	
	size_t buffer_size;
	//parse global header
	file_buffer->Next(buffer, buffer_size);
	progress->Update(buffer_size);

	unsigned int magic;
	memcpy(&magic, buffer, sizeof(unsigned int));	//get capture header magic number to infer byte order
	//inspect header to ensure file is in valid format, and determine header byte order
	if (magic == 0xd4c3b2a1)	{
		reversed = true;
	}
	else if (magic != 0xa1b2c3d4) {
		POST_ERROR("Processor.Process() Magic Number Test", "Capture File damaged or format incorrect.");
	}


	memcpy(&snap_len, buffer +  16, 4);
	if (reversed) snap_len = _byteswap_ulong(snap_len);
	overflow = (char*)malloc(snap_len);

	if (snap_len < filter.record_length) filter.record_length = snap_len;

	//get start time
	memcpy(&start_time, buffer + 24, sizeof(unsigned int));
	if (reversed) start_time = _byteswap_ulong(start_time);

	if (index.enabled) index_buffer->SetStartTime(start_time);

	int packets = 0;
	//read and process buffers
	do
	{
		if (partial_record)
		{
			if (length == 0) 
			{
				//copy record header if needed
				if (BLOCK_SIZE - local_index < 16) 
				{
					memcpy(overflow + (BLOCK_SIZE - local_index), buffer, 16 + BLOCK_SIZE - local_index);
				}

				memcpy(&length, overflow + 8, sizeof(unsigned int));
				if (reversed) length = _byteswap_ulong(length);
				
				memcpy(overflow + 16, buffer, (local_index + 16 + length) % BLOCK_SIZE);
			}
			//copy remaining data into the temp buffer (local index < buffer size) - as partial record, block size is guarenteed 
			else memcpy(overflow + (BLOCK_SIZE - local_index), buffer, (local_index + 16 + length) % BLOCK_SIZE);
							
			if (index.enabled) 
			{
				memcpy(&curr_time, overflow, sizeof(unsigned int)); //get arrival time second
				if (reversed) curr_time = _byteswap_ulong(curr_time);
				index_buffer->CopyIndexData(global_index, curr_time);
			}
		
			if (filter.enabled) cuda_buffer->CopyPacket(overflow + 16, length);
			global_index += 16 + length;
			partial_record = false;

			length = 0;
		}
		local_index  = static_cast<_int64>(global_index % BLOCK_SIZE);
		global_index -= local_index;
		
		//while the buffer contains additional complete packet data
		while (local_index + 16 < buffer_size)
		{
			memcpy(&length, buffer + local_index + 8, sizeof(unsigned int)); 		//copy length from header (uint 3)
			if (reversed) length = _byteswap_ulong(length);
		
			if (local_index +  (int)min(16 + length, used_bytes) >= buffer_size) break;
			//copy packet data to cuda buffer
			if (filter.enabled) cuda_buffer->CopyPacket(buffer + local_index + 16, length);
			if (index.enabled)
			{
				memcpy(&curr_time, buffer + local_index, sizeof(unsigned int)); //get arrival time second
				if (reversed) curr_time = _byteswap_ulong(curr_time);
				index_buffer->CopyIndexData(global_index + local_index, curr_time);
			}

			//skip to next packet
			local_index += 16 + length;
			length = 0;
		}
		
		
		if (local_index < buffer_size && buffer_size == BLOCK_SIZE)
		{
			partial_record = true;
			//copy all bytes residing in buffer to temporary storage
			memcpy(overflow, static_cast<char*>(buffer) + local_index, buffer_size - local_index);
		}
		file_buffer->ReturnBuffer(buffer);
		global_index += local_index;	//update global index to correct current value
		file_buffer->Next(buffer, buffer_size);
		progress->Update(buffer_size);

	} while(buffer_size != 0);
	if (filter.enabled) {
		cuda_buffer->Finished();
	}
	if (index.enabled) 
	{
		index_buffer->Finished(global_index);
	}
	progress->Finished();
}


//void FakeProcess(void* arg)
//{
//	Processor * processor = static_cast<Processor*>(arg);
//	ProcessOptions options = processor->options;
//
//	
//	ProgressBar * progress = processor->GetProgressBar();
//	FileBuffer * file_buffer = processor->GetFileBuffer();
//
//	char* buffer;
//	
//	
//	size_t buffer_size;
//	//parse global header
//	bool f;
//	file_buffer->Next(buffer, buffer_size);
//	progress->Update(buffer_size);
//
//
//
//	//read and process buffers
//	do
//	{		
//		free(buffer);// - (BLOCKS_PER_ALLOC - 1)*BLOCK_SIZE);
//		file_buffer->Next(buffer, buffer_size);
//		progress->Update(buffer_size);
//
//	} while(buffer_size != 0);
//	progress->Finished();
//}