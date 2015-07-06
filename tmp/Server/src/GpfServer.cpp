#include "GpfServer.h"
#include "zhelpers.hpp"
#include "zmq.hpp"
#include "stdlib.h"
#include "tinythread.h"
#include "Processor.h"
#include "Common.h"

#define GPF_SVR_RESET -1
#define GPF_SVR_ENDREQ 0
#define GPF_SVR_INDEX 1
#define GPF_SVR_FILTER 2
#define GPF_SVR_POLL 4
#define GPF_SVR_CONNECT 1234


void GpfServer::Start(int port)
{
	zmq::socket_t client_socket(context, ZMQ_REP);
	char * str = (char*)malloc(25);
	sprintf(str, "tcp://*:%d\0",port);
	client_socket.bind(str);

	printf("GPF Server v0.1\n---------------\n\n");
	
	int command;
	while(true)
	{
		printf("Waiting for client...\n");
		ClientConnect(client_socket);
	
		ClientProcess(client_socket);
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
			socket.send(&command, sizeof(int)); //send command back to indicate it has been received
			printf("Connected to client.\n");
			break;
		}
		else if (command == GPF_SVR_RESET) //reset
		{
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

void GpfServer::ClientProcess(zmq::socket_t& socket)
{
	int command = GPF_SVR_CONNECT;
	ProcessOptions options;
	
	options.file = GetFileOptions(socket);

	while (command != GPF_SVR_RESET && command != GPF_SVR_ENDREQ)
	{
		socket.recv(&command, sizeof(int));

		switch(command)
		{	
		case GPF_SVR_RESET:
		case GPF_SVR_ENDREQ:
			break;
		case GPF_SVR_INDEX:
			options.index = GetIndexOptions(socket);
			break;
		case GPF_SVR_FILTER:
			options.filter = GetFilterOptions(socket);
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
	if (command == GPF_SVR_ENDREQ)
	{

		Poll(socket, options);
	}
}


void GpfServer::Poll(zmq::socket_t& socket, ProcessOptions options)
{
	zmq::socket_t progress(context, ZMQ_PAIR);
	progress.bind("inproc://progress");

	Processor * proc = new Processor(&context, options);

	_int64 total;
	proc->Start();
	progress.recv(&total, sizeof(_int64));	//get blocks fro progress bar				//determine size
	socket.send(&total, sizeof(_int64));	//send size to interface

	
	zmq::pollitem_t items[2] = {{socket, 0, ZMQ_POLLIN, 0}, {progress, 0, ZMQ_POLLIN, 0}};

	_int64 curr = 0;
	int tmp;
	while(true)
	{
		zmq::poll(&items[0], 2, -1);

		if (items[0].revents & ZMQ_POLLIN)
		{
			socket.recv(&tmp, sizeof(int));
			if (tmp != GPF_SVR_POLL) break;

			socket.send(&curr, sizeof(_int64));
			if (curr == total) break; //done
		}
		if (items[1].revents & ZMQ_POLLIN)
		{
			progress.recv(&curr, sizeof(_int64));
		}
	}
	printf("Task Complete.\n");

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
	char* out_folder;
	int tmp;
	FilterOptions options;
	zmq::message_t folder;
	socket.recv(&folder);

	out_folder = (char*) malloc(folder.size() + 1);
	memcpy(out_folder, folder.data(), folder.size());
	out_folder[folder.size()] = 0;
	
	options.enabled = true;
	options.out_folder = out_folder;

	socket.recv(&tmp, sizeof(int));
	options.record_start = tmp;
	
	socket.recv(&tmp, sizeof(int));
	options.record_length = tmp;

	socket.recv(&tmp, sizeof(int));
	options.packets_per_buffer = tmp;

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


