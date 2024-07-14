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
using System.Diagnostics.CodeAnalysis;
using UeSaveGame;
using UeSaveGame.PropertyTypes;
using UeSaveGame.StructData;

namespace EditIcarusProspect
{
	internal static class CharacterReader
	{
		/// <summary>
		/// Reads data about characters from recorders in a prospect
		/// </summary>
		/// <param name="prospect">The prospect to read</param>
		/// <param name="logger">For logging errors</param>
		/// <param name="sort">Whether to sort the list of characters</param>
		public static CharactersData ReadCharacters(ProspectSave prospect, Logger logger, bool sort = false)
		{
			List<CharacterData> characters = new();
			CharactersData result = new() { Characters = characters };

			HashSet<string> recordersToRead = new(StringComparer.OrdinalIgnoreCase)
			{
				"/Script/Icarus.PlayerHistoryRecorderComponent",
				"/Script/Icarus.DynamicRocketSpawnRecorderComponent",
				"/Script/Icarus.RocketRecorderComponent",
				"/Script/Icarus.PlayerRecorderComponent",
				"/Script/Icarus.PlayerStateRecorderComponent"
			};

			UProperty[] recorderProperties = (UProperty[])prospect.ProspectData[0].Value!;

			List<RecorderData> playerRecorders = new();
			ArrayProperty? savedHistoryProperty = null;
			Dictionary<CharacterID, string> characterNameMap = new();
			Dictionary<CharacterID, int> characterHistoryIndexMap = new();
			Dictionary<CharacterID, RecorderData?> playerStateRecorderMap = new();
			Dictionary<int, RecorderData?> rocketSpawnRecorderMap = new();
			Dictionary<int, RecorderData?> rocketRecorderMap = new();

			for (int i = 0; i < recorderProperties.Length; ++i)
			{
				StructProperty recorderProperty = (StructProperty)recorderProperties[i];
				PropertiesStruct recorderValue = (PropertiesStruct)recorderProperty.Value!;
				string recorderName = ((FString)recorderValue.Properties[0].Value!).Value;

				if (!recordersToRead.Contains(recorderName)) continue;

				IList<UProperty> recorderData = ProspectSerlializationUtil.DeserializeRecorderData(recorderValue.Properties[1]);
				RecorderData recorder = new() { Name = recorderName, Index = i, Data = recorderData };

				if (recorderName.Equals("/Script/Icarus.PlayerRecorderComponent", StringComparison.OrdinalIgnoreCase))
				{
					playerRecorders.Add(recorder);
				}
				else if (recorderName.Equals("/Script/Icarus.PlayerStateRecorderComponent", StringComparison.OrdinalIgnoreCase))
				{
					foreach (UProperty prop in recorder.Data)
					{
						if (prop.Name.Value.Equals("PlayerCharacterID", StringComparison.OrdinalIgnoreCase))
						{
							CharacterID? id = ReadCharacterID(prop);
                            if (id.HasValue)
                            {
								playerStateRecorderMap.Add(id.Value, recorder);
                            }
                        }
					}
				}
				else if (recorderName.Equals("/Script/Icarus.DynamicRocketSpawnRecorderComponent", StringComparison.OrdinalIgnoreCase))
				{
					foreach (UProperty prop in recorder.Data)
					{
						if (prop.Name.Value.Equals("IcarusActorGUID", StringComparison.OrdinalIgnoreCase))
						{
							int uid = (int)prop.Value!;
							rocketSpawnRecorderMap.Add(uid, recorder);
						}
					}
				}
				else if (recorderName.Equals("/Script/Icarus.RocketRecorderComponent", StringComparison.OrdinalIgnoreCase))
				{
					foreach (UProperty prop in recorder.Data)
					{
						if (prop.Name.Value.Equals("IcarusActorGUID", StringComparison.OrdinalIgnoreCase))
						{
							int uid = (int)prop.Value!;
							rocketRecorderMap.Add(uid, recorder);
						}
					}
				}
				else if (recorderName.Equals("/Script/Icarus.PlayerHistoryRecorderComponent", StringComparison.OrdinalIgnoreCase))
				{
					result.PlayerHistoryRecorder = recorder;

					foreach (UProperty prop in result.PlayerHistoryRecorder.Data)
					{
						if (prop.Name.Value.Equals("SavedHistoryData"))
						{
							savedHistoryProperty = (ArrayProperty)prop;
							for (int j = 0; j < savedHistoryProperty.Value!.Length; ++j)
							{
								UProperty history = ((UProperty[])savedHistoryProperty.Value)[j];

								string? id = null;
								int slot = -1;
								string? name = null;

								PropertiesStruct savedHistoryData = (PropertiesStruct)history.Value!;
								foreach (UProperty historyProp in savedHistoryData.Properties)
								{
									if (historyProp.Name.Value.Equals("UserID", StringComparison.OrdinalIgnoreCase))
									{
										id = ((FString)historyProp.Value!).Value;
									}
									else if (historyProp.Name.Value.Equals("ChrSlot", StringComparison.OrdinalIgnoreCase))
									{
										slot = (int)historyProp.Value!;
									}
									else if (historyProp.Name.Value.Equals("CachedCharacterName", StringComparison.OrdinalIgnoreCase))
									{
										name = ((FString)historyProp.Value!).Value;
									}
								}

								if (id is not null && slot >= 0 && name is not null)
								{
									CharacterID charId = new(id, slot);
									characterNameMap.Add(charId, name);
									characterHistoryIndexMap.Add(charId, j);
								}
							}
							break;
						}
					}
				}
			}

			foreach (RecorderData recorder in playerRecorders)
			{
				CharacterID? charId = null;
				int rocketSpawnId = -1;
				int rocketId = -1;
				foreach (UProperty prop in recorder.Data)
				{
					if (prop.Name.Value.Equals("PlayerCharacterID", StringComparison.OrdinalIgnoreCase))
					{
						charId = ReadCharacterID(prop);
					}
					else if (prop.Name.Value.Equals("AssignedDropshipSpawnUID", StringComparison.OrdinalIgnoreCase))
					{
						rocketSpawnId = (int)prop.Value!;
					}
					else if (prop.Name.Value.Equals("AssignedDropshipUID", StringComparison.OrdinalIgnoreCase))
					{
						rocketId = (int)prop.Value!;
					}
				}
				if (!charId.HasValue)
				{
					logger.Log(LogLevel.Warning, "Found character data with missing ID. This data will be ignored.");
					continue;
				}

				if (!characterNameMap.TryGetValue(charId.Value, out string? charName))
				{
					charName = null;
				}

				if (!characterHistoryIndexMap.TryGetValue(charId.Value, out int historyIndex))
				{
					historyIndex = -1;
				}

				if (playerStateRecorderMap.TryGetValue(charId.Value, out RecorderData? playerStateRecorder))
				{
					playerStateRecorderMap.Remove(charId.Value);
				}
				else
				{
					playerStateRecorder = null;
				}

				if (rocketSpawnId >= 0 && rocketSpawnRecorderMap.TryGetValue(rocketSpawnId, out RecorderData? rocketSpawnRecorder))
				{
					rocketSpawnRecorderMap.Remove(rocketSpawnId);
				}
				else
				{
					rocketSpawnRecorder = null;
				}

				if (rocketId >= 0 && rocketRecorderMap.TryGetValue(rocketId, out RecorderData? rocketRecorder))
				{
					rocketRecorderMap.Remove(rocketId);
				}
				else
				{
					rocketRecorder = null;
				}

				characters.Add(new(charId.Value)
				{
					Name = charName,
					RocketSpawnId = rocketSpawnId,
					RocketId = rocketId,
					HistoryIndex = historyIndex,
					PlayerRecorder = recorder,
					PlayerStateRecorder = playerStateRecorder,
					RocketSpawnRecorder = rocketSpawnRecorder,
					RocketRecorder = rocketRecorder
				});
			}

			if (sort)
			{
				characters.Sort();
			}

			if (playerStateRecorderMap.Count > 0)
			{
				result.UnownedPlayerStates = new List<RecorderData>(playerStateRecorderMap.Values.Select(r => r!.Value));
			}
			if (rocketSpawnRecorderMap.Count > 0)
			{
				result.UnownedRocketSpawns = new List<RecorderData>(rocketSpawnRecorderMap.Values.Select(r => r!.Value));
			}
			if (rocketRecorderMap.Count > 0)
			{
				result.UnownedRockets = new List<RecorderData>(rocketRecorderMap.Values.Select(r => r!.Value));
			}

			return result;
		}

		/// <summary>
		/// Attempts to read a character id from a struct property
		/// </summary>
		/// <param name="idProperty">The struct property containing a character id</param>
		/// <returns>The character id, or null if no character id could be read</returns>
		public static CharacterID? ReadCharacterID(UProperty idProperty)
		{
			string? id = null;
			int slot = -1;
			if ((PropertiesStruct)idProperty.Value! is not PropertiesStruct charIdStruct)
			{
				return null;
			}
			foreach (UProperty prop in charIdStruct.Properties)
			{
				if (prop.Name.Value.Equals("UserID", StringComparison.OrdinalIgnoreCase) ||
					prop.Name.Value.Equals("PlayerID", StringComparison.OrdinalIgnoreCase))
				{
					id = ((FString)prop.Value!).Value;
				}
				else if (prop.Name.Value.Equals("ChrSlot", StringComparison.OrdinalIgnoreCase))
				{
					slot = (int)prop.Value!;
				}
			}

			if (id is null) return null;
			return new(id, slot);
		}
	}

	/// <summary>
	/// Data returned from CharacterReader.ReadCharacters
	/// </summary>
	internal struct CharactersData
	{
		public IList<CharacterData> Characters;

		public RecorderData PlayerHistoryRecorder;

		public IReadOnlyList<RecorderData>? UnownedPlayerStates;
		public IReadOnlyList<RecorderData>? UnownedRocketSpawns;
		public IReadOnlyList<RecorderData>? UnownedRockets;

		public override readonly string ToString()
		{
			return $"{Characters.Count} characters";
		}
	}

	/// <summary>
	/// Data about a specific character
	/// </summary>
	internal struct CharacterData : IEquatable<CharacterData>, IComparable<CharacterData>
	{
		public readonly CharacterID ID;
		public string? Name;
		public int RocketSpawnId;
		public int RocketId;
		public int HistoryIndex;

		public RecorderData PlayerRecorder;
		public RecorderData? PlayerStateRecorder;
		public RecorderData? RocketSpawnRecorder;
		public RecorderData? RocketRecorder;

		public CharacterData(CharacterID id)
		{
			ID = id;
		}

		public override readonly int GetHashCode()
		{
			return ID.GetHashCode();
		}

		public override readonly bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is CharacterData other && Equals(other);
		}

		public readonly bool Equals(CharacterData other)
		{
			return ID.Equals(other.ID);
		}

		public readonly int CompareTo(CharacterData other)
		{
			return ID.CompareTo(other.ID);
		}

		public override readonly string ToString()
		{
			return $"[{ID}] {Name}";
		}
	}

	/// <summary>
	/// A unique identifier for a character
	/// </summary>
	internal readonly struct CharacterID : IEquatable<CharacterID>, IComparable<CharacterID>
	{
		public readonly string? PlayerID;
		public readonly int Slot;

		public CharacterID()
		{
			PlayerID = null;
			Slot = -1;
		}

		public CharacterID(string playerID, int slot)
		{
			PlayerID = playerID;
			Slot = slot;
		}

		public static bool TryParse(string value, out CharacterID result)
		{
			string[] parts = value.Split('-');
			if (parts.Length == 1)
			{
				result = new(parts[0], -1);
				return true;
			}
			else if (parts.Length == 2)
			{
				if (int.TryParse(parts[1], out int slot))
				{
					result = new(parts[0], slot);
					return true;
				}
			}

			result = default;
			return false;
		}

		public readonly bool Matches(CharacterID other)
		{
			if (PlayerID != other.PlayerID) return false;
			if (Slot < 0 || other.Slot < 0) return true;
			return Slot == other.Slot;
		}

		public override readonly int GetHashCode()
		{
			return HashCode.Combine(PlayerID, Slot);
		}

		public override readonly bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is CharacterID other && Equals(other);
		}

		public readonly bool Equals(CharacterID other)
		{
			if (PlayerID is null) return other.PlayerID is null && Slot.Equals(other.Slot);
			return PlayerID.Equals(other.PlayerID) && Slot.Equals(other.Slot);
		}

		public readonly int CompareTo(CharacterID other)
		{
			if (PlayerID is null)
			{
				if (other.PlayerID is not null)
				{
					return -1;
				}
				return Slot.CompareTo(other.Slot);
			}

			int result = PlayerID.CompareTo(other.PlayerID);
			if (result == 0)
			{
				result = Slot.CompareTo(other.Slot);
			}
			return result;
		}

		public override readonly string ToString()
		{
			return $"{PlayerID}-{Slot}";
		}

		public static bool operator ==(CharacterID a, CharacterID b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(CharacterID a, CharacterID b)
		{
			return !a.Equals(b);
		}
	}

	/// <summary>
	/// Data about a recorder from a prospect blob
	/// </summary>
	internal struct RecorderData
	{
		public string Name;
		public int Index;
		public IList<UProperty> Data;

		public override readonly string ToString()
		{
			return $"[{Index}] {Name} - {Data.Count} properties";
		}
	}
}
