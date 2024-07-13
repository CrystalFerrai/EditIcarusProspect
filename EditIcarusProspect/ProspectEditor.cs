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

using IcarusSaveLib;
using UeSaveGame;
using UeSaveGame.PropertyTypes;
using UeSaveGame.StructData;

namespace EditIcarusProspect
{
	internal class ProspectEditor
	{
		private readonly Logger mLogger;

		public ProspectEditor(Logger logger)
		{
			mLogger = logger;
		}

		public bool Run(ProspectSave prospect, ProgramOptions options)
		{
			ArrayProperty? stateRecorderBlobs = prospect.ProspectData[0] as ArrayProperty;
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
			EnumProperty? lobbyPrivacyProperty = prospect.ProspectData.FirstOrDefault(p => p.Name.Equals("LobbyPrivacy")) as EnumProperty;
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

			mLogger.Log(LogLevel.Information, "PlayerID-CharacterSlot  CharacterName           DropShipLocation");
			foreach (CharacterData character in characters.Characters)
			{
				string output = $"{character.ID,-24}{character.Name,-24}";

				foreach (UProperty prop in character.RocketRecorder.Data)
				{
					if (prop.Name.Value.Equals("SpawnLocation", StringComparison.OrdinalIgnoreCase))
					{
						VectorStruct spawnLocationStruct = (VectorStruct)prop.Value!;
						output += $"{spawnLocationStruct.Value.X:0},{spawnLocationStruct.Value.Y:0}";
						break;
					}
				}
				mLogger.Log(LogLevel.Information, output);
			}

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
			foreach (UProperty prop in prospect.ProspectData)
			{
				if (prop.Name.Value.Equals("ProspectInfo", StringComparison.OrdinalIgnoreCase))
				{
					PropertiesStruct infoProperties = (PropertiesStruct)prop.Value!;
					foreach (UProperty infoProp in infoProperties.Properties)
					{
						if (infoProp.Name.Value.Equals("AssociatedMembers"))
						{
							ArrayProperty membersArray = (ArrayProperty)infoProp;
							List<UProperty> membersToKeep = new();
							foreach (UProperty memberProp in membersArray.Value!)
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
							membersArray.Value = membersToKeep.ToArray();

							break;
						}
					}

					break;
				}
			}

			// Remove from recorders
			HashSet<int> historyIndicesToRemove = charactersToRemove.Select(c => c.HistoryIndex).ToHashSet();
			foreach (UProperty prop in allCharacters.PlayerHistoryRecorder.Data)
			{
				if (prop.Name.Value.Equals("SavedHistoryData"))
				{
					ArrayProperty savedHistoryProperty = (ArrayProperty)prop;
					List<UProperty> historyToKeep = new();
					for (int i = 0; i < savedHistoryProperty.Value!.Length; ++i)
					{
						if (historyIndicesToRemove.Contains(i)) continue;
						historyToKeep.Add(savedHistoryProperty.Value[i]);
					}
					savedHistoryProperty.Value = historyToKeep.ToArray();
					
					break;
				}
			}

			HashSet<int> recordersToRemove = new();
			foreach (CharacterData character in charactersToRemove)
			{
				recordersToRemove.Add(character.PlayerRecorder.Index);
				recordersToRemove.Add(character.PlayerStateRecorder.Index);
				recordersToRemove.Add(character.RocketSpawnRecorder.Index);
				recordersToRemove.Add(character.RocketRecorder.Index);
			}

			ArrayProperty recorderProperties = (ArrayProperty)prospect.ProspectData[0];
			List<UProperty> propsToKeep = new();
			for (int i = 0; i < recorderProperties.Value!.Length; ++i)
			{
				if (recordersToRemove.Contains(i)) continue;
				propsToKeep.Add(recorderProperties.Value[i]);
			}
			recorderProperties.Value = propsToKeep.ToArray();

			return true;
		}

		private PropertiesStruct? GetProspectInfo(ProspectSave prospect)
		{
			StructProperty? prospectInfoProperty = prospect.ProspectData.FirstOrDefault(p => p.Name.Equals("ProspectInfo")) as StructProperty;
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

		private T? GetProspectInfoProperty<T>(ProspectSave prospect, string propertyName) where T : UProperty
		{
			PropertiesStruct? prospectInfo = GetProspectInfo(prospect);
			if (prospectInfo is null) return null;

			T? property = prospectInfo.Properties.FirstOrDefault(p => p.Name.Equals(propertyName)) as T;
            if (property is null)
			{
				mLogger.LogError($"Error locating property '{propertyName}' inside binary blob");
			}
			return property;
        }

#if DEBUG
		private void DebugProspect(ProspectSave prospect)
		{
			UProperty[] recorderProperties = (UProperty[])prospect.ProspectData[0].Value!;

			Dictionary<string, int> stateRecorderCounts = new(StringComparer.OrdinalIgnoreCase);
			foreach (StructProperty recorderProperty in recorderProperties)
			{
				PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
				string recorderName = ((FString)recorderValue.Properties[0].Value!).Value;

				int value;
				if (!stateRecorderCounts.TryGetValue(recorderName, out value))
				{
					value = 0;
				}
				++value;

				stateRecorderCounts[recorderName] = value;
			}

			HashSet<string> recordersToRead = new(StringComparer.OrdinalIgnoreCase)
			{
				"/Script/Icarus.GameModeStateRecorderComponent",
				"/Script/Icarus.PlayerHistoryRecorderComponent",
				"/Script/Icarus.DynamicRocketSpawnRecorderComponent",
				"/Script/Icarus.RocketRecorderComponent",
				"/Script/Icarus.PlayerRecorderComponent",
				"/Script/Icarus.PlayerStateRecorderComponent"
			};

			List<KeyValuePair<string, IList<UProperty>>> recorderData = new();
			
			foreach (StructProperty recorderProperty in recorderProperties)
			{
				PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
				string recorderName = ((FString)recorderValue.Properties[0].Value!).Value;

				if (!recordersToRead.Contains(recorderName)) continue;

				recorderData.Add(new(recorderName, ProspectSerlializationUtil.DeserializeRecorderData(recorderValue.Properties[1])));
			}

			System.Diagnostics.Debugger.Break();
		}
#endif

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
	}
}
