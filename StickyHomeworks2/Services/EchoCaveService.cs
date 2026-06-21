using System.Net.Http;
using Microsoft.Extensions.Logging;
using StickyHomeworks.Models;
using StickyHomeworks2.Helpers;

namespace StickyHomeworks.Services;

public class EchoCaveService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EchoCaveService> _logger;
    private const string ApiUrl = "https://api.classisband.xyz/api/echoes";
    private readonly List<EchoItem> _echoQueue = new();
    private readonly Random _random = new();

    private EchoItem? _currentEcho;

    public EchoItem? CurrentEcho
    {
        get => _currentEcho;
        set => _currentEcho = value;
    }

    public EchoCaveService(HttpClient httpClient, ILogger<EchoCaveService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<EchoItem>> FetchEchoesAsync()
    {
        try
        {
            var echoes = await WebRequestHelper.GetJsonAsync<List<EchoItem>>(_httpClient, ApiUrl);
            echoes ??= new List<EchoItem>();
            _logger.LogInformation("成功获取 {Count} 条回声", echoes.Count);
            return echoes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取回声数据失败");
            return new List<EchoItem>();
        }
    }

    public async Task GetNextEchoAsync()
    {
        if (_echoQueue.Count <= 0)
        {
            var echoes = await FetchEchoesAsync();
            var list = echoes.ToList();
            var count = list.Count;
            for (var i = count - 1; i > 0; i--)
            {
                var j = _random.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            _echoQueue.AddRange(list);
        }

        if (_echoQueue.Count > 0)
        {
            CurrentEcho = _echoQueue[0];
            _echoQueue.RemoveAt(0);
        }
    }
}
