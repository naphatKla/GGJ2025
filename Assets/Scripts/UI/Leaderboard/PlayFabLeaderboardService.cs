using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using Player;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.ProgressionModels;
using UnityEngine;
using ProgressionEmptyResponse = PlayFab.ProgressionModels.EmptyResponse;

namespace UI.Leaderboard
{
    public static class PlayFabLeaderboardService
    {
        public const string DefaultLeaderboardName = "highest_score";

        private const string FallbackTitleId = "16A02C";
        private const string GuestIdPrefsKey = "PlayFabLeaderboardGuestId";
        private const string GuestDisplayName = "Guest";
        private const int MaxPageSize = 100;

        private static string _loggedInCustomId;
        private static string _loggedInEntityId;
        private static UniTaskCompletionSource<bool> _loginCompletion;
        private static string _loginCustomId;

        [Serializable]
        private class EntryMetadata
        {
            public string displayName;
        }

        public class Entry
        {
            public string DisplayName;
            public int Score;
            public int Rank;
            public bool IsMine;
        }

        public class Snapshot
        {
            public readonly List<Entry> Entries = new();
            public int CurrentPlayerRank = -1;
            public uint EntryCount;
        }

        public static async UniTask<bool> SubmitHighestScoreAsync(
            PlayerData profile,
            int score,
            string leaderboardName = DefaultLeaderboardName)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            await EnsureLoggedInAsync(profile);

            if (string.IsNullOrEmpty(_loggedInEntityId))
                throw new InvalidOperationException("PlayFab entity id is missing after login.");

            var request = new UpdateLeaderboardEntriesRequest
            {
                LeaderboardName = leaderboardName,
                Entries = new List<LeaderboardEntryUpdate>
                {
                    new()
                    {
                        EntityId = _loggedInEntityId,
                        Scores = new List<string>
                        {
                            Mathf.Max(0, score).ToString(CultureInfo.InvariantCulture)
                        },
                        Metadata = BuildMetadata(profile.DisplayName)
                    }
                }
            };

            await UpdateLeaderboardEntriesAsync(request);
            return true;
        }

        public static async UniTask<Snapshot> GetTopScoresAsync(
            PlayerData profile,
            int maxEntries,
            string leaderboardName = DefaultLeaderboardName)
        {
            await EnsureLoggedInAsync(profile);

            var request = new GetEntityLeaderboardRequest
            {
                LeaderboardName = leaderboardName,
                PageSize = (uint)Mathf.Clamp(maxEntries, 1, MaxPageSize),
                StartingPosition = 1
            };

            var result = await GetLeaderboardAsync(request);
            var snapshot = new Snapshot
            {
                EntryCount = result.EntryCount
            };

            if (result.Rankings == null)
                return snapshot;

            for (int i = 0; i < result.Rankings.Count; i++)
            {
                var row = result.Rankings[i];
                var rank = row.Rank > 0 ? row.Rank : i + 1;
                var isMine = !string.IsNullOrEmpty(_loggedInEntityId)
                             && string.Equals(row.Entity?.Id, _loggedInEntityId, StringComparison.Ordinal);

                snapshot.Entries.Add(new Entry
                {
                    DisplayName = ResolveEntryDisplayName(row),
                    Score = ParseScore(row.Scores),
                    Rank = rank,
                    IsMine = isMine
                });

                if (isMine)
                    snapshot.CurrentPlayerRank = rank;
            }

            return snapshot;
        }

        private static async UniTask<bool> EnsureLoggedInAsync(PlayerData profile)
        {
            EnsureTitleId();

            var resolvedProfile = ResolveProfile(profile);
            var customId = ResolveCustomId(resolvedProfile);
            if (string.IsNullOrEmpty(customId))
                throw new InvalidOperationException("Unable to resolve PlayFab custom id.");

            if (IsLoggedInAs(customId))
                return true;

            if (_loginCompletion != null && string.Equals(_loginCustomId, customId, StringComparison.Ordinal))
                return await _loginCompletion.Task;

            var completion = new UniTaskCompletionSource<bool>();
            _loginCompletion = completion;
            _loginCustomId = customId;

            try
            {
                var result = await LoginWithCustomIdAsync(new LoginWithCustomIDRequest
                {
                    CustomId = customId,
                    CreateAccount = true,
                    TitleId = PlayFabSettings.TitleId
                });

                _loggedInCustomId = customId;
                _loggedInEntityId = result.EntityToken?.Entity?.Id ?? PlayFabSettings.staticPlayer.EntityId;

                if (string.IsNullOrEmpty(_loggedInEntityId))
                    throw new InvalidOperationException("PlayFab login did not return a title player entity id.");

                completion.TrySetResult(true);
                return true;
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
                throw;
            }
            finally
            {
                if (ReferenceEquals(_loginCompletion, completion))
                {
                    _loginCompletion = null;
                    _loginCustomId = null;
                }
            }
        }

        private static bool IsLoggedInAs(string customId)
        {
            return PlayFabClientAPI.IsClientLoggedIn()
                   && PlayFabProgressionAPI.IsEntityLoggedIn()
                   && string.Equals(_loggedInCustomId, customId, StringComparison.Ordinal)
                   && !string.IsNullOrEmpty(_loggedInEntityId);
        }

        private static PlayerData ResolveProfile(PlayerData profile)
        {
            if (profile != null)
                return profile;

            var service = ActiveProfileService.Instance;
            return service?.CurrentProfile ?? service?.LoadCurrent();
        }

        private static string ResolveCustomId(PlayerData profile)
        {
            if (!string.IsNullOrWhiteSpace(profile?.ProfileId))
                return $"profile_{profile.ProfileId}";

            var guestId = PlayerPrefs.GetString(GuestIdPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(guestId))
            {
                guestId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(GuestIdPrefsKey, guestId);
                PlayerPrefs.Save();
            }

            return $"guest_{guestId}";
        }

        private static void EnsureTitleId()
        {
            if (string.IsNullOrWhiteSpace(PlayFabSettings.TitleId))
                PlayFabSettings.TitleId = FallbackTitleId;
        }

        private static string BuildMetadata(string displayName)
        {
            return JsonUtility.ToJson(new EntryMetadata
            {
                displayName = NormalizeDisplayName(displayName)
            });
        }

        private static string ResolveEntryDisplayName(EntityLeaderboardEntry entry)
        {
            var metadataName = ParseMetadataDisplayName(entry.Metadata);
            if (!string.IsNullOrWhiteSpace(metadataName))
                return metadataName;

            if (!string.IsNullOrWhiteSpace(entry.DisplayName))
                return entry.DisplayName;

            if (!string.IsNullOrWhiteSpace(entry.Entity?.Id))
                return entry.Entity.Id.Length > 8 ? entry.Entity.Id.Substring(0, 8) : entry.Entity.Id;

            return GuestDisplayName;
        }

        private static string ParseMetadataDisplayName(string metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
                return null;

            try
            {
                return NormalizeDisplayName(JsonUtility.FromJson<EntryMetadata>(metadata)?.displayName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string NormalizeDisplayName(string displayName)
        {
            var normalized = string.IsNullOrWhiteSpace(displayName)
                ? GuestDisplayName
                : displayName.Trim();

            return normalized.Length > 24 ? normalized.Substring(0, 24) : normalized;
        }

        private static int ParseScore(List<string> scores)
        {
            if (scores == null || scores.Count == 0)
                return 0;

            return int.TryParse(scores[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var score)
                ? score
                : 0;
        }

        private static UniTask<LoginResult> LoginWithCustomIdAsync(LoginWithCustomIDRequest request)
        {
            var completion = new UniTaskCompletionSource<LoginResult>();
            try
            {
                PlayFabClientAPI.LoginWithCustomID(
                    request,
                    result => completion.TrySetResult(result),
                    error => completion.TrySetException(ToException(error)));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }

            return completion.Task;
        }

        private static UniTask<ProgressionEmptyResponse> UpdateLeaderboardEntriesAsync(UpdateLeaderboardEntriesRequest request)
        {
            var completion = new UniTaskCompletionSource<ProgressionEmptyResponse>();
            try
            {
                PlayFabProgressionAPI.UpdateLeaderboardEntries(
                    request,
                    result => completion.TrySetResult(result),
                    error => completion.TrySetException(ToException(error)));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }

            return completion.Task;
        }

        private static UniTask<GetEntityLeaderboardResponse> GetLeaderboardAsync(GetEntityLeaderboardRequest request)
        {
            var completion = new UniTaskCompletionSource<GetEntityLeaderboardResponse>();
            try
            {
                PlayFabProgressionAPI.GetLeaderboard(
                    request,
                    result => completion.TrySetResult(result),
                    error => completion.TrySetException(ToException(error)));
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }

            return completion.Task;
        }

        private static Exception ToException(PlayFabError error)
        {
            return new Exception(error != null ? error.GenerateErrorReport() : "Unknown PlayFab error");
        }
    }
}
