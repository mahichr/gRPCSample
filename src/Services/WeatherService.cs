﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using GrpcService.Server.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GrpcService.Server.Services
{
    public class WeatherService : Server.WeatherService.WeatherServiceBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WeatherService> _logger;
        private readonly string _openWeatherMapApiKey;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WeatherService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _openWeatherMapApiKey = configuration["OpenWeatherMapApiKey"];
        }

        public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherForCityRequest request, ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var temperatures = await GetTemperaturesAsync(request, httpClient);
            
            return new WeatherResponse
            {
                Temperature = temperatures.Main.Temp,
                FeelsLike = temperatures.Main.FeelsLike,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }

        public override async Task GetCurrentWeatherStream(GetCurrentWeatherForCityRequest request, IServerStreamWriter<WeatherResponse> responseStream, ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            for (int i = 0; i < 10; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Request was cancelled");
                    break;
                }
                var temperatures = await GetTemperaturesAsync(request, httpClient);
                await responseStream.WriteAsync(new WeatherResponse
                {
                    Temperature = temperatures.Main.Temp,
                    FeelsLike = temperatures.Main.FeelsLike,
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                });
                await Task.Delay(1000);
            }
        }

        private async Task<Temperatures> GetTemperaturesAsync(GetCurrentWeatherForCityRequest request, HttpClient httpClient)
        {
            var responseText = await httpClient.GetStringAsync(
                    $"https://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={_openWeatherMapApiKey}&units={request.Units}");

            var temperatures = JsonSerializer.Deserialize<Temperatures>(responseText);
            return temperatures;

        }
    }
}
