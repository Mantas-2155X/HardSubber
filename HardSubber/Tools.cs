using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
			var result = new int[3];

			var p = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = "/usr/bin/zenity",
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

				result[0] = Convert.ToInt32(split[0]);
				result[1] = Convert.ToInt32(split[1]);
				result[2] = Convert.ToInt32(split[2]);
			}
			
			return result;
		}
	}
}