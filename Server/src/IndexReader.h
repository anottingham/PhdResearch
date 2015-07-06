#pragma once


class IndexReader
{
	_int64 * index_data;
	FILE * index_file;
	int curr_index;

public:
	IndexReader(DistillerOptions * options);
	~IndexReader();
	void LoadRange(_int64 index, int count);
	_int64 GetNext();
	_int64 PeekNext();

};