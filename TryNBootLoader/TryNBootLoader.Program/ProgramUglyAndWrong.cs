﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

#pragma warning disable all
namespace TryNBootLoader.Programm
{
	internal class ProgramUglyAndWrong
	{
		private static long _imei;

		private static List<long> _generatedOemList = new();

		private static void SaveUntestedCodes()
		{
			while (true)
			{
				try
				{
					WriteToCodeTextFile(ref _generatedOemList);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error happens in ugly saveTestCodes function.");
				}

				var testDelay = TimeSpan.FromSeconds(5);
				Log.Information("Thread sleeping '{testDelay}' before repeating..", testDelay);
				Thread.Sleep(testDelay);
			}
		}


		private static void WriteToCodeTextFile(ref List<long> generatedOemList)
		{
			using TextWriter tw = new StreamWriter(AppContext.BaseDirectory + $"oemcodes_{_imei}.txt");
			foreach (var s in generatedOemList)
				tw.WriteLine(s.ToString());
		}


		private static void ExecuteFastBootUnlock(long oemCode, ref bool success, ref List<long> generatedOemList,
			string fastbooDir)
		{
			// TODO [C.Groothoff]: Write auto downlaoder for every platoform.
			var output = "";
			var fastBootExe = new Process
			{
				StartInfo =
				{
					FileName = "fastboot",
					Arguments = $"oem unlock {oemCode}",
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				}
			};


			try
			{
				var timeout = TimeSpan.FromSeconds(10);
				fastBootExe.Start();
				var readerStdError = fastBootExe.StandardError;
				var readerStdOutput = fastBootExe.StandardError;
				output = readerStdError.ReadToEnd() + readerStdOutput.ReadToEnd();


				fastBootExe.WaitForExit((int) timeout.TotalMilliseconds);
			}
			catch (Exception ex)
			{
			}

			Console.WriteLine(output);

			if (output.Contains("sucess"))
			{
				File.WriteAllText(AppContext.BaseDirectory + $"oemcode_successful_{_imei}.txt",
					$"OEM Unlock Code for IMEI={_imei} : {oemCode}");

				success = true;
			}

			if (output.Contains("Invalid key"))
			{
				var i = generatedOemList.IndexOf(oemCode);
				try
				{
					generatedOemList.RemoveAt(i);
				}
				catch (Exception ex)
				{
				}
			}
		}

		private static int CheckLuhn(string imei)
		{
			Log.Information("Running \"luhn\" algorithm with {imei}", imei);
			var sum = 0;
			var alternate = false;
			for (var i = imei.Length - 1; i >= 0; i--)
			{
				var nx = imei.ToArray();
				var n = int.Parse(nx[i].ToString());

				if (alternate)
				{
					n *= 2;

					if (n > 9) n = n % 10 + 1;
				}

				sum += n;
				alternate = !alternate;
			}

			return sum % 10;
		}


		public static long IncrementChecksum(ref long oemCode, long checksum, long imei)
		{
			oemCode += (long) (checksum + Math.Sqrt(imei) * 1024);
			return oemCode;
		}


		// private static void ExtractResource(byte[] resFile, string resFileOutDir)
		// {
		//     using (var fileStream = new FileStream(resFileOutDir + "fastboot.zip", FileMode.Create))
		//     {
		//         fileStream.Write(resFile, 0, resFile.Length);
		//     }
		//
		//     try
		//     {
		//         ZipFile.ExtractToDirectory(resFileOutDir + "fastboot.zip", "fastboot");
		//         File.Delete(resFileOutDir + "fastboot.zip");
		//     }
		//     catch (Exception ex)
		//     {
		//         Console.WriteLine(ex.Message);
		//     }
		// }


		private static void Maiasdn(string[] args)
		{
			var oemCode = 1000000000000000;
			var currentDir = AppContext.BaseDirectory;

			if (args.Length != 0)
			{
				_imei = long.Parse(args[0]);
			}
			else
			{
				Console.Write("Enter IMEI Code = ");
				_imei = long.Parse(Console.ReadLine());
			}

			while (_imei.ToString().Length != 15)
			{
				Console.Clear();
				Console.Write("Enter IMEI Code = ");
				_imei = long.Parse(Console.ReadLine());
			}

			long checksum = CheckLuhn(_imei.ToString());
			var finish = false;


			if (File.Exists(AppContext.BaseDirectory + $"oemcodes_{_imei}.txt"))
			{
				var lines = File.ReadAllLines(AppContext.BaseDirectory + $"oemcodes_{_imei}.txt");

				foreach (var s in lines) _generatedOemList.Add(long.Parse(s));
			}
			else
			{
				while (finish != true)
					if (oemCode.ToString().Length == 16)
					{
						_generatedOemList.Add(IncrementChecksum(ref oemCode, checksum, _imei));
					}
					else
					{
						finish = true;
						WriteToCodeTextFile(ref _generatedOemList);
					}
			}


			var count = 0;
			var countMax = _generatedOemList.Count;
			var success = false;

			var savingThread = new Thread(SaveUntestedCodes);
			savingThread.Start();

			Parallel.ForEach(_generatedOemList, new ParallelOptions { MaxDegreeOfParallelism = 16 }, currentOem =>
			{
				if (success == false)
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine($"Processing {currentOem} on thread {Thread.CurrentThread.ManagedThreadId}");
					Console.ResetColor();
					count += 1;
					Console.Title = $"Processing: {count} / {countMax}";

					//ExecuteFastBootUnlock(currentOem, ref success, ref _generatedOemList, fastbootFolder);
				}
				else
				{
					savingThread.Abort();
				}
			});


			try
			{
				Directory.Delete(currentDir + "fastboot", true);
			}
			catch
			{
			}

			Console.ReadKey();
		}
	}
}


namespace TryNBootLoader.Programm.Constants
{
}

namespace TryNBootLoader.Programm.Extensions
{
}
