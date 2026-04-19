using CrosshairZ.Interop;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace CrosshairZ.Services
{
    public sealed class ProfileStore
    {
        private const string FileName = "crosshair_profiles.json";

        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource _saveCts;

        public async Task<ProfilesFile> LoadAsync()
        {
            try
            {
                var item = await ApplicationData.Current.RoamingFolder.TryGetItemAsync(FileName);
                if (item is not StorageFile file)
                {
                    var profiles = CreateDefault();
                    await SaveImmediateAsync(profiles);
                    return profiles;
                }

                var json = await FileIO.ReadTextAsync(file);
                var profiles2 = JsonConvert.DeserializeObject<ProfilesFile>(json);

                if (profiles2 == null || profiles2.Profiles == null || profiles2.Profiles.Count == 0)
                {
                    return CreateDefault();
                }

                if (string.IsNullOrWhiteSpace(profiles2.ActiveProfileId))
                {
                    profiles2.ActiveProfileId = profiles2.Profiles.First().Id;
                }

                return profiles2;
            }
            catch (Exception)
            {
                var profiles = CreateDefault();
                try { await SaveImmediateAsync(profiles); } catch { }
                return profiles;
            }
        }


        public async Task SaveAsync(ProfilesFile profiles)
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            try
            {
                await Task.Delay(350, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await SaveImmediateAsync(profiles);
        }


        public async Task SaveImmediateAsync(ProfilesFile profiles)
        {
            await _saveLock.WaitAsync();
            try
            {
                var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(
                    FileName,
                    CreationCollisionOption.ReplaceExisting);

                var json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
                await FileIO.WriteTextAsync(file, json);
            }
            finally
            {
                _saveLock.Release();
            }
        }

        public static ProfilesFile CreateDefault()
        {
            var profile = new CrosshairProfile
            {
                Id = "default",
                Name = "Default",
                Crosshair = new CrosshairData()
            };

            return new ProfilesFile
            {
                Version = 1,
                Profiles = new System.Collections.Generic.List<CrosshairProfile> { profile },
                ActiveProfileId = profile.Id
            };
        }
    }
}
