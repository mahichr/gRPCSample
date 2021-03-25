using Grpc.Core;
using GrpcService.Server.Contracts;
using Microsoft.Extensions.Configuration;
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
        private readonly string _openWeatherMapApiKey;

        public WeatherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _openWeatherMapApiKey = configuration["OpenWeatherMapApiKey"];
        }

        public override async Task<WeatherResponse> GetCurrentWeather(GetCurrentWeatherForCityRequest request, ServerCallContext context)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var responseText = await httpClient.GetStringAsync(
                $"https://api.openweathermap.org/data/2.5/weather?q={request.City}&appid={_openWeatherMapApiKey}&units={request.Units}");

            var temperatures = JsonSerializer.Deserialize<Temperatures>(responseText); 

            return new WeatherResponse
            { 
                Temperature = temperatures.Main.Temp,
                FeelsLike = temperatures.Main.FeelsLike
            };
        }
    }
}
