using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.IO;
using System.Data;

namespace PyroNexusTradingAlertBot.API
{
    public class CoinTrackingOptions
    {
        public string Key { get; set; }
        public string Secret { get; set; }
        public HttpClient Client { get; set; }
        public List<CoinTrackingUpdateJob> UpdateJobs { get; set; }
        public int UpdateJobsRefreshInterval { get; set; }
    }

    public abstract class CoinTrackingService : ICoinTrackingService
    {
        public class RemoteUpdate : CoinTrackingService, ICoinTrackingService.RemoteUpdate
        {
            public RemoteUpdate(IOptions<CoinTrackingOptions> options, ILogger<RemoteUpdate> logger)
                : base(options, logger)
            {
                _coinTrackingUpdateJobs = options.Value.UpdateJobs;
                updateJobsRefreshInterval = options.Value.UpdateJobsRefreshInterval;
            }

            private readonly List<CoinTrackingUpdateJob> _coinTrackingUpdateJobs;
            private readonly int updateJobsRefreshInterval;

            private async Task RunUpdateJobs(List<CoinTrackingUpdateJob> jobs)
            {
                int delay = 15;
                foreach (CoinTrackingUpdateJob job in jobs)
                {
                    _logger.LogDebug("Running update jobs for {0}", job.Name);
                    
                    await RunUpdateJob(job);
                    if (jobs.Last() == job)
                    {
                        _logger.LogDebug("Update finished for {0}. No more jobs to run.", job.Name);
                    }
                    else
                    {
                        _logger.LogDebug("Update finished for {0}. Delaying {1} seconds before the next job run...", job.Name, delay);
                        await Task.Delay(TimeSpan.FromSeconds(delay));
                    }
                }
            }

            private async Task RunUpdateJob(CoinTrackingUpdateJob job)
            {
                var delay = 10;
                foreach (int jobId in job.JobIds)
                {
                    _logger.LogDebug("Running update job for {0} with id {1}", job.Name, jobId);
                    var jobUpdatePath = string.Format("{0}/check.php?j={1}&check=check", job.Path, jobId);

                    var request = new HttpRequestMessage
                    {
                        RequestUri = new Uri(updateJobUrl, jobUpdatePath),
                        Method = HttpMethod.Get
                    };

                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.152 Safari/537.36");

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        if (job.JobIds.Last() == jobId)
                        {
                            _logger.LogDebug("Update finished for {0}.", job.Name);
                        }
                        else
                        {
                            _logger.LogDebug("Update finished for {0}. Delaying {1} seconds before the next job id run...", jobId, delay);
                            await Task.Delay(TimeSpan.FromSeconds(delay));
                        }
                    }
                    else
                    {
                        _logger.LogError("Update failed running job for {0} with id {1}. Http status code: {2} {3}", job.Name, jobId, response.StatusCode, response.ReasonPhrase);
                    }

                }
            }

            public async Task UpdateTrades()
            {
                while (true)
                {
                    _logger.LogInformation("Running...");
                    await Task.Run(() => RunUpdateJobs(_coinTrackingUpdateJobs));

                    _logger.LogInformation("Complete. Waiting {0} hours for next run.", updateJobsRefreshInterval);
                    await Task.Delay(new TimeSpan(updateJobsRefreshInterval, 0, 0));
                }
            }
        }

        public class LocalImport : CoinTrackingService, ICoinTrackingService.LocalImport
        {
            public LocalImport(IOptions<CoinTrackingOptions> options, ILogger<LocalImport> logger)
                : base(options, logger)
            { }

            private class RequestType
            {
                public const string Balance = "getBalance";
                public const string Trades = "getTrades";
                public const string HistoricalSummary = "getHistoricalSummary";
                public const string HistoricalCurrency = "getHistoricalCurrency";
                public const string GroupedBalance = "getGroupedBalance";
                public const string Gains = "getGains";
            }

            private FormUrlEncodedContent PrepareRequestData(string method, IEnumerable<KeyValuePair<string, string>> data)
            {
                return new FormUrlEncodedContent(
                    Enumerable.Concat(
                        data,
                        new[] {
                        new KeyValuePair<string, string>("method", method),
                        new KeyValuePair<string, string>("nonce", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString())
                        }
                        )
                    );
            }

            private async Task SignMessage(FormUrlEncodedContent formData)
            {
                HMACSHA512 hmac = new HMACSHA512(Encoding.ASCII.GetBytes(apiSecret));
                byte[] sign = hmac.ComputeHash(await formData.ReadAsByteArrayAsync());

                formData.Headers.Add("Key", apiKey);
                formData.Headers.Add("Sign", BitConverter.ToString(sign).Replace("-", string.Empty).ToLower());
            }

            private async Task<bool> CheckResult(Stream stream)
            {
                var json = await JsonSerializer.DeserializeAsync<Result>(stream);

                if (json.success == 0)
                {
                    if (string.IsNullOrWhiteSpace(json.error))
                    {
                        json.error = "UNKNOWN_ERR";
                    }
                    if (string.IsNullOrWhiteSpace(json.error_msg))
                    {
                        json.error_msg = "Unknown error: No error message was provided by the server.";
                    }
                    _logger.LogError(string.Format("{0}: {1}", json.error, json.error_msg));
                    return false;
                }
                return true;
            }

            private async Task ProcessStream<T>(Stream stream, Dictionary<string, T> result) where T : class
            {
                var json = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(stream);

                var props = typeof(Result).GetProperties();
                List<string> ignoredProps = new List<string>();
                for (int i = 0; i < props.Length; i++)
                {
                    ignoredProps.Add(props[i].Name.ToString());
                }

                foreach (var item in json)
                {
                    if (ignoredProps.Contains(item.Key))
                    {
                        continue;
                    }
                    result.Add(item.Key, JsonSerializer.Deserialize<T>(item.Value.ToString()));
                }
            }

            private async Task<Dictionary<string, T>> DoPost<T>(string request, List<KeyValuePair<string, string>> requestParams) where T : class
            {
                FormUrlEncodedContent formData = PrepareRequestData(request, requestParams);
                await SignMessage(formData);

                HttpResponseMessage response = await client.PostAsync(apiUrl, formData);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var result = new Dictionary<string, T>();

                var ok = await CheckResult(stream);
                if (ok)
                {
                    stream.Position = 0;
                    await ProcessStream(stream, result);
                }
                return result;
            }

            private async Task<Dictionary<string, T>> DoPost<T>(string request) where T : class
            {
                return await DoPost<T>(request, new List<KeyValuePair<string, string>>());
            }

            public async Task<Dictionary<string, Trade>> GetTrades(int limit = 0, string order = "ASC", int start = 0, int end = 0, bool tradePrices = false)
            {
                _logger.LogInformation("Requesting trades from CoinTracking...");
                List<KeyValuePair<string, string>> optionalParams = new List<KeyValuePair<string, string>>();

                if (limit > 0)
                {
                    optionalParams.Add(new KeyValuePair<string, string>("limit", limit.ToString()));
                }

                optionalParams.Add(new KeyValuePair<string, string>("order", order));

                if (start > 0)
                {
                    optionalParams.Add(new KeyValuePair<string, string>("start", start.ToString()));
                }

                if (end > 0)
                {
                    optionalParams.Add(new KeyValuePair<string, string>("end", end.ToString()));
                }

                if (tradePrices)
                {
                    optionalParams.Add(new KeyValuePair<string, string>("trade_prices", "1"));
                }

                return await DoPost<Trade>(RequestType.Trades, optionalParams);
            }
        }



        private readonly ILogger _logger;
        private readonly HttpClient client;

        public readonly string apiUrl = "https://cointracking.info/api/v1/";
        public readonly Uri updateJobUrl = new Uri("https://cointracking.info/import/");
        private readonly string apiKey;
        private readonly string apiSecret;

        private CoinTrackingService(IOptions<CoinTrackingOptions> options, ILogger logger)
        {
            client = options.Value.Client;
            apiKey = options.Value.Key;
            apiSecret = options.Value.Secret;
            _logger = logger;
        }

        public static HttpClient GetClient(string cookie1, string cookie2)
        {
            var cookies = new CookieContainer();
            cookies.Add(new CookieCollection()
            {
                new Cookie("cointracking_cookie", cookie1, "/import", ".cointracking.info"),
                new Cookie("cointracking_cookie2", cookie2, "/import", ".cointracking.info")
            });
            var handler = new HttpClientHandler() { CookieContainer = cookies };
            return new HttpClient(handler); 
        }


        //public async Task<Dictionary<string, Balance>> GetBalance()
        //{
        //    return await doPost<Balance>(RequestType.Balance);
        //}

        //public async Task<string> getHistoricalSummary(bool btc = false, int start = 0, int end = 0)
        //{
        //    List<KeyValuePair<string, string>> optionalParams = new List<KeyValuePair<string, string>>();

        //    if (btc)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("btc", "1"));
        //    }

        //    if (start > 0)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("start", start.ToString()));
        //    }

        //    if (end > 0)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("end", end.ToString()));
        //    }

        //    return await doPost<HistoricalSummary>(RequestType.HistoricalSummary);
        //}

        //public async Task<string> getHistoricalCurrency(string currency = null, int start = 0, int end = 0)
        //{
        //    List<KeyValuePair<string, string>> optionalParams = new List<KeyValuePair<string, string>>();

        //    if (!string.IsNullOrEmpty(currency))
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("currency", currency));
        //    }

        //    if (start > 0)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("start", start.ToString()));
        //    }

        //    if (end > 0)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("end", end.ToString()));
        //    }

        //    return await doPost(RequestType.HistoricalCurrency, optionalParams);
        //}

        //public async Task<string> getGroupedBalance(string group = "exchange", bool excludeDepWith = false, string type = null)
        //{
        //    List<KeyValuePair<string, string>> optionalParams = new List<KeyValuePair<string, string>>();

        //    if (!string.IsNullOrEmpty(group))
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("group", group));
        //    }

        //    if (!string.IsNullOrEmpty(type))
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("type", type));
        //    }

        //    if (excludeDepWith)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("exclude_dep_with", "1"));
        //    }

        //    return await doPost(RequestType.GroupedBalance, optionalParams);
        //}

        //public async Task<string> getGains(string price = null, bool btc = false)
        //{
        //    List<KeyValuePair<string, string>> optionalParams = new List<KeyValuePair<string, string>>();

        //    if (!string.IsNullOrEmpty(price))
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("price", price));
        //    }

        //    if (btc)
        //    {
        //        optionalParams.Add(new KeyValuePair<string, string>("btc", "1"));
        //    }

        //    return await doPost(RequestType.Gains, optionalParams);
        //}
    }
}