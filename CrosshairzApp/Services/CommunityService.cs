using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CrosshairZ.Services
{
    public class CommunityCrosshair
    {
        [JsonProperty("id")]      public long   Id        { get; set; }
        [JsonProperty("name")]    public string Name      { get; set; }
        [JsonProperty("author")]  public string Author    { get; set; }
        [JsonProperty("code")]    public string Code      { get; set; }
        [JsonProperty("tags")]    public List<string> Tags { get; set; } = new List<string>();
        [JsonProperty("likes")]   public long   Likes     { get; set; }
        [JsonProperty("created_at")] public string CreatedAt { get; set; }
    }

    public class CommunityListResponse
    {
        [JsonProperty("items")]    public List<CommunityCrosshair> Items   { get; set; } = new List<CommunityCrosshair>();
        [JsonProperty("total")]    public long Total    { get; set; }
        [JsonProperty("page")]     public long Page     { get; set; }
        [JsonProperty("per_page")] public long PerPage  { get; set; }
    }

    public class SubmitRequest
    {
        [JsonProperty("name")]   public string Name   { get; set; }
        [JsonProperty("author")] public string Author { get; set; }
        [JsonProperty("code")]   public string Code   { get; set; }
        [JsonProperty("tags")]   public List<string> Tags { get; set; } = new List<string>();
    }

    public class CommunityService
    {
        private const string BaseUrl = "http://localhost:7373";
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

        public static async Task<CommunityListResponse> GetCrosshairsAsync(
            int page = 1, int perPage = 30, string search = null)
        {
            try
            {
                string url = $"{BaseUrl}/crosshairs?page={page}&per_page={perPage}";
                if (!string.IsNullOrWhiteSpace(search))
                    url += $"&search={Uri.EscapeDataString(search)}";

                string json = await _http.GetStringAsync(url);
                return JsonConvert.DeserializeObject<CommunityListResponse>(json)
                       ?? new CommunityListResponse();
            }
            catch
            {
                return new CommunityListResponse();
            }
        }

        public static async Task<CommunityCrosshair> GetCrosshairAsync(long id)
        {
            try
            {
                string json = await _http.GetStringAsync($"{BaseUrl}/crosshairs/{id}");
                return JsonConvert.DeserializeObject<CommunityCrosshair>(json);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<long?> SubmitCrosshairAsync(
            string name, string author, string code, List<string> tags = null)
        {
            try
            {
                var body = new SubmitRequest
                {
                    Name   = name,
                    Author = string.IsNullOrWhiteSpace(author) ? "Anonymous" : author,
                    Code   = code,
                    Tags   = tags ?? new List<string>(),
                };
                string bodyJson = JsonConvert.SerializeObject(body);
                var content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync($"{BaseUrl}/crosshairs", content);
                if (!response.IsSuccessStatusCode) return null;

                string responseJson = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeAnonymousType(responseJson, new { id = 0L });
                return obj?.id;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> LikeCrosshairAsync(long id)
        {
            try
            {
                var response = await _http.PostAsync($"{BaseUrl}/crosshairs/{id}/like", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsServerReachableAsync()
        {
            try
            {
                string result = await _http.GetStringAsync($"{BaseUrl}/health");
                return result?.Trim() == "ok";
            }
            catch
            {
                return false;
            }
        }
    }
}
