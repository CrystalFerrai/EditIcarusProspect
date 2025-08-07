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

using System.Diagnostics.CodeAnalysis;

namespace EditIcarusProspect
{
	/// <summary>
	/// Represents passed in options for the overall program
	/// </summary>
	internal class ProgramOptions
	{
		/// <summary>
		/// The path to the prospect to read or modify
		/// </summary>
		public string ProspectPath { get; }

		/// <summary>
		/// A new name for the prospect
		/// </summary>
		public string? ProspectName { get; }

		/// <summary>
		/// A new privacy setting for the prospect
		/// </summary>
		public ELobbyPrivacy LobbyPrivacy { get; }

		/// <summary>
		/// A new difficulty for the prospect
		/// </summary>
		public EMissionDifficulty Difficulty { get; }

		/// <summary>
		/// A new hardcore setting for the prospect
		/// </summary>
		public bool? Hardcore { get; }

		/// <summary>
		/// A new drop zone for the prospect
		/// </summary>
		public int? DropZone { get; }

		/// <summary>
		/// Instructs the program to list all players in the prospect
		/// </summary>
		public bool ListPlayers { get; }

		/// <summary>
		/// Instructs the program to cleanup unassociated player related recorders
		/// </summary>
		public bool RunCleanup { get; }

		/// <summary>
		/// A list of players to remove fromt he prospect
		/// </summary>
		public IReadOnlyList<string>? PlayersToRemove { get; }

		public const int MaxOptionStringLength = 24; // Length of "-d, -difficulty [option]"

		public ProgramOptions(string prospectPath, string? prospectName, ELobbyPrivacy lobbyPrivacy, EMissionDifficulty difficulty, bool? hardcore, int? dropZone, bool listPlayers, bool runCleanup, IReadOnlyList<string>? playersToRemove)
		{
			ProspectPath = prospectPath;
			ProspectName = prospectName;
			LobbyPrivacy = lobbyPrivacy;
			Difficulty = difficulty;
			Hardcore = hardcore;
			DropZone = dropZone;
			ListPlayers = listPlayers;
			RunCleanup = runCleanup;
			PlayersToRemove = playersToRemove;
		}

		public static void PrintCommandLineOptions(Logger logger, LogLevel logLevel = LogLevel.Information, string indent = "")
		{
			logger.Log(logLevel, $"{indent}-n, -name [value]         Set the prospect name to the supplied value.");
			logger.Log(logLevel, $"{indent}                          Note: This will also change the file name.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-p, -privacy [option]     Set the lobby privacy for the prospect to one of the following.");
			logger.Log(logLevel, $"{indent}                          friends    Steam friends can join.");
			logger.Log(logLevel, $"{indent}                          private    No one can join.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-d, -difficulty [option]  Set the game difficulty for the prospect to one of [easy, medium, hard, extreme].");
			logger.Log(logLevel, $"{indent}                          Warning: Extreme difficulty is only implemented for outposts. Things will break if");
			logger.Log(logLevel, $"{indent}                          you use it elsewhere.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-h, -hardcore [on/off]    Turn on or off the ability to self-respawn if you die in the prospect.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-z, -dropzone [index]     Set the selected drop zone for the prospect.");
			logger.Log(logLevel, $"{indent}                          Warning: Ensure the chosen index is valid for the specific map.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-l, -list                 Prints information about all player characters stored in the prospect.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-c, -cleanup              Removes any rockets or other player data that is not associated with a valid player.");
			logger.Log(logLevel, $"{indent}                          Run this if you see any warnings when running -list that you want to clean up.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}-r, -remove [players]     Removes listed player characters and their rockets. List a player's Steam ID to");
			logger.Log(logLevel, $"{indent}                          remove all of that player's characters. To remove only a specific character, list");
			logger.Log(logLevel, $"{indent}                          a Steam ID followed by a hyphen, followed by the character slot number. Separate");
			logger.Log(logLevel, $"{indent}                          list entries with commas. Do not include any spaces.");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}                          Example: -r 76561100000000000,76561150505050505-0,76561123232323232");
			logger.LogEmptyLine(logLevel);
			logger.Log(logLevel, $"{indent}                          Warning: Players removed this way will not be able to reclaim their loadout unless");
			logger.Log(logLevel, $"{indent}                          it is insured.");
		}

		public static bool TryParseCommandLine(IReadOnlyList<string> commandLine, Logger logger, [NotNullWhen(true)] out ProgramOptions? options)
		{
			options = null;

			string? prospectPath = null;
			string? prospectName = null;
			ELobbyPrivacy lobbyPrivacy = ELobbyPrivacy.Unknown;
			EMissionDifficulty difficulty = EMissionDifficulty.None;
			bool? hardcore = null;
			int? dropZone = null;
			bool listPlayers = false;
			bool runCleanup = false;
			List<string>? playersToRemove = null;

			int positionalArgIndex = 0;

			for (int i = 0; i < commandLine.Count; ++i)
			{
				if (commandLine[i].StartsWith('-'))
				{
					string input = commandLine[i][1..].ToLowerInvariant();

					switch (input)
					{
						case "n":
						case "name":
							{
								if (prospectName is not null)
								{
									logger.LogError("[name] parameter found more than once");
									return false;
								}

								++i;
								if (i >= commandLine.Count)
								{
									logger.LogError("Missing [value] for parameter [name]");
									return false;
								}

								string subCommand = commandLine[i];
								if (subCommand.StartsWith('-'))
								{
									logger.LogError("Missing [value] for parameter [name]");
									return false;
								}

								prospectName = subCommand;
							}
							break;
						case "p":
						case "privacy":
							{
								if (lobbyPrivacy != ELobbyPrivacy.Unknown)
								{
									logger.LogError("[privacy] parameter found more than once");
									return false;
								}

								++i;
								if (i >= commandLine.Count)
								{
									logger.LogError("Missing [option] for parameter [privacy]");
									return false;
								}

								string subCommand = commandLine[i].ToLowerInvariant();
								if (subCommand.StartsWith('-'))
								{
									logger.LogError("Missing [option] for parameter [privacy]");
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
										logger.LogError($"Unrecognized [option] '{subCommand}' for parameter [privacy]");
										return false;
								}
							}
							break;
						case "d":
						case "difficulty":
							{
								if (difficulty != EMissionDifficulty.None)
								{
									logger.LogError("[difficulty] parameter found more than once");
									return false;
								}

								++i;
								if (i >= commandLine.Count)
								{
									logger.LogError("Missing [option] for parameter [difficulty]");
									return false;
								}

								string subCommand = commandLine[i].ToLowerInvariant();
								if (subCommand.StartsWith('-'))
								{
									logger.LogError("Missing [option] for parameter [difficulty]");
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
										logger.LogError($"Unrecognized [option] '{subCommand}' for parameter [difficulty]");
										return false;
								}
							}
							break;
						case "h":
						case "hardcore":
							{
								if (hardcore.HasValue)
								{
									logger.LogError("[hardcore] parameter found more than once");
									return false;
								}

								++i;
								if (i >= commandLine.Count)
								{
									logger.LogError("Missing [on/off] for parameter [hardcore]");
									return false;
								}

								string subCommand = commandLine[i].ToLowerInvariant();
								if (subCommand.StartsWith('-'))
								{
									logger.LogError("Missing [on/off] for parameter [hardcore]");
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
										logger.LogError($"Unrecognized [on/off] '{subCommand}' for parameter [hardcore]");
										return false;
								}
							}
							break;
						case "z":
						case "dropzone":
							{
								if (dropZone.HasValue)
								{
									logger.LogError("[dropzone] parameter found more than once");
									return false;
								}

								++i;
								if (i >= commandLine.Count)
								{
									logger.LogError("Missing [index] for parameter [dropzone]");
									return false;
								}

								string subCommand = commandLine[i].ToLowerInvariant();

								int value;
								if (!int.TryParse(subCommand, out value))
								{
									logger.LogError($"Expected integer [index] for parameter [dropZone]. Found '{subCommand}'.");
									return false;
								}

								dropZone = value;
							}
							break;
						case "l":
						case "list":
							{
								listPlayers = true;
							}
							break;
						case "c":
						case "cleanup":
							{
								runCleanup = true;
							}
							break;
						case "r":
						case "remove":
							{
								++i;
								if (i >= commandLine.Count)
								{
									logger.LogError("Missing [players] for parameter [remove]");
									return false;
								}

								string subCommand = commandLine[i].ToLowerInvariant();

								// If [remove] is passed more than once, concatenate the values
								if (playersToRemove is null)
								{
									playersToRemove = new();
								}
								playersToRemove.AddRange(subCommand.Split(','));
							}
							break;
						default:
							logger.LogError($"Unrecognized argument '{commandLine[i]}'");
							return false;
					}
				}
				else
				{
					// Positional arg
					switch (positionalArgIndex)
					{
						case 0:
							prospectPath = Path.GetFullPath(commandLine[i]);
							break;
						default:
							logger.LogError("Too many positional arguments.");
							return false;
					}
					++positionalArgIndex;
				}
			}

			if (prospectPath is null)
			{
				logger.LogError("Missing prospect path argument");
				return false;
			}

			options = new ProgramOptions(prospectPath, prospectName, lobbyPrivacy, difficulty, hardcore, dropZone, listPlayers, runCleanup, playersToRemove);
			return true;
		}

		public bool Any()
		{
			return ProspectName is not null
				|| LobbyPrivacy != ELobbyPrivacy.Unknown
				|| Difficulty != EMissionDifficulty.None
				|| Hardcore.HasValue
				|| DropZone.HasValue
				|| ListPlayers
				|| RunCleanup
				|| PlayersToRemove is not null;
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
