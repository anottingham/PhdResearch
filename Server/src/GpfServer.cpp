#include "GpfServer.h"
#include "zhelpers.hpp"
#include "zmq.hpp"
#include "stdlib.h"
#include "tinythread.h"
#include "Processor.h"
#include "Common.h"
#include "cuda_runtime.h"

#include "GpfVm.cuh"
#include "GpfViewUtil.cuh"

#define GPF_SVR_RESET -1
#define GPF_SVR_START 0
#define GPF_SVR_INDEX 1
#define GPF_SVR_FILTER 2
#define GPF_SVR_FILE 3
#define GPF_SVR_POLL 4
#define GPF_SVR_VIEW 5
#define GPF_SVR_CONNECT 1234


void GpfServer::Start(int port)
{
	zmq::socket_t client_socket(context, ZMQ_REP);
	zmq::socket_t output_socket(context, ZMQ_PAIR);
	zmq::socket_t index_socket(context, ZMQ_PAIR);
	char * str = (char*)malloc(25);
	sprintf(str, "tcp://*:%d\0",port);
	client_socket.bind(str);
	output_socket.bind("inproc://complete_filter");
	index_socket.bind("inproc://complete_index");

	printf("GPF Server v0.1\n---------------\n\n");
	
	printf("Waiting for client...\n");
	ClientConnect(client_socket);
	while(true)
	{
		ClientProcess(client_socket, output_socket, index_socket);
	}
	
}

void GpfServer::ClientConnect(zmq::socket_t& socket)
{
	int command;
	while(true)
	{
		socket.recv(&command, sizeof(int));
		if (command == GPF_SVR_CONNECT)
		{			
			socket.send(&command, sizeof(int), ZMQ_SNDMORE); //send command back to indicate it has been received
			printf("Connected to client.\n");
			printf("Sending GPU Data...\n");

			int count;
			cudaGetDeviceCount(&count);
			socket.send(&count, sizeof(int), ZMQ_SNDMORE);
			
			for (int k = 0; k < count; k++)
			{
				cudaDeviceProp prop;
				cudaGetDeviceProperties(&prop, k);

				char * string = (char *) malloc(256);
				sprintf(string, "%s (%d MB) - CC %d.%d ", prop.name, prop.totalGlobalMem / (1024 * 1024),prop.major,prop.minor);
				int len = strlen(string);
				string[len++] = '\0';
				socket.send(string, len, ZMQ_SNDMORE);
			}
			
			socket.send(&count, sizeof(int));

			socket.recv(&command, sizeof(int));
			
			if (command == count) 
			{
				command = GPF_SVR_CONNECT;
				socket.send(&command, sizeof(int));
				break;
			}
			
			printf("GPU Data - Invalid Response: %d\n", command);
			command = -1;
			socket.send(&command, sizeof(int));
			
		}
		else if (command == GPF_SVR_RESET) //reset
		{
			socket.send(&command, sizeof(int));
			//do nothing - no state to reset

		}
		else 
		{
			printf("Malformed request received: %d\n", command);
			command = -1;
			socket.send(&command, sizeof(int));
		}
	}
}

void GpfServer::ClientProcess(zmq::socket_t& socket, zmq::socket_t& complete_filter, zmq::socket_t& complete_index)
{
	int command = GPF_SVR_CONNECT;
	ProcessOptions options;
	bool filter_enabled = false;
	bool index_enabled = false;


	while (command != GPF_SVR_RESET && command != GPF_SVR_START && command != GPF_SVR_VIEW)
	{
		socket.recv(&command, sizeof(int));

		switch(command)
		{	
		case GPF_SVR_RESET:
		case GPF_SVR_VIEW:
		case GPF_SVR_START:
			break;
		case GPF_SVR_INDEX:
			options.index = GetIndexOptions(socket);
			index_enabled = true;
			break;
		case GPF_SVR_FILTER:
			options.filter = GetFilterOptions(socket);
			filter_enabled = true;
			break;
		case GPF_SVR_FILE:
			options.file = GetFileOptions(socket);
			break;
		case GPF_SVR_CONNECT: 
		default:
			command = -1;
			socket.send(&command, sizeof(int));
			//force reset
			command = GPF_SVR_RESET;
			break;
		}
	}
	if (command == GPF_SVR_VIEW)
	{
		socket.send(&command, sizeof(int)); //confirm action
		socket.recv(&command, sizeof(int)); //get port
		socket.send(&command, sizeof(int)); //confirm port

		GpfViewUtil(context, command); //change to count monitor
	}
	else if (command == GPF_SVR_START)
	{
		StartProcess(socket, options);
		int response;
		
		if (filter_enabled) 
		{
			complete_filter.recv(&response, sizeof(int));
			if (response != 0) 
			{
				socket.recv(&response, sizeof(int));
				response = -1;
				printf("Error receiving output completion notice from output writer.");
				socket.send(&response, sizeof(int));
				getchar();
				exit(1);
			}
		}
		
		if (index_enabled) 
		{
			complete_index.recv(&response, sizeof(int));
			if (response != 0) 
			{
				socket.recv(&response, sizeof(int));
				response = -1;
				printf("Error receiving index completion notice from index writer.");
				socket.send(&response, sizeof(int));
				getchar();
				exit(1);
			}
		}

		socket.recv(&response, sizeof(int));
		if (response != 1234)
		{
			printf("Error communicating with client: unexpected response during post processing cleanup.");
			getchar();
			exit(1);
		}
			
		socket.send(&response, sizeof(int));
	}
}


void GpfServer::StartProcess(zmq::socket_t& socket, ProcessOptions options)
{
	zmq::socket_t progress(context, ZMQ_PAIR);
	progress.bind("inproc://progress");

	Processor * proc = new Processor(&context, options);

	_int64 total;
	proc->Start();
	progress.recv(&total, sizeof(_int64));	//get blocks fro progress bar			
	socket.send(&total, sizeof(_int64));	//send size to interface

	//create a proxy loop for client and processor to mediate responses
	zmq::pollitem_t items[2] = {{socket, 0, ZMQ_POLLIN, 0}, {progress, 0, ZMQ_POLLIN, 0}};

	_int64 curr = 0;
	int tmp;
	while(true)
	{
		zmq::poll(&items[0], 2, -1);

		//handle client socket poll, sending current progress
		if (items[0].revents & ZMQ_POLLIN)
		{
			socket.recv(&tmp, sizeof(int));
			if (tmp != GPF_SVR_POLL) break;

			socket.send(&curr, sizeof(_int64));
			if (curr == total) 
				break; //done
		}
		//handle progress update from process
		if (items[1].revents & ZMQ_POLLIN)
		{
			progress.recv(&curr, sizeof(_int64));
		}
	}
	//printf("Task Complete.\n");
	progress.close();

}

FileOptions GpfServer::GetFileOptions(zmq::socket_t& socket)
{
	FileOptions options;
	zmq::message_t msg;

	int file_count;
	char** file_names;

	socket.recv(&msg);

	memcpy(&file_count, msg.data(), sizeof(int));
	file_names = (char**) malloc(file_count * sizeof(char*));

	for (int k = 0; k < file_count; k++)
	{
		zmq::message_t file;
		socket.recv(&file);
		file_names[k] = (char*) malloc(file.size() + 1);
		memcpy(file_names[k], file.data(), file.size());
		file_names[k][file.size()] = 0;
	}
	
	options.file_count = file_count;
	options.file_names = file_names;

	return options;
}

FilterOptions GpfServer::GetFilterOptions(zmq::socket_t& socket)
{
	FilterOptions options;
	options.get_program(socket);
	return options;
}

IndexOptions GpfServer::GetIndexOptions(zmq::socket_t& socket)
{
	char* pidx;
	char* tidx;
	IndexOptions options;
	zmq::message_t file;
	socket.recv(&file);

	pidx = (char*) malloc(file.size() + 1);
	memcpy(pidx, file.data(), file.size());
	pidx[file.size()] = 0;
	
	socket.recv(&file);
	tidx = (char*) malloc(file.size() + 1);
	memcpy(tidx, file.data(), file.size());
	tidx[file.size()] = 0;

	options.enabled = true;
	options.pidx_file = pidx;
	options.tidx_file = tidx;

	return options;
}


