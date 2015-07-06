
#include <ctime>

class ProcessTimer
{
private:
	char * filename;
	clock_t start;
	clock_t stop;

public:
	ProcessTimer(char* outfile)
	{
		filename = outfile;
	}

	inline void Start()
	{
		start = clock();
	}

	inline void Stop()
	{
		stop = clock();

		time_t time_ms = stop - start;

		FILE * file;
		errno_t result = fopen_s(&file, filename, "a");
		if (result == 0)
		{
			char * str = (char*) malloc(256);
			sprintf(str, "%d\n\0", time_ms);
			fputs(str, file);
			fflush(file);
			fclose(file);
			free(str);
		}
		else
		{
			printf("Timing failed with errno %d\n\n", result);
		}

	}

	

};