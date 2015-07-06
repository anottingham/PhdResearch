
#include "Distiller.h"
#include "PcapWriter.h"
#include "Common.h"

#define FILTER_CHUNK_SIZE_MB 8
#define FILTER_CHUNK_SIZE (FILTER_CHUNK_SIZE_MB * 1024 * 1024)

#define WRITE_BUFFER_SIZE_MB 8
#define WRITE_BUFFER_SIZE  (WRITE_BUFFER_SIZE_MB * 1024 * 1024)

PcapWriter::PcapWriter(DistillerOptions * options, char * header)
{
	if (options->filter)
	{
		filter = true;
		errno_t error = fopen_s(&filter_file, options->filter_file, "rb"); //open file
		if (error != 0) { POST_ERROR("Distiller Writer Thread", "Filter file could not be opened."); }	
		filter_data  = (char*)malloc(FILTER_CHUNK_SIZE);
	}
	else filter = false;
	
	//create output file
	errno_t error = fopen_s(&out_file, options->out_name, "wb"); //open file
	if (error != 0) { POST_ERROR("Distiller Writer Thread", "Output file could not be opened."); }	
	
	fwrite(header, sizeof(char), 24, out_file);	//records unknown
	write_buffer= (char*)malloc(WRITE_BUFFER_SIZE);
	write_offset = 0;
}


void PcapWriter::LoadRange(_int64 index, int count)
{
	if (!filter) return;

	curr_index = index & 7;

	_int64 byteOffset = 272 + (index>>3);
	_int64 byteLength = ((curr_index + count)>>3) + (((curr_index + count) & 7) == 0 ? 0 : 1);

	_fseeki64(filter_file, byteOffset, SEEK_SET);
	fread(filter_data, sizeof(char), byteLength, filter_file);
}

void PcapWriter::AddPacket(char * ptr, unsigned int length)
{
	if (filter && ((filter_data[curr_index>>3] & (0x80 >> (curr_index++ & 7)))) == 0) return;
	
	if (write_offset + length + 16 >= WRITE_BUFFER_SIZE)
	{
		fwrite(write_buffer, sizeof(char), write_offset, out_file); //include packet header
		write_offset = 0;
	}

	memcpy(write_buffer + write_offset, ptr, 16 + length);
	write_offset += 16 + length;
	
}

void PcapWriter::Close()
{
	if (write_offset > 0)
	{
		fwrite(write_buffer, sizeof(char), write_offset, out_file); //include packet header
	}
	//fflush(out_file);
	fclose(out_file);
	free(write_buffer);
	if (filter)
	{
		free(filter_data);
		fclose(filter_file);
	}
}