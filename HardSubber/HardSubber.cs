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
		private const string VERSION = "1.0.1";

		private static readonly string[] supportedVideoFormats =
		{
			".avi",
			".mkv",
			".m4v",
			".mp4",
		};

		private static string ffmpegPath;
		public static string zenityPath;
		
		public static async Task Start(string[] args)
		{
			if (args == null || args.Length == 0)
			{
				Log.PrintHelp();
				return;
			}

			zenityPath = await getPath("zenity");
			ffmpegPath = await getPath("ffmpeg");
			if (string.IsNullOrEmpty(ffmpegPath))
			{
				Log.ConsoleWrite("ffmpeg was not found", ELogType.Error);
				return;
			}

			Log.ConsoleWrite("ffmpeg found at: " + ffmpegPath, ELogType.Message);

			
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
				case "--fix":
					await processFix(args);
					break;
				default:
					Log.PrintHelp();
					break;
			}
			
			Console.ResetColor();
		}
		
		private static async Task processHardsub(string[] args)
		{
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
					case "zenity" when !string.IsNullOrEmpty(zenityPath):
						var options = Tools.GetZenityOptions();
						subStream = options[0];
						audioStream = options[1];
						picture = Convert.ToBoolean(options[2]);
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

				files = files.OrderBy(f => f).ToArray();
				
				var workers = Environment.ProcessorCount / 4;
				var workersFiles = files.Split(workers + 1);
				
				foreach (var workerFiles in workersFiles)
				{
					Parallel.ForEach(workerFiles, s =>
					{
						var file = new FileInfo(s);
						if (!supportedVideoFormats.Contains(file.Extension))
						{
							Log.ConsoleWrite("File " + file.Name + " skipped because the video format (" + file.Extension + ") is not supported", ELogType.Error);
						}
						else
						{
							hardsubFile(file, outputPath, subStream, audioStream, picture);
						}
					});
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

			Log.ConsoleWrite("Finished HardSubbing", ELogType.Message);
			
			await Task.Delay(-1);
		}
		
		private static async Task processFix(string[] args)
		{
			var inputPath = "";
			var outputPath = "";

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
				}
			}

			if (inputPath == "")
			{
				Log.ConsoleWrite("No input path provided", ELogType.Error);
				return;
			}
			
			if (outputPath == "")
				Log.ConsoleWrite("No output path provided", ELogType.Warning);

			var attributes = File.GetAttributes(inputPath);
			if (attributes == FileAttributes.Directory)
			{
				if (outputPath == "")
					outputPath = inputPath + "/fixed";

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

					fixFile(file, outputPath);
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
					outputPath = file.DirectoryName + "/fixed";

				if (!Directory.Exists(outputPath))
					Directory.CreateDirectory(outputPath);
				
				fixFile(file, outputPath);
			}
		}
		
		private static void hardsubFile(FileInfo file, string output, int subIndex, int audioIndex, bool picture)
		{ // TODO: support nvidia/intel and windows for vaapi
			Log.ConsoleWrite("Hardsubbing file " + file.Name, ELogType.Message);
			Log.ConsoleWrite("", ELogType.Message);
			
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

			var newName = file.FullName.Replace("'", "");
			if (newName != file.FullName)
				file.MoveTo(newName);

			var shortName = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
			
			if (picture)
			{
				subMap = subIndex != -1 ? 
					$"-filter_complex \"[0:v][0:s:{subIndex}]overlay[v]\" -map \"[v]\" " : 
					$"-filter_complex \"[0:v][0:s]overlay[v]\" -map \"[v]\" ";
			}
			else
			{
				subMap = subIndex != -1 ? 
					$"-filter_complex \"subtitles='{file.FullName}':stream_index={subIndex}, format=nv12,hwupload\" " : 
					$"-filter_complex \"subtitles='{file.FullName}', format=nv12,hwupload\" ";
			}
			
			process.StartInfo.Arguments += $"-hide_banner -loglevel warning -stats ";
			process.StartInfo.Arguments += $"-vaapi_device /dev/dri/renderD128 ";
			process.StartInfo.Arguments += $"-i \"{file.FullName}\" ";
			process.StartInfo.Arguments += subMap;
			process.StartInfo.Arguments += audioMap + "-c:a aac ";
			
			if (!picture) 
				process.StartInfo.Arguments += "-c:v h264_vaapi ";
			
			process.StartInfo.Arguments += "-qp 22 ";
			process.StartInfo.Arguments += "-color_primaries unknown -color_trc unknown -colorspace unknown ";
			process.StartInfo.Arguments += "-metadata title=\"" + shortName + "\" ";
			process.StartInfo.Arguments += "-movflags faststart ";
			process.StartInfo.Arguments += "-strict -2 ";
			process.StartInfo.Arguments += $"\"{output}/{shortName}\".mp4";
			
			process.Start();
			process.WaitForExit();
		}
		
		private static void fixFile(FileInfo file, string output)
		{
			Log.ConsoleWrite("Fixing file " + file.Name, ELogType.Message);
			Log.ConsoleWrite("", ELogType.Message);
			
			var newName = file.FullName.Replace("'", "");
			if (newName != file.FullName)
				file.MoveTo(newName);

			var shortName = file.Name.Substring(0, file.Name.Length - file.Extension.Length);
			
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

			process.StartInfo.Arguments += $"-hide_banner -loglevel warning -stats ";
			process.StartInfo.Arguments += $"-i \"{file.FullName}\" ";
			process.StartInfo.Arguments += "-metadata title=\"" + shortName + "\" ";
			process.StartInfo.Arguments += "-movflags faststart ";
			process.StartInfo.Arguments += "-c copy ";
			process.StartInfo.Arguments += $"\"{output}/{file.Name}\"";
			
			process.Start();
			process.WaitForExit();
		}
		
		private static async Task<string> getPath(string app)
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					Arguments = app,
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