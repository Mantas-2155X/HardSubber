using System;
using HardSubber.Enums;

namespace HardSubber
{
	public static class Log
	{
		public static void ConsoleWrite(string text, ELogType logType)
		{
			switch (logType)
			{
				case ELogType.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case ELogType.Message:
					Console.ForegroundColor = ConsoleColor.White;
					break;
				case ELogType.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
			}

			Console.WriteLine(text);
			Console.ResetColor();
		}

		public static void PrintHelp()
		{
			ConsoleWrite("Usage:", ELogType.Message);
			ConsoleWrite("HardSubber	--help			# Show usage manual", ELogType.Message);
			ConsoleWrite("HardSubber	--version		# Show current version of HardSubber", ELogType.Message);
			ConsoleWrite("", ELogType.Message);
			ConsoleWrite("HardSubber	--hardsub		# Hardsub video(s)", ELogType.Message);
			ConsoleWrite("		--path			# Video input path. File or directory", ELogType.Message);
			ConsoleWrite("		--output		# Video output path. Optional, directory", ELogType.Message);
			ConsoleWrite("		--substream		# Subtitle stream index. Optional, number", ELogType.Message);
			ConsoleWrite("		--audiostream		# Audio stream index. Optional, number", ELogType.Message);
			ConsoleWrite("		--picture		# Use picture based subtitles. Optional", ELogType.Message);
		}
	}
}