namespace HardSubber
{
	public static class Program
	{
		private static void Main(string[] args)
		{
			HardSubber.Start(args).GetAwaiter().GetResult();
		}
	}
}