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

			if (options.Mission.HasValue)
			{
				if (!ProcessMissionHistory(prospect, options.Mission.Value))
				{
					return false;
				}
				changed |= options.Mission.Value.Command != MissionCommand.List;
			}

			if (options.Prebuilt.HasValue)
			{
				if (!ProcessPrebuilts(prospect, options.Prebuilt.Value))
				{
					return false;
				}
				changed |= options.Prebuilt.Value.Command != PrebuiltCommand.List;
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

		private bool ProcessMissionHistory(ProspectSave prospect, MissionOptions options)
		{
			switch (options.Command)
			{
				case MissionCommand.List:
					return ListMissionHistory(prospect);
				case MissionCommand.Remove:
					return RemoveFromMissionHistory(prospect, options.Parameters);
				case MissionCommand.Clear:
					return ClearMissionHistory(prospect);
				default:
					mLogger.LogError("Invalid mission history command");
					return false;
			}
		}

		private bool ListMissionHistory(ProspectSave prospect)
		{
			if (!TryGetMissionHistoryProperty(prospect, out ArrayProperty? missionHistory, out PropertiesStruct? recorder, out IList<FPropertyTag>? recorderProperties))
			{
				mLogger.LogError("Error: Unable to locate mission history");
				return false;
			}

			mLogger.Log(LogLevel.Information, "Listing mission history records");
			mLogger.Log(LogLevel.Information, $"{"Index",-5}  {"Mission Name",-32}{"Status",-14}{"End Time"}");

			for (int i = 0; i < missionHistory.Value!.Length; ++i)
			{
				StructProperty historyProperty = (StructProperty)missionHistory.Value!.GetValue(i)!;

				string? name = null;
				int? status = null;
				int? endTime = null;

				foreach (FPropertyTag entryProperty in ((PropertiesStruct)(historyProperty.Value!)).Properties)
				{
					switch (entryProperty.Name.Value)
					{
						case "Mission":
							name = ((StrProperty)entryProperty.Property!).Value!.Value;
							break;
						case "Status":
							status = ((IntProperty)entryProperty.Property!).Value;
							break;
						case "MissionEndTime":
							endTime = ((IntProperty)entryProperty.Property!).Value;
							break;
					}
				}

				if (name is null) name = "NAME_MISSING";

				string statusStr;
				if (!status.HasValue) statusStr = "STATUS_MISSING";
				else statusStr = ((EMissionState)status).ToString();

				string? endTimeStr;
				if (!endTime.HasValue) endTimeStr = null;
				else endTimeStr = FormatTimestamp(endTime.Value);

				mLogger.Log(LogLevel.Information, $"{i,5}  {name,-32}{statusStr,-14}{endTimeStr}");
			}

			return true;
		}

		private bool RemoveFromMissionHistory(ProspectSave prospect, int[] missionsToRemove)
		{
			if (!TryGetMissionHistoryProperty(prospect, out ArrayProperty? missionHistory, out PropertiesStruct? recorder, out IList<FPropertyTag>? recorderProperties))
			{
				mLogger.LogError("Error: Unable to locate mission history");
				return false;
			}

			mLogger.Log(LogLevel.Information, $"Removing mission history records at indeces: {string.Join(',', missionsToRemove)}");

			HashSet<int> toRemove = new(missionsToRemove);

			FProperty[] newHistory = new FProperty[missionHistory.Value!.Length - missionsToRemove.Length];
			for (int inIndex = 0, outIndex = 0; inIndex < missionHistory.Value!.Length; ++inIndex)
			{
				if (toRemove.Contains(inIndex)) continue;

				newHistory[outIndex] = (FProperty)missionHistory.Value!.GetValue(inIndex)!;
				++outIndex;
			}
			missionHistory.Value = newHistory;

			recorder.Properties[1] = ProspectSerlializationUtil.SerializeRecorderData(recorder.Properties[1], recorderProperties);

			return true;
		}

		private bool ClearMissionHistory(ProspectSave prospect)
		{
			if (!TryGetMissionHistoryProperty(prospect, out ArrayProperty? missionHistory, out PropertiesStruct? recorder, out IList<FPropertyTag>? recorderProperties))
			{
				mLogger.LogError("Error: Unable to locate mission history");
				return false;
			}

			mLogger.Log(LogLevel.Information, "Removing all history records");

			missionHistory.Value = new FProperty[0];
			recorder.Properties[1] = ProspectSerlializationUtil.SerializeRecorderData(recorder.Properties[1], recorderProperties);

			return true;
		}

		private static bool TryGetMissionHistoryProperty(
			ProspectSave prospect,
			[NotNullWhen(true)] out ArrayProperty? array,
			[NotNullWhen(true)] out PropertiesStruct? recorder,
			[NotNullWhen(true)] out IList<FPropertyTag>? recorderProperties)
		{
			array = null;
			recorder = null;
			recorderProperties = null;

			FProperty[] recorders = (FProperty[])prospect.ProspectData[0].Property!.Value!;
			for (int i = 0; i < recorders.Length; ++i)
			{
				StructProperty recorderProperty = (StructProperty)recorders[i];
				PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
				string recorderName = ((FString)recorderValue.Properties[0].Property!.Value!).Value;

				if (!recorderName.Equals("/Script/Icarus.GameModeStateRecorderComponent")) continue;

				IList<FPropertyTag> properties = ProspectSerlializationUtil.DeserializeRecorderData(recorderValue.Properties[1]);

				FPropertyTag? missionHistoryProperty = properties.FirstOrDefault(p => p.Name.Equals("MissionHistory"));
				if (missionHistoryProperty is null)
				{
					return false;
				}

				array = (ArrayProperty)missionHistoryProperty.Property!;
				recorder = recorderValue;
				recorderProperties = properties;
				return true;
			}

			return false;
		}

		private bool ProcessPrebuilts(ProspectSave prospect, PrebuiltOptions options)
		{
			switch (options.Command)
			{
				case PrebuiltCommand.List:
					return ListPrebuilts(prospect);
				case PrebuiltCommand.Remove:
					return RemovePrebuilts(prospect, options.Parameters);
				case PrebuiltCommand.Clear:
					return ClearPrebuilts(prospect);
				default:
					mLogger.LogError("Invalid prebuilt command");
					return false;
			}
		}

		private bool ListPrebuilts(ProspectSave prospect)
		{
			mLogger.Log(LogLevel.Information, "Listing prebuilt structures");
			mLogger.Log(LogLevel.Information, $"{"Index",-5}  {"Structure Name",-32}{"Actors",-8}{"Location"}");

			FProperty[] recorderProperties = (FProperty[])prospect.ProspectData[0].Property!.Value!;
			for (int i = 0, prebuiltIndex = 0; i < recorderProperties.Length; ++i)
			{
				StructProperty recorderProperty = (StructProperty)recorderProperties[i];
				PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
				string recorderName = ((FString)recorderValue.Properties[0].Property!.Value!).Value;

				if (!recorderName.Equals("/Script/Icarus.PrebuiltStructureRecorderComponent")) continue;

				IList<FPropertyTag> properties = ProspectSerlializationUtil.DeserializeRecorderData(recorderValue.Properties[1]);

				string structureName = "NAME_MISSING";
				int actorCount = 0;
				FVector? structureLocation = null;
				foreach (FPropertyTag property in properties)
				{
					switch (property.Name.Value)
					{
						case "PrebuiltStructureName":
							structureName = ((NameProperty)property.Property!).Value!.Value;
							break;
						case "RelevantActorRecords":
							actorCount = ((ArrayProperty)property.Property!).Value!.Length;
							break;
						case "ActorTransform":
							structureLocation = ((VectorStruct)((PropertiesStruct)property.Property!.Value!).Properties.FirstOrDefault(p => p.Name.Equals("Translation"))?.Property!.Value!).Value;
							break;
					}
				}

				mLogger.Log(LogLevel.Information, $"{prebuiltIndex,-5}  {structureName,-32}{actorCount,-8}{FormatVector(structureLocation)}");

				++prebuiltIndex;
			}

			return true;
		}

		private bool RemovePrebuilts(ProspectSave prospect, int[] prebuiltsToRemove)
		{
			mLogger.Log(LogLevel.Information, $"Removing prebuilt structures at indeces: {string.Join(',', prebuiltsToRemove)}");
			return InternalRemovePrebuilts(prospect, false, prebuiltsToRemove);
		}

		private bool ClearPrebuilts(ProspectSave prospect)
		{
			mLogger.Log(LogLevel.Information, "Removing all prebuilt structures");
			return InternalRemovePrebuilts(prospect, true, new int[0]);
		}

		private bool InternalRemovePrebuilts(ProspectSave prospect, bool clear, int[] prebuiltsToRemove)
		{
			List<PropertiesStruct> prebuiltStructureRecorders = new();
			Dictionary<int, List<int>> actorToIndexMap = new();

			HashSet<int> recordersToRemove = new();
			HashSet<int> prebuiltRemoveSet = new(prebuiltsToRemove);

			void checkActor(int index, IList<FPropertyTag> properties)
			{
				FPropertyTag? actorGuidProperty = properties.FirstOrDefault(p => p.Name.Equals("IcarusActorGUID"));
				if (actorGuidProperty is not null && actorGuidProperty.Property is IntProperty asIntProperty && asIntProperty.Value != 0)
				{
					List<int>? value;
					if (!actorToIndexMap.TryGetValue(asIntProperty.Value, out value))
					{
						value = new();
						actorToIndexMap.Add(asIntProperty.Value, value);
					}
					value.Add(index);
				}
			}

			FProperty[] recorderProperties = (FProperty[])prospect.ProspectData[0].Property!.Value!;
			for (int i = 0, prebuiltIndex = 0; i < recorderProperties.Length; ++i)
			{
				StructProperty recorderProperty = (StructProperty)recorderProperties[i];
				PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
				string recorderName = ((FString)recorderValue.Properties[0].Property!.Value!).Value;

				if (recorderName.Equals("/Script/Icarus.PrebuiltStructureRecorderComponent"))
				{
					if (clear || prebuiltRemoveSet.Contains(prebuiltIndex))
					{
						prebuiltStructureRecorders.Add(recorderValue);
						recordersToRemove.Add(i);
					}
					++prebuiltIndex;
				}
				else if (recorderName.Equals("/Script/Icarus.BuildingGridRecorderComponent"))
				{
					IList<FPropertyTag> properties = ProspectSerlializationUtil.DeserializeRecorderData(recorderValue.Properties[1]);

					checkActor(i, properties);

					FPropertyTag? buildingGridRecordProperty = properties.FirstOrDefault(p => p.Name.Equals("BuildingGridRecord"));
					if (buildingGridRecordProperty is not null)
					{
						FPropertyTag? buildingTypesProperty = ((PropertiesStruct?)((StructProperty?)buildingGridRecordProperty.Property)?.Value)?.Properties.FirstOrDefault(p => p.Name.Equals("BuildingTypes"));
						if (buildingTypesProperty is not null)
						{
							FProperty[] buildingTypes = (FProperty[])((ArrayProperty)buildingTypesProperty.Property!).Value!;
							foreach (StructProperty buildingTypeProperty in buildingTypes)
							{
								FPropertyTag? buildingInstancesProperty = ((PropertiesStruct)buildingTypeProperty.Value!).Properties.FirstOrDefault(p => p.Name.Equals("BuildingInstances"));
								if (buildingInstancesProperty is not null)
								{
									FProperty[] buildingInstances = (FProperty[])((ArrayProperty)buildingInstancesProperty.Property!).Value!;
									foreach (StructProperty buildingInstanceProperty in buildingInstances)
									{
										IntProperty? uidProperty = (IntProperty?)((PropertiesStruct)buildingInstanceProperty.Value!).Properties.FirstOrDefault(p => p.Name.Equals("IcarusUID"))?.Property;
										if (uidProperty is not null)
										{
											List<int>? value;
											if (!actorToIndexMap.TryGetValue(uidProperty.Value, out value))
											{
												value = new();
												actorToIndexMap.Add(uidProperty.Value, value);
											}
											value.Add(i);
										}
									}
								}
							}
						}
					}
				}
				else
				{
					IList<FPropertyTag> properties = ProspectSerlializationUtil.DeserializeRecorderData(recorderValue.Properties[1]);
					checkActor(i, properties);
				}
			}

			HashSet<int> actorsToRemove = new();

			foreach (PropertiesStruct prebuiltStructureRecorder in prebuiltStructureRecorders)
			{
				IList<FPropertyTag> properties = ProspectSerlializationUtil.DeserializeRecorderData(prebuiltStructureRecorder.Properties[1]);

				foreach (FPropertyTag property in properties)
				{
					if (!property.Name.Equals("RelevantActorRecords")) continue;

					FProperty[] relevantActors = (FProperty[])((ArrayProperty)property.Property!).Value!;
					foreach(FProperty relevantActorProperty in relevantActors)
					{
						IntProperty? uidProperty = (IntProperty?)((PropertiesStruct)relevantActorProperty.Value!).Properties.FirstOrDefault(p => p.Name.Equals("RelevantActorIcarusUID"))?.Property;
						if (uidProperty is not null)
						{
							actorsToRemove.Add(uidProperty.Value);
						}
					}

					break;
				}
			}

			foreach (int actor in actorsToRemove)
			{
				List<int>? recorders;
				if (actorToIndexMap.TryGetValue(actor, out recorders))
				{
					if (recorders.Count > 1)
					{
						mLogger.Log(LogLevel.Debug, $"Found actor {actor} in {recorders.Count} recorders. Skipping");
					}
					else
					{
						foreach (int recorder in recorders)
						{
							recordersToRemove.Add(recorder);
						}
					}
				}
				else
				{
					mLogger.Log(LogLevel.Information, $"Could not locate actor to remove: {actor}");
				}
			}

			if (mLogger.LogLevel <= LogLevel.Debug)
			{
				mLogger.Log(LogLevel.Debug, "Removing recorders:");
				foreach (int index in recordersToRemove.OrderBy(i => i))
				{
					StructProperty recorderProperty = (StructProperty)recorderProperties[index];
					PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
					string recorderName = ((FString)recorderValue.Properties[0].Property!.Value!).Value;
					mLogger.Log(LogLevel.Debug, $"{index.ToString().PadLeft(7)} {recorderName}");
				}
			}

			List<FProperty> outRecorders = new();
			for (int i = 0; i < recorderProperties.Length; ++i)
			{
				if (recordersToRemove.Contains(i)) continue;

				outRecorders.Add(recorderProperties[i]);
			}

			prospect.ProspectData[0].Property!.Value = outRecorders.ToArray();

			return true;
		}

		private bool ListPlayers(ProspectSave prospect)
		{
			CharactersData characters = CharacterReader.ReadCharacters(prospect, mLogger, true);

			mLogger.Log(LogLevel.Information, $"Listing {characters.Characters.Count} characters");

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
			mLogger.Log(LogLevel.Information, "Performing record cleanup");

			CharactersData characters = CharacterReader.ReadCharacters(prospect, mLogger, true);

			HashSet<int> recordersToRemove = new();

			if (characters.UnownedPlayerStates is not null && characters.UnownedPlayerStates.Count > 0)
			{
				mLogger.Log(LogLevel.Information, $"Removing {characters.UnownedPlayerStates.Count} unassociated player states");
				foreach (RecorderData recorder in characters.UnownedPlayerStates)
				{
					recordersToRemove.Add(recorder.Index);
				}
			}

			if (characters.UnownedRocketSpawns is not null && characters.UnownedRocketSpawns.Count > 0)
			{
				mLogger.Log(LogLevel.Information, $"Removing {characters.UnownedRocketSpawns.Count} unowned rocket spawns");
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

		private static string FormatTimestamp(int value)
		{
			int seconds = value % 60;
			int minutes = value / 60 % 60;
			int hours = value / 3600 % 24;
			int days = value / 3600 / 24;

			return $"{days}:{hours:00}:{minutes:00}:{seconds:00}";
		}

		private static string? FormatVector(FVector? value)
		{
			if (!value.HasValue) return null;

			return $"{Math.Round(value.Value.X)}, {Math.Round(value.Value.Y)}, {Math.Round(value.Value.Z)}";
		}

		private enum EMissionState
		{
			InProgress,
			Completed,
			Abandoned,
			Failed,
			MAX,
		};
	}
}
