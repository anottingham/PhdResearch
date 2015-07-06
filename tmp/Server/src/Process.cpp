#include "zmq.hpp"
//#include "FileBuffer.h"
//#include "ProgressBar.h"
#include <ctime>
#include "GpfServer.h"
//
//void test1() //passed
//{
//	int file_count = 2;
//	char** filenames = (char**)malloc(sizeof(char*) * file_count);
//	char* ptr;
//	size_t length;
//	size_t curr_pos = 0;
//	
//	filenames[0] = (char*)malloc(sizeof(char) * 256);
//	filenames[1] = (char*)malloc(sizeof(char) * 256);
//	strcpy(filenames[0], "K:\\large.cap");
//	strcpy(filenames[1], "M:\\large.cap");
//
//	FileBuffer buffer;
//	_int64 size;
//	try{
//		size = buffer.Initialise(file_count, filenames);
//	}
//	catch (zmq::error_t err)
//	{
//		printf("ZMQ error");
//	}
//
//	ProgressBar bar(size);
//	
//	clock_t start = clock();
//	
//	FILE * file;
//	errno_t error = fopen_s(&file, "L:\\large.cap", "rb"); //open file
//	if (error != 0) { printf("File could not be opened."); }	
//	
//	int total_blocks = (size / (512*1024)) + (size % (512*1024) == 0 ? 0 : 1);
//	for (int k = 0; k < total_blocks; k++)
//	{
//		char* comp = (char*) malloc((512*1024));
//		size_t bytes_read = fread(comp, sizeof(char), (512*1024), file);
//		if (bytes_read != (512*1024) && k + 1 != total_blocks)
//		{
//			if (feof(file))
//			{
//			printf("EOF error");
//			}
//			if (ferror(file)) //handle
//			{
//			printf("READ error");
//			}
//		}
//				
//			buffer.Next(ptr, length);
//			printf("ZMQ error");
//
//		bool pause = false;
//
//		if (length != bytes_read) 
//		{
//			printf("Block [%d] : Length mismatch - %d != %d\n", k, length, bytes_read); 
//			getchar();
//		}
//		for (int j = 0; j < length; j++)
//		{
//			if (ptr[j] != comp[j])
//			{
//				printf("Block [%d, %d] : %X != %X\n", k,j, ptr[j], comp[j]);
//				pause = true;
//			}
//		}
//		if (pause) getchar();
//		if (ptr != nullptr) free(ptr);
//		if (comp != nullptr) free (comp);
//		curr_pos += length;
//		bar.Update(length);
//
//	}
//	
//	clock_t stop = clock();
//	float time = (stop - start) / (float) (1000);
//	size_t data = curr_pos / (1024 * 1024);
//
//	printf("\nComplete: Processed %d MB of data in %.3f seconds (%.1f MB/s Avg).\n\n", data, time, data/time);
//}
//
//void test2() 
//{
//	int file_count = 3;
//	char** filenames = (char**)malloc(sizeof(char*) * file_count);
//	char* ptr;
//	size_t length;
//	size_t curr_pos = 0;
//	
//	filenames[0] = (char*)malloc(sizeof(char) * 256);
//	filenames[1] = (char*)malloc(sizeof(char) * 256);
//	filenames[2] = (char*)malloc(sizeof(char) * 256);
//	strcpy(filenames[0], "K:\\large.cap");
//	strcpy(filenames[1], "L:\\large.cap");
//	strcpy(filenames[2], "M:\\large.cap");
//
//	capture_io::FileBuffer buffer;
//	_int64 size;
//	try{
//		size = buffer.Initialise(file_count, filenames);
//	}
//	catch (zmq::error_t err)
//	{
//		printf("ZMQ error");
//	}
//
//	ProgressBar bar(size);
//			
//	while(buffer.Next(ptr, length))
//	{
//		if (ptr != nullptr) free(ptr);
//		curr_pos += length;
//		bar.Update(length);
//	}
//
//	
//	bar.Finished();
//}


int main () {
	

    //Wait for signal
	GpfServer server;

	server.Start();

	getchar();
    return 0;
}