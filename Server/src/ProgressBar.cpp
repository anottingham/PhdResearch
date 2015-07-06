#include "ProgressBar.h"
#include <cstdio>
#define _WIN32_WINNT 0x0500

#include <windows.h>
#include "zmq.hpp"

ProgressBar::ProgressBar(zmq::context_t * context, _int64 total_elements)
{
	total = total_elements;
	curr = 0;
	start = clock();

	//initial performance record
	cache[0] = 0;
	times[0] = start;
	idx = 1; //first read record
	space = 0; 

	server = new zmq::socket_t(*context, ZMQ_PAIR);
	server->connect("inproc://progress");
	server->send(&total, sizeof(_int64));

}

void ProgressBar::Update(_int64 increment)
{
	curr += increment;
	clock_t time = clock();

	if ( time - last_update > 400)
	{
		server->send(&curr, sizeof(_int64));
		last_update = time;

		if (space < 9) space++;
		cache[idx] = curr;
		times[idx] = time;
		
		int pos = static_cast<int>(((curr * 200) / total) / 5);
		int percent = static_cast<int>((curr * 100) / total); 
		for(int i = 0; i < 40; i++)
		{
			if( i < pos)
			{
				bar[i] = 178;
			}
			else if( i == pos)
			{
				bar[i] = 177;
			}
			else
			{
				bar[i] = 176;
			}
		}
		bar[40] = '\0';

		_int64 total_read = cache[idx] - cache[(idx + (10 - space))%10]; 
		
		double total_time = (double)(times[idx] - times[(idx + (10 - space))%10])/(double)CLOCKS_PER_SEC;
		//if (total_time == 0) ++total_time;
		double avg_read_time = (total_read / total_time)/(double)(1024*1024);

		
		int tmp = (clock() - start)/CLOCKS_PER_SEC;
		
		int hours, minutes, seconds;
		seconds = tmp % 60;
		tmp /= 60;
		minutes = tmp % 60; 
		tmp /= 60;
		hours = tmp;
		
		printf("\r%3d:", hours);
		if (minutes < 10) printf("0");
		printf("%d:", minutes);
		if (seconds < 10) printf("0");
		printf("%d ", seconds);

		printf("\%s %3d%% \xB0\xB0 %.1f / %.1f GB \xB0\xB0 %.0f MB/s ",bar, percent, (double)(curr) / (1024 * 1024 * 1024), (double)(total) / (1024 * 1024 * 1024), avg_read_time);
		
		idx = (idx + 1) % 10;
	}
}

void ProgressBar::Finished()
{
	clock_t end = clock();
	server->send(&total, sizeof(_int64));

	for(int i = 0; i < 40; i++)
	{
		bar[i] = 178;
	}
	bar[40] = '\0';

	_int64 total_read = cache[idx] - cache[(idx + (10 - space))%10]; 
		
	double total_time = (double)(end - start)/(double)CLOCKS_PER_SEC;
	//if (total_time == 0) ++total_time;
	double avg_read_time = (total / total_time)/(double)(1024*1024);

	int tmp = static_cast<int>(total_time);
	
	int hours, minutes, seconds;
	seconds = tmp % 60;
	tmp /= 60;
	minutes = tmp % 60; 
	tmp /= 60;
	hours = tmp;

	printf("\r%3d:", hours);
	if (minutes < 10) printf("0");
	printf("%d:", minutes);
	if (seconds < 10) printf("0");
	printf("%d ", seconds);

	printf("\%s 100%% \xB0\xB0 %.1f / %.1f GB \xB0\xB0 %.0f MB/s ",bar, (double)(total) / (1024 * 1024 * 1024), (double)(total) / (1024 * 1024 * 1024), avg_read_time);
	server->close();
}