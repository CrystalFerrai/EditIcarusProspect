// Copyright 2025 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using IcarusSaveLib;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace EditIcarusProspect
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			Logger? logger;
			if (!TryCreateLoggger(out logger))
			{
				Console.Error.WriteLine("No logger could be created. Program will exit.");
				return OnExit(1);
			}

			if (args.Length == 0)
			{
				PrintUsage(logger);
				return OnExit(0);
			}

			ProgramOptions? options;
			if (!ProgramOptions.TryParseCommandLine(args, logger, out options))
			{
				PrintUsage(logger);
				return OnExit(1);
			}

			if (!options.Any())
			{
				PrintUsage(logger);
				return OnExit(0);
			}

			if (!File.Exists(options.ProspectPath))
			{
				logger.Error($"File not found or not accessible: {options.ProspectPath}");
				return OnExit(1);
			}

			bool success;
			try
			{
				success = UpdateProspect(options, logger);
			}
			catch (Exception ex)
			{
				logger.Error($"{ex.GetType().FullName}: {ex.Message}");
				success = false;
			}

			if (success)
			{
				logger.Log(LogLevel.Important, "Done.");
			}

			return OnExit(success ? 0 : 1);
		}

		private static bool TryCreateLoggger([NotNullWhen(true)] out Logger? logger)
		{
			logger = null;

			try
			{
				logger = ConsoleLogger.Create(Encoding.UTF8);
				return true;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Failed to create UTF8 logger. Error: [{ex.GetType().FullName}] {ex.Message}");
			}

			if (logger is null)
			{
				try
				{
					logger = new ConsoleLogger();
					return true;
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine($"Failed to create default logger. Error: [{ex.GetType().FullName}] {ex.Message}");
				}
			}

			return false;
		}

		private static int OnExit(int code)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.ReadKey(true);
			}
			return code;
		}

		private static void PrintUsage(Logger logger, LogLevel logLevel = LogLevel.Information)
		{
			string optionIndent = "    ";
			logger.Log(logLevel, $"Usage: EditIcarusProspect [options] path\n");
			ProgramOptions.PrintCommandLineOptions(logger, indent: optionIndent);
			logger.Log(logLevel, $"\n{optionIndent}{"path",-ProgramOptions.MaxOptionStringLength}  The path to the prospect save json file to modify. Recommended to backup file first.\n");
			logger.Log(logLevel, "Note: Must select at least one action to perform.");
		}

		private static bool UpdateProspect(ProgramOptions options, Logger logger)
		{
			string path = options.ProspectPath;
			string oldPath = path;
			if (options.ProspectName is not null)
			{
				path = Path.Combine(Path.GetDirectoryName(path)!, $"{options.ProspectName}.json");
				if (File.Exists(path))
				{
					logger.Error($"Cannot rename prospect. A prospect with the name {options.ProspectName} already exists.");
					return false;
				}
			}

			ProspectSave? prospect;

			logger.Log(LogLevel.Important, "Loading prospect...");

			try
			{
				using (FileStream file = File.OpenRead(oldPath))
				{
					prospect = ProspectSave.Load(file);
				}
			}
			catch (Exception ex)
			{
				logger.Error($"Error reading prospect file. [{ex.GetType().FullName}] {ex.Message}");
				return false;
			}

			if (prospect == null)
			{
				logger.Error("Error reading prospect file. Could not load Json.");
				return false;
			}

			ProspectEditor editor = new(logger);
			if (!editor.Run(prospect, options))
			{
				// Run indicated not to save
				return true;
			}

			logger.Log(LogLevel.Important, "Saving prospect...");

			using (FileStream file = File.Create(path))
			{
				prospect.Save(file);
			}

			if (options.ProspectName is not null)
			{
				File.Delete(oldPath);
			}

			return true;
		}
	}
}
