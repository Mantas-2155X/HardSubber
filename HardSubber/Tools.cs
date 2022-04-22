using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HardSubber.Enums;

namespace HardSubber
{
	public static class Tools
	{
		public static IEnumerable<IEnumerable<T>> Split<T>(this T[] array, int size)
		{
			for (var i = 0; i < (float)array.Length / size; i++)
			{
				yield return array.Skip(i * size).Take(size);
			}
		}

		public static int[] GetZenityOptions()
		{
			var result = new [] {0, 0, 0};

			if (HardSubber.zenityPath == null)
			{
				Log.ConsoleWrite("Zenity path not found", ELogType.Warning);
				return result;
			}
			
			var p = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = HardSubber.zenityPath,
					Arguments = "--forms --title \"HardSubber\" --text \"Enter options\" --add-entry \"Subtitle Index\" --add-entry \"Audio Index\" --add-entry \"Picture Mode\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			
			p.Start();
			
			while (!p.StandardOutput.EndOfStream)
			{
				var line = p.StandardOutput.ReadLine();
				if (string.IsNullOrEmpty(line))
					return result;

				var split = line.Split("|");
				if (split.Length != 3)
					return result;

				for (var i = 0; i < split.Length; i++)
					if (split[i] != "")
						result[i] = Convert.ToInt32(split[i]);
			}
			
			return result;
		}
	}
}