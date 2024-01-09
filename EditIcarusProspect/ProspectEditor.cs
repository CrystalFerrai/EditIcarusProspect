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
		TextWriter mOutputLog;
		TextWriter mErrorLog;
		TextWriter mWarningLog;

		public ProspectEditor(TextWriter outputLog, TextWriter errorLog, TextWriter warningLog)
		{
			mOutputLog = outputLog;
			mErrorLog = errorLog;
			mWarningLog = warningLog;
		}

		public bool Run(ProspectSave prospect, ProgramOptions options)
		{
			ArrayProperty? stateRecorderBlobs = prospect.ProspectData[0] as ArrayProperty;
			if (stateRecorderBlobs?.Value == null)
			{
				mErrorLog.WriteLine("Error reading prospect. Failed to locate state recorder array at index 0.");
				return false;
			}

			mOutputLog.WriteLine("Modifying prospect...");

			if (options.ProspectName is not null)
			{
				if (!UpdateProspectName(prospect, options.ProspectName))
				{
					return false;
				}
			}

			if (options.LobbyPrivacy != ELobbyPrivacy.Unknown)
			{
				if (!UpdateLobbyPrivacy(prospect, options.LobbyPrivacy))
				{
					return false;
				}
            }

			if (options.Difficulty != EMissionDifficulty.None)
			{
				if (!UpdateDifficulty(prospect, options.Difficulty))
				{
					return false;
				}
			}

			if (options.Hardcore.HasValue)
			{
				if (!UpdateHardcore(prospect, options.Hardcore.Value))
				{
					return false;
				}
			}

			if (options.DropZone.HasValue)
			{
				if (!UpdateDropZone(prospect, options.DropZone.Value))
				{
					return false;
				}
			}

			return true;
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

			mOutputLog.WriteLine($"Prospect name changed from '{oldName}' to '{name}'");

			return true;
		}

		private bool UpdateLobbyPrivacy(ProspectSave prospect, ELobbyPrivacy lobbyPrivacy)
		{
			EnumProperty? lobbyPrivacyProperty = prospect.ProspectData.FirstOrDefault(p => p.Name.Equals("LobbyPrivacy")) as EnumProperty;
			if (lobbyPrivacyProperty is null)
			{
				mErrorLog.WriteLine("Error locating lobby privacy property");
				return false;
			}

			string oldLobbyPrivacy = GetEnumValue(lobbyPrivacyProperty.Value?.Value, ELobbyPrivacy.Unknown.ToString());

			lobbyPrivacyProperty.Value = new FString($"{nameof(ELobbyPrivacy)}::{lobbyPrivacy}");

			mOutputLog.WriteLine($"Lobby privacy changed from '{oldLobbyPrivacy}' to '{lobbyPrivacy}'.");

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

			mOutputLog.WriteLine($"Difficulty changed from '{oldDifficulty}' to '{difficulty}'.");

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

			mOutputLog.WriteLine($"Hardcore changed from '{oldEnable}' to '{(enable ? "on" : "off")}'.");

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

			mOutputLog.WriteLine($"Drop zone changed from '{oldDropZone}' to '{dropZone}'.");

			return true;
		}

		private PropertiesStruct? GetProspectInfo(ProspectSave prospect)
		{
			StructProperty? prospectInfoProperty = prospect.ProspectData.FirstOrDefault(p => p.Name.Equals("ProspectInfo")) as StructProperty;
			if (prospectInfoProperty is null)
			{
				mErrorLog.WriteLine("Error locating prospect info property inside binary blob");
				return null;
			}

			PropertiesStruct? prospectInfoPropertyData = prospectInfoProperty.Value as PropertiesStruct;
			if (prospectInfoPropertyData is null)
			{
				mErrorLog.WriteLine("Error reading prospect info property inside binary blob");
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
				mErrorLog.WriteLine($"Error locating property '{propertyName}' inside binary blob");
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
	}
}
