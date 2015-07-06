#pragma once
#include "Distiller.h"

class PcapWriter
{
	bool filter;
	char * filter_data;
	char * write_buffer;
	FILE * filter_file;
	FILE * out_file;
	int curr_index;
	int write_offset;
	
public:
	PcapWriter(DistillerOptions * options, char * header);
	void AddPacket(char * ptr, unsigned int length);
	void LoadRange(_int64 index, int count);
	void Close();

};