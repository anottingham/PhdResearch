#pragma once

#define WRITE_WIDTH_KB (64)


#define BLOCKS_PER_WRITE (100) 
//#define BLOCKS_PER_ALLOC (25)
//#define MAX_ITERATIONS (1000)

#define ZMQ_SAFECALL(socket_str, call)		do { try { call; } catch (zmq::error_t err) { printf("ZMQ Error : %s - %s\n", socket_str, err.what()); } } while(0); 
				


#define POST_ERROR(context, error_string)	do {																	\
								printf("Error in %s:\t%s.\nPress any key to terminate the application...\n", context, error_string); \
								getchar();																\
								exit(1);																\
							} while (0);