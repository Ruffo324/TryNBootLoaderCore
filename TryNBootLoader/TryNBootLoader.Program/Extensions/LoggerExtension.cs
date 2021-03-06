using System;
using Serilog;

namespace TryNBootLoader.Program.Extensions
{
	internal static class LoggerExtension
	{
		// TODO [C.Groothoff]: Move to toolbox project
		public static void PrintLine(this ILogger logger, char lineSeparator = '-')
		{
			var lineLength = Console.WindowWidth * 0.5;
			var printedLine = string.Empty.PadRight((int)lineLength, lineSeparator);
			logger.Information("{Line}", printedLine);
		}
	}
}
