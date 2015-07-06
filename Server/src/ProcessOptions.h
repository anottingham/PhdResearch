#pragma once
#include "FileBuffer.h"
#include "CudaBuffer.h"
#include "IndexBuffer.h"


struct ProcessOptions
{
	FileOptions file;
	FilterOptions filter;
	IndexOptions index;

	void SetFileOptions(int file_count, char** file_names)
	{
		file.file_count = file_count;
		file.file_names = file_names;
	}

	void SetFilterOptions(char* out_folder, int record_start, int record_length, int packets_per_buffer)
	{
		filter.enabled = true;
		filter.out_folder = out_folder;
		filter.record_start = record_start;
		filter.record_length = record_length;
		filter.packets_per_buffer = packets_per_buffer;
	}

	void SetIndexOptions(char* pidx, char* tidx)
	{
		index.enabled = true;
		index.pidx_file = pidx;
		index.tidx_file = tidx;
	}
};