#include "Distiller.h"
#include "IndexReader.h"

#define INDEX_BUFFER_SIZE_MB	16
#define INDEX_BUFFER_SIZE		(INDEX_BUFFER_SIZE_MB * 1024 * 1024)


IndexReader::IndexReader(DistillerOptions * options)
{
	errno_t error = fopen_s(&index_file, options->index_file, "rb"); //open file
	index_data  = (_int64*)malloc(INDEX_BUFFER_SIZE);
}

IndexReader::~IndexReader()
{
	fclose(index_file);
	free(index_data);
}

void IndexReader::LoadRange(_int64 index, int count)
{
	//to byte index
	_int64 byteIndex = (1 + index) * sizeof(_int64);
	_fseeki64(index_file, byteIndex, SEEK_SET);
	fread(index_data, sizeof(_int64), count, index_file);
	curr_index = 0;
}

_int64 IndexReader::GetNext()
{
	return index_data[curr_index++];
}

_int64 IndexReader::PeekNext()
{
	return index_data[curr_index];
}