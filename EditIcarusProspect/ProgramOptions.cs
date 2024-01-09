// Copyright 2024 Crystal Ferrai
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

using System.Diagnostics.CodeAnalysis;

namespace EditIcarusProspect
{
	internal class ProgramOptions
	{
		public string? ProspectName { get; }

		public ELobbyPrivacy LobbyPrivacy { get; }

		public EMissionDifficulty Difficulty { get; }

		public bool? Hardcore { get; }

		public int? DropZone { get; }

		public const int MaxOptionStringLength = 24; // Length of "-d, -difficulty [option]"

		public ProgramOptions(string? prospectName, ELobbyPrivacy lobbyPrivacy, EMissionDifficulty difficulty, bool? hardcore, int? dropZone)
		{
			ProspectName = prospectName;
			LobbyPrivacy = lobbyPrivacy;
			Difficulty = difficulty;
			Hardcore = hardcore;
			DropZone = dropZone;
		}

		public static void PrintCommandLineOptions(TextWriter writer, string indent)
		{
			writer.WriteLine($"{indent}-n, -name [value]         Set the prospect name to the supplied value.");
			writer.WriteLine($"{indent}                          Note: This will also change the file name.");
			writer.WriteLine();
			writer.WriteLine($"{indent}-p, -privacy [option]     Set the lobby privacy for the prospect to one of the following.");
			writer.WriteLine($"{indent}                          friends    Steam friends can join.");
			writer.WriteLine($"{indent}                          private    No one can join.");
			writer.WriteLine();
			writer.WriteLine($"{indent}-d, -difficulty [option]  Set the game difficulty for the prospect to one of [easy, medium, hard, extreme].");
			writer.WriteLine($"{indent}                          Warning: Extreme difficulty is only implemented for outposts. Things will break if");
			writer.WriteLine($"{indent}                          you use it elsewhere.");
			writer.WriteLine();
			writer.WriteLine($"{indent}-h, -hardcore [on/off]    Turn on or off the ability to self-respawn if you die in the prospect.");
			writer.WriteLine();
			writer.WriteLine($"{indent}-z, -dropzone [index]     Set the selected drop zone for the prospect.");
			writer.WriteLine($"{indent}                          Warning: Ensure the chosen index is valid for the specific map.");
		}

		public static bool TryParseCommandLine(
			IReadOnlyList<string> commandLine,
			TextWriter errorWriter,
			[NotNullWhen(true)] out ProgramOptions? options,
			[NotNullWhen(true)] out IReadOnlyList<string>? remainingCommandLine)
		{
			options = null;
			remainingCommandLine = null;

			List<string> remaining = new();

			string? prospectName = null;
			ELobbyPrivacy lobbyPrivacy = ELobbyPrivacy.Unknown;
			EMissionDifficulty difficulty = EMissionDifficulty.None;
			bool? hardcore = null;
			int? dropZone = null;

			for (int i = 0; i < commandLine.Count; ++i)
			{
				if (!commandLine[i].StartsWith('-'))
				{
					remaining.Add(commandLine[i]);
					continue;
				}

				string input = commandLine[i][1..].ToLowerInvariant();

				switch (input)
				{
					case "n":
					case "name":
						{
							++i;
							if (i >= commandLine.Count)
							{
								errorWriter.WriteLine("Missing [value] for parameter [name]");
								return false;
							}

							string subCommand = commandLine[i];
							if (subCommand.StartsWith('-'))
							{
								errorWriter.WriteLine("Missing [value] for parameter [name]");
								return false;
							}

							prospectName = subCommand;
						}
						break;
					case "p":
					case "privacy":
						{
							++i;
							if (i >= commandLine.Count)
							{
								errorWriter.WriteLine("Missing [option] for parameter [privacy]");
								return false;
							}

							string subCommand = commandLine[i].ToLowerInvariant();
							if (subCommand.StartsWith('-'))
							{
								errorWriter.WriteLine("Missing [option] for parameter [privacy]");
								return false;
							}

							switch (subCommand)
							{
								case "f":
								case "friends":
									lobbyPrivacy = ELobbyPrivacy.FriendsOnly;
									break;
								case "p":
								case "private":
									lobbyPrivacy = ELobbyPrivacy.Private;
									break;
								default:
									errorWriter.WriteLine($"Unrecognized [option] '{subCommand}' for parameter [privacy]");
									return false;
							}
						}
						break;
					case "d":
					case "difficulty":
						{
							++i;
							if (i >= commandLine.Count)
							{
								errorWriter.WriteLine("Missing [option] for parameter [difficulty]");
								return false;
							}

							string subCommand = commandLine[i].ToLowerInvariant();
							if (subCommand.StartsWith('-'))
							{
								errorWriter.WriteLine("Missing [option] for parameter [difficulty]");
								return false;
							}

							switch (subCommand)
							{
								case "e":
								case "easy":
									difficulty = EMissionDifficulty.Easy;
									break;
								case "m":
								case "medium":
								case "n":
								case "normal":
									difficulty = EMissionDifficulty.Medium;
									break;
								case "h":
								case "hard":
									difficulty = EMissionDifficulty.Hard;
									break;
								case "ex":
								case "extreme":
									difficulty = EMissionDifficulty.Extreme;
									break;
								default:
									errorWriter.WriteLine($"Unrecognized [option] '{subCommand}' for parameter [difficulty]");
									return false;
							}
						}
						break;
					case "h":
					case "hardcore":
						{
							++i;
							if (i >= commandLine.Count)
							{
								errorWriter.WriteLine("Missing [on/off] for parameter [hardcore]");
								return false;
							}

							string subCommand = commandLine[i].ToLowerInvariant();
							if (subCommand.StartsWith('-'))
							{
								errorWriter.WriteLine("Missing [on/off] for parameter [hardcore]");
								return false;
							}

							switch (subCommand)
							{
								case "on":
								case "t":
								case "true":
									hardcore = true;
									break;
								case "off":
								case "f":
								case "false":
									hardcore = false;
									break;
								default:
									errorWriter.WriteLine($"Unrecognized [on/off] '{subCommand}' for parameter [hardcore]");
									return false;
							}
						}
						break;
					case "z":
					case "dropzone":
						{
							++i;
							if (i >= commandLine.Count)
							{
								errorWriter.WriteLine("Missing [index] for parameter [dropzone]");
								return false;
							}

							string subCommand = commandLine[i].ToLowerInvariant();

							int value;
							if (!int.TryParse(subCommand, out value))
							{
								errorWriter.WriteLine($"Expected integer [index] for parameter [dropZone]. Found '{subCommand}'.");
								return false;
							}

							dropZone = value;
						}
						break;
					default:
						remaining.Add(commandLine[i]);
						break;
				}
			}

			remainingCommandLine = remaining;
			options = new ProgramOptions(prospectName, lobbyPrivacy, difficulty, hardcore, dropZone);
			return true;
		}

		public bool Any()
		{
			return ProspectName is not null || LobbyPrivacy != ELobbyPrivacy.Unknown || Difficulty != EMissionDifficulty.None || Hardcore.HasValue || DropZone.HasValue;
		}
	}

	internal enum ELobbyPrivacy
	{
		Unknown,
		FriendsOnly,
		Private
	}

	internal enum EMissionDifficulty
	{
		None,
		Easy,
		Medium,
		Hard,
		Extreme
	}
}
