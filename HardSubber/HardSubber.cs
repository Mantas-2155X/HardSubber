using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HardSubber.Enums;

namespace HardSubber
{
	public static class HardSubber
	{
		private const string VERSION = "1.0.0";

		private static readonly string[] supportedVideoFormats =
		{
			".avi",
			".mkv",
			".mp4",
		};

		private static string ffmpegPath;
		
		public static async Task Start(string[] args)
		{
			if (args == null || args.Length == 0)
			{
				Log.PrintHelp();
				return;
			}

			switch (args[0])
			{
				case "--version":
					Log.ConsoleWrite("HardSubber v" + VERSION, ELogType.Message);
					break;
				case "--help":
					Log.PrintHelp();
					break;
				case "--hardsub":
					await processHardsub(args);
					break;
				default:
					Log.PrintHelp();
					break;
			}
			
			Console.ResetColor();
			Console.Clear();
		}
		
		private static async Task processHardsub(string[] args)
		{
			ffmpegPath = await getFFmpegPath();
			if (string.IsNullOrEmpty(ffmpegPath))
			{
				Log.ConsoleWrite("ffmpeg was not found", ELogType.Error);
				return;
			}

			Log.ConsoleWrite("ffmpeg found at: " + ffmpegPath, ELogType.Message);

			var inputPath = "";
			var outputPath = "";
			var subStream = -1;
			var audioStream = -1;
			var picture = false;

			for (var i = 0; i < args.Length; i++)
			{
				if (args[i].Length < 2)
					continue;
				
				switch (args[i].Substring(2))
				{
					case "path" when args.Length < i + 1:
						Log.ConsoleWrite("No input path provided", ELogType.Error);
						return;
					case "path":
						inputPath = args[i + 1];
						break;
					case "output" when args.Length < i + 1:
						Log.ConsoleWrite("No output path provided", ELogType.Error);
						return;
					case "output":
						outputPath = args[i + 1];
						break;
					case "substream" when args.Length < i + 1:
						Log.ConsoleWrite("No subtitle stream index provided", ELogType.Error);
						return;
					case "substream":
						subStream = Convert.ToInt32(args[i + 1]);
						break;
					case "audiostream" when args.Length < i + 1:
						Log.ConsoleWrite("No audio stream index provided", ELogType.Error);
						return;
					case "audiostream":
						audioStream = Convert.ToInt32(args[i + 1]);
						break;
					case "picture":
						picture = true;
						break;
				}
			}

			if (inputPath == "")
			{
				Log.ConsoleWrite("No input path provided", ELogType.Error);
				return;
			}
			
			if (outputPath == "")
				Log.ConsoleWrite("No output path provided", ELogType.Warning);

			if (subStream < 0)
				Log.ConsoleWrite("No subtitle stream index provided", ELogType.Warning);

			if (audioStream < 0)
				Log.ConsoleWrite("No audio stream index provided", ELogType.Warning);

			var attributes = File.GetAttributes(inputPath);
			if (attributes == FileAttributes.Directory)
			{
				if (outputPath == "")
					outputPath = inputPath + "/subbed";

				if (!Directory.Exists(outputPath))
					Directory.CreateDirectory(outputPath);

				var files = Directory.GetFiles(inputPath);
				if (files == null || files.Length == 0)
				{
					Log.ConsoleWrite("No files found in provided input path", ELogType.Error);
					return;
				}

				foreach (var filePath in files)
				{
					var file = new FileInfo(filePath);
					if (!supportedVideoFormats.Contains(file.Extension))
					{
						Log.ConsoleWrite("File " + file.Name + " skipped because the video format (" + file.Extension + ") is not supported", ELogType.Error);
						continue;
					}

					hardsubFile(file, outputPath, subStream, audioStream, picture);
				}
			}
			else if (attributes == FileAttributes.Normal)
			{
				var file = new FileInfo(inputPath);
				if (!supportedVideoFormats.Contains(file.Extension))
				{
					Log.ConsoleWrite("Unsupported video format (" + file.Extension + ")", ELogType.Error);
					return;
				}
				
				if (outputPath == "")
					outputPath = file.DirectoryName + "/subbed";

				if (!Directory.Exists(outputPath))
					Directory.CreateDirectory(outputPath);
				
				hardsubFile(file, outputPath, subStream, audioStream, picture);
			}
		}
		
		private static void hardsubFile(FileInfo file, string output, int subIndex, int audioIndex, bool picture)
		{
			Log.ConsoleWrite("Hardsubbing file " + file.Name, ELogType.Message);
			
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = ffmpegPath,
					Arguments = "",
					UseShellExecute = false, 
					RedirectStandardOutput = false,
					CreateNoWindow = true
				}
			};

			var audioMap = "";
			if (audioIndex != -1)
			{
				audioMap = "-map 0:a:" + audioIndex + " ";
			}

			string subMap;
			
			if (picture)
			{
				if (subIndex != -1)
				{
					subMap = $"-filter_complex \"[0:v][0:s:{subIndex}]overlay[v]\" -map \"[v]\" ";
				}
				else
				{
					subMap = $"-filter_complex \"[0:v][0:s]overlay[v]\" -map \"[v]\" ";
				}
			}
			else
			{
				if (subIndex != -1)
				{
					subMap = $"-filter_complex \"subtitles='{file.FullName}':stream_index={subIndex}\" ";
				}
				else
				{
					subMap = $"-filter_complex \"subtitles='{file.FullName}'\" ";
				}
			}

			process.StartInfo.Arguments += $"-hide_banner -loglevel warning -stats ";
			process.StartInfo.Arguments += $"-i \"{file.FullName}\" ";
			process.StartInfo.Arguments += subMap;
			process.StartInfo.Arguments += audioMap + "-c:a copy ";
			process.StartInfo.Arguments += $"\"{output}/{file.Name}\"";
			
			Log.ConsoleWrite("Running ffmpeg with: " + process.StartInfo.Arguments, ELogType.Message);
			
			process.Start();
			process.WaitForExit();
		}

		private static async Task<string> getFFmpegPath()
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					Arguments = "ffmpeg",
					UseShellExecute = false, 
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			
			process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
			process.Start();

			string path = null;
			
			while (!process.StandardOutput.EndOfStream)
			{
				path = await process.StandardOutput.ReadLineAsync();
			}
			
			return path;
		}
	}
}