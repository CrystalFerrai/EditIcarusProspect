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
using UeSaveGame;
using UeSaveGame.DataTypes;
using UeSaveGame.PropertyTypes;
using UeSaveGame.StructData;

namespace EditIcarusProspect
{
	/// <summary>
	/// Processes/updates a prospect save
	/// </summary>
	internal class ProspectEditor
	{
		private readonly Logger mLogger;

		public ProspectEditor(Logger logger)
		{
			mLogger = logger;
		}

		/// <summary>
		/// Process/update a prospect
		/// </summary>
		/// <param name="prospect">The prospect to update</param>
		/// <param name="options">Decsription of updates to perform</param>
		/// <returns>True if the prospect has been modified as a result of this run, else false</returns>
		public bool Run(ProspectSave prospect, ProgramOptions options)
		{
			ArrayProperty? stateRecorderBlobs = prospect.ProspectData[0].Property as ArrayProperty;
			if (stateRecorderBlobs?.Value == null)
			{
				mLogger.LogError("Error reading prospect. Failed to locate state recorder array at index 0.");
				return false;
			}

			mLogger.Log(LogLevel.Important, "Processing...");

			bool changed = false;

			if (options.ProspectName is not null)
			{
				if (!UpdateProspectName(prospect, options.ProspectName))
				{
					return false;
				}
				changed = true;
			}

			if (options.LobbyPrivacy != ELobbyPrivacy.Unknown)
			{
				if (!UpdateLobbyPrivacy(prospect, options.LobbyPrivacy))
				{
					return false;
				}
				changed = true;
			}

			if (options.Difficulty != EMissionDifficulty.None)
			{
				if (!UpdateDifficulty(prospect, options.Difficulty))
				{
					return false;
				}
				changed = true;
			}

			if (options.Hardcore.HasValue)
			{
				if (!UpdateHardcore(prospect, options.Hardcore.Value))
				{
					return false;
				}
				changed = true;
			}

			if (options.DropZone.HasValue)
			{
				if (!UpdateDropZone(prospect, options.DropZone.Value))
				{
					return false;
				}
				changed = true;
			}

			if (options.ListPlayers)
			{
				if (!ListPlayers(prospect))
				{
					return false;
				}
			}

			if (options.RunCleanup)
			{
				if (!CleanupUnassociatedRecorders(prospect))
				{
					return false;
				}
				changed = true;
			}

			if (options.PlayersToRemove is not null)
			{
				if (!RemovePlayers(prospect, options.PlayersToRemove))
				{
					return false;
				}
				changed = true;
			}

			return changed;
		}

		private bool UpdateProspectName(ProspectSave prospect, string name)
		{
			string oldName = prospect.ProspectInfo.ProspectID;

			FProspectInfo prospectInfo = prospect.ProspectInfo;
			prospectInfo.ProspectID = name;
			prospect.ProspectInfo = prospectInfo;

			StrProperty? prospectIdProperty = GetProspectInfoProperty<StrProperty>(prospect, nameof(FProspectInfo.ProspectID));
			if (prospectIdProperty is null)
			{
				return false;
			}

			prospectIdProperty.Value = new FString(name);

			mLogger.Log(LogLevel.Information, $"Prospect name changed from '{oldName}' to '{name}'");

			return true;
		}

		private bool UpdateLobbyPrivacy(ProspectSave prospect, ELobbyPrivacy lobbyPrivacy)
		{
			EnumProperty? lobbyPrivacyProperty = prospect.ProspectData.FirstOrDefault(p => p.Name.Equals("LobbyPrivacy"))?.Property as EnumProperty;
			if (lobbyPrivacyProperty is null)
			{
				mLogger.LogError("Error locating lobby privacy property");
				return false;
			}

			string oldLobbyPrivacy = GetEnumValue(lobbyPrivacyProperty.Value?.Value, ELobbyPrivacy.Unknown.ToString());

			lobbyPrivacyProperty.Value = new FString($"{nameof(ELobbyPrivacy)}::{lobbyPrivacy}");

			mLogger.Log(LogLevel.Information, $"Lobby privacy changed from '{oldLobbyPrivacy}' to '{lobbyPrivacy}'.");

			return true;
		}

		private bool UpdateDifficulty(ProspectSave prospect, EMissionDifficulty difficulty)
		{
			FProspectInfo prospectInfo = prospect.ProspectInfo;
			prospectInfo.Difficulty = difficulty.ToString();
			prospect.ProspectInfo = prospectInfo;

			EnumProperty? difficultyProperty = GetProspectInfoProperty<EnumProperty>(prospect, nameof(FProspectInfo.Difficulty));
			if (difficultyProperty is null)
			{
				return false;
			}

			string oldDifficulty = GetEnumValue(difficultyProperty.Value?.Value, EMissionDifficulty.None.ToString());

			difficultyProperty.Value = new FString($"{nameof(EMissionDifficulty)}::{difficulty}");

			mLogger.Log(LogLevel.Information, $"Difficulty changed from '{oldDifficulty}' to '{difficulty}'.");

			return true;
		}

		private bool UpdateHardcore(ProspectSave prospect, bool enable)
		{
			FProspectInfo prospectInfo = prospect.ProspectInfo;
			prospectInfo.NoRespawns = enable;
			prospect.ProspectInfo = prospectInfo;

			BoolProperty? noRespawnsProperty = GetProspectInfoProperty<BoolProperty>(prospect, nameof(FProspectInfo.NoRespawns));
			if (noRespawnsProperty is null)
			{
				return false;
			}

			string oldEnable = noRespawnsProperty.Value ? "on" : "off";

			noRespawnsProperty.Value = enable;

			mLogger.Log(LogLevel.Information, $"Hardcore changed from '{oldEnable}' to '{(enable ? "on" : "off")}'.");

			return true;
		}

		private bool UpdateDropZone(ProspectSave prospect, int dropZone)
		{
			FProspectInfo prospectInfo = prospect.ProspectInfo;
			prospectInfo.SelectedDropPoint = dropZone;
			prospect.ProspectInfo = prospectInfo;

			IntProperty? selectedDropPointProperty = GetProspectInfoProperty<IntProperty>(prospect, nameof(FProspectInfo.SelectedDropPoint));
			if (selectedDropPointProperty is null)
			{
				return false;
			}

			int oldDropZone = selectedDropPointProperty.Value;

			selectedDropPointProperty.Value = dropZone;

			mLogger.Log(LogLevel.Information, $"Drop zone changed from '{oldDropZone}' to '{dropZone}'.");

			return true;
		}

		private bool ListPlayers(ProspectSave prospect)
		{
			CharactersData characters = CharacterReader.ReadCharacters(prospect, mLogger, true);

			mLogger.Log(LogLevel.Information, $"Listing {characters.Characters.Count} characters...");

			mLogger.LogEmptyLine(LogLevel.Information);
			mLogger.Log(LogLevel.Information, "PlayerID-CharacterSlot  CharacterName           DropShipLocation");
			foreach (CharacterData character in characters.Characters)
			{
				string output = $"{character.ID,-24}{character.Name,-24}";

				if (character.RocketRecorder.HasValue)
				{
					foreach (FPropertyTag prop in character.RocketRecorder.Value.Data)
					{
						if (prop.Name.Value.Equals("SpawnLocation", StringComparison.OrdinalIgnoreCase))
						{
							VectorStruct spawnLocationStruct = (VectorStruct)prop.Property!.Value!;
							output += $"{spawnLocationStruct.Value.X:0},{spawnLocationStruct.Value.Y:0}";
							break;
						}
					}
				}
				else
				{
					output += "[No Rocket]";
				}
				mLogger.Log(LogLevel.Information, output);
			}

			if (characters.UnownedPlayerStates is not null && characters.UnownedPlayerStates.Count > 0)
			{
				mLogger.LogEmptyLine(LogLevel.Information);
				mLogger.Log(LogLevel.Warning, $"Found {characters.UnownedPlayerStates.Count} player states not associated with a valid player.");
				foreach (RecorderData recorder in characters.UnownedPlayerStates)
				{
					FPropertyTag? idProperty = recorder.Data.FirstOrDefault(p => p.Name.Value.Equals("PlayerCharacterID", StringComparison.OrdinalIgnoreCase));
					CharacterID? id = null;
					if (idProperty is not null)
					{
						id = CharacterReader.ReadCharacterID(idProperty.Property!);
					}

					if (id.HasValue)
					{
						mLogger.Log(LogLevel.Warning, id.Value.ToString());
					}
					else
					{
						mLogger.Log(LogLevel.Warning, "(No ID)");
					}
				}
			}

			if (characters.UnownedRocketSpawns is not null && characters.UnownedRocketSpawns.Count > 0)
			{
				mLogger.LogEmptyLine(LogLevel.Information);
				mLogger.Log(LogLevel.Warning, $"Found {characters.UnownedRocketSpawns.Count} rocket spawn actors not owned by any player.");
				foreach (RecorderData recorder in characters.UnownedRocketSpawns)
				{
					int actorId = -1;
					FVector? actorLocation = null;
					foreach (FPropertyTag prop in recorder.Data)
					{
						if (prop.Name.Value.Equals("IcarusActorGUID", StringComparison.OrdinalIgnoreCase))
						{
							actorId = (int)prop.Property!.Value!;
						}
						else if (prop.Name.Value.Equals("ActorTransform", StringComparison.OrdinalIgnoreCase))
						{
							PropertiesStruct transformStruct = (PropertiesStruct)prop.Property!.Value!;
							FPropertyTag translationProp = transformStruct.Properties.First(p => p.Name.Value.Equals("Translation", StringComparison.OrdinalIgnoreCase));
							VectorStruct locationStruct = (VectorStruct)translationProp.Property!.Value!;
							actorLocation = locationStruct.Value;
						}
					}
					mLogger.Log(LogLevel.Warning, $"[{(actorId >= 0 ? actorId.ToString() : "No ID")}] ({(actorLocation.HasValue ? $"{actorLocation.Value.X:0},{actorLocation.Value.Y:0}" : "No Location")})");
				}
			}

			if (characters.UnownedRockets is not null && characters.UnownedRockets.Count > 0)
			{
				mLogger.LogEmptyLine(LogLevel.Information);
				mLogger.Log(LogLevel.Warning, $"Found {characters.UnownedRockets.Count} rocket actors not owned by any player.");
				foreach (RecorderData recorder in characters.UnownedRockets)
				{
					int actorId = -1;
					FVector? actorLocation = null;
					foreach (FPropertyTag prop in recorder.Data)
					{
						if (prop.Name.Value.Equals("IcarusActorGUID", StringComparison.OrdinalIgnoreCase))
						{
							actorId = (int)prop.Property!.Value!;
						}
						else if (prop.Name.Value.Equals("SpawnLocation", StringComparison.OrdinalIgnoreCase))
						{
							VectorStruct spawnLocationStruct = (VectorStruct)prop.Property!.Value!;
							actorLocation = spawnLocationStruct.Value;
						}
					}
					mLogger.Log(LogLevel.Warning, $"[{(actorId >= 0 ? actorId.ToString() : "No ID")}] ({(actorLocation.HasValue ? $"{actorLocation.Value.X:0},{actorLocation.Value.Y:0}" : "No Location")})");
				}
			}

			mLogger.LogEmptyLine(LogLevel.Information);

			return true;
		}

		private bool CleanupUnassociatedRecorders(ProspectSave prospect)
		{
			CharactersData characters = CharacterReader.ReadCharacters(prospect, mLogger, true);

			HashSet<int> recordersToRemove = new();

			if (characters.UnownedPlayerStates is not null && characters.UnownedPlayerStates.Count > 0)
			{
				mLogger.Log(LogLevel.Information, $"Removing {characters.UnownedPlayerStates.Count} unassociated player states...");
				foreach (RecorderData recorder in characters.UnownedPlayerStates)
				{
					recordersToRemove.Add(recorder.Index);
				}
			}

			if (characters.UnownedRocketSpawns is not null && characters.UnownedRocketSpawns.Count > 0)
			{
				mLogger.Log(LogLevel.Information, $"Removing {characters.UnownedRocketSpawns.Count} unowned rocket spawns...");
				foreach (RecorderData recorder in characters.UnownedRocketSpawns)
				{
					recordersToRemove.Add(recorder.Index);
				}
			}

			if (characters.UnownedRockets is not null && characters.UnownedRockets.Count > 0)
			{
				mLogger.Log(LogLevel.Information, $"Removing {characters.UnownedRockets.Count} unowned rockets...");
				foreach (RecorderData recorder in characters.UnownedRockets)
				{
					recordersToRemove.Add(recorder.Index);
				}
			}

			if (recordersToRemove.Count == 0)
			{
				mLogger.Log(LogLevel.Information, "Found nothing to cleanup.");
				return true;
			}

			RemoveRecorders(prospect, recordersToRemove);

			return true;
		}

		private bool RemovePlayers(ProspectSave prospect, IReadOnlyList<string> playersToRemove)
		{
			List<CharacterID> inputCharacters = new();
			foreach (string input in playersToRemove)
			{
				if (!CharacterID.TryParse(input, out CharacterID characterId))
				{
					mLogger.LogError($"Could not parse input as a character ID: {input}");
					return false;
				}
				inputCharacters.Add(characterId);
			}

			HashSet<CharacterData> charactersToRemove = new();

			CharactersData allCharacters = CharacterReader.ReadCharacters(prospect, mLogger);
			foreach (CharacterData character in allCharacters.Characters)
			{
				foreach (CharacterID inputCharacter in inputCharacters)
				{
					if (character.ID.Matches(inputCharacter))
					{
						charactersToRemove.Add(character);
					}
				}
			}

			if (charactersToRemove.Count == 0)
			{
				mLogger.Log(LogLevel.Information, "No players/characters matching the supplied IDs were found in the prospect.");
				return false;
			}

			mLogger.Log(LogLevel.Information, "Removing the following characters from the prospect:");
			foreach (CharacterData character in charactersToRemove)
			{
				mLogger.Log(LogLevel.Information, $"  {character.ToString()}");
			}

			// Remove from json associated members
			for (int i = prospect.ProspectInfo.AssociatedMembers.Count - 1; i >= 0; --i)
			{
				FAssociatedMember member = prospect.ProspectInfo.AssociatedMembers[i];
				CharacterID id = new(member.UserID, member.ChrSlot);
				foreach (CharacterData removeCharacter in charactersToRemove)
				{
					if (removeCharacter.ID.Matches(id))
					{
						prospect.ProspectInfo.AssociatedMembers.RemoveAt(i);
						break;
					}
				}
			}

			// Remove from binary associated members
			{
				ArrayProperty? associatedMembersProperty = GetProspectInfoProperty<ArrayProperty>(prospect, nameof(FProspectInfo.AssociatedMembers));
				if (associatedMembersProperty is not null)
				{
					List<FProperty> membersToKeep = new();
					foreach (FProperty memberProp in associatedMembersProperty.Value!)
					{
						bool keep = true;
						CharacterID? id = CharacterReader.ReadCharacterID(memberProp);
						if (id.HasValue)
						{
							foreach (CharacterData removeCharacter in charactersToRemove)
							{
								if (removeCharacter.ID.Matches(id.Value))
								{
									keep = false;
									break;
								}
							}
						}
						if (keep)
						{
							membersToKeep.Add(memberProp);
						}
					}
					associatedMembersProperty.Value = membersToKeep.ToArray();
				}
			}

			// Remove from recorders
			HashSet<int> historyIndicesToRemove = charactersToRemove.Select(c => c.HistoryIndex).ToHashSet();
			foreach (FPropertyTag prop in allCharacters.PlayerHistoryRecorder.Data)
			{
				if (prop.Name.Value.Equals("SavedHistoryData"))
				{
					ArrayProperty savedHistoryProperty = (ArrayProperty)prop.Property!;
					List<FProperty> historyToKeep = new();
					for (int i = 0; i < savedHistoryProperty.Value!.Length; ++i)
					{
						if (historyIndicesToRemove.Contains(i)) continue;
						historyToKeep.Add(((FProperty[])savedHistoryProperty.Value)[i]);
					}
					savedHistoryProperty.Value = historyToKeep.ToArray();

					break;
				}
			}

			HashSet<int> recordersToRemove = new();
			foreach (CharacterData character in charactersToRemove)
			{
				recordersToRemove.Add(character.PlayerRecorder.Index);
				if (character.PlayerStateRecorder.HasValue)
				{
					recordersToRemove.Add(character.PlayerStateRecorder.Value.Index);
				}
				if (character.RocketSpawnRecorder.HasValue)
				{
					recordersToRemove.Add(character.RocketSpawnRecorder.Value.Index);
				}
				if (character.RocketRecorder.HasValue)
				{
					recordersToRemove.Add(character.RocketRecorder.Value.Index);
				}
			}
			RemoveRecorders(prospect, recordersToRemove);

			return true;
		}

		private PropertiesStruct? GetProspectInfo(ProspectSave prospect)
		{
			StructProperty? prospectInfoProperty = prospect.ProspectData.FirstOrDefault(p => p.Name.Equals("ProspectInfo"))?.Property as StructProperty;
			if (prospectInfoProperty is null)
			{
				mLogger.LogError("Error locating prospect info property inside binary blob");
				return null;
			}

			PropertiesStruct? prospectInfoPropertyData = prospectInfoProperty.Value as PropertiesStruct;
			if (prospectInfoPropertyData is null)
			{
				mLogger.LogError("Error reading prospect info property inside binary blob");
				return null;
			}

			return prospectInfoPropertyData;
		}

		private T? GetProspectInfoProperty<T>(ProspectSave prospect, string propertyName) where T : FProperty
		{
			PropertiesStruct? prospectInfo = GetProspectInfo(prospect);
			if (prospectInfo is null) return null;

			T? property = prospectInfo.Properties.FirstOrDefault(p => p.Name.Equals(propertyName))?.Property as T;
			if (property is null)
			{
				mLogger.LogError($"Error locating property '{propertyName}' inside binary blob");
			}
			return property;
		}

		private static string GetEnumValue(string? enumPair, string defaultValue)
		{
			if (enumPair is null) return defaultValue;

			int separatorIndex = enumPair.LastIndexOf("::");
			if (separatorIndex >= 0)
			{
				return enumPair[(separatorIndex + 2)..];
			}

			return enumPair;
		}

		private static void RemoveRecorders(ProspectSave prospect, IReadOnlySet<int> recordersToRemove)
		{
			ArrayProperty recorderProperties = (ArrayProperty)prospect.ProspectData[0].Property!;
			List<FProperty> propsToKeep = new();
			for (int i = 0; i < recorderProperties.Value!.Length; ++i)
			{
				if (recordersToRemove.Contains(i)) continue;
				propsToKeep.Add(((FProperty[])recorderProperties.Value)[i]);
			}
			recorderProperties.Value = propsToKeep.ToArray();
		}
	}
}
