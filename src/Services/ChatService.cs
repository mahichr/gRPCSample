using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcService.Server.Services
{
    public class ChatService : Server.Chat.ChatBase
    {
        private readonly ILogger<ChatService> _logger;

        public ChatService(ILogger<ChatService> logger )
        {
            _logger = logger;
        }

        public override async Task SendMessage(IAsyncStreamReader<ClientToServerMessage> requestStream, IServerStreamWriter<ServerToClientMessage> responseStream, ServerCallContext context)
        {
            var clientToServerTask = ClientToServerPingHandlingAsync(requestStream, context);
            var serverToClientTask = ServerToClientPingHandlingAsync(responseStream, context);
            await Task.WhenAll(clientToServerTask, serverToClientTask);
        }

        private static async Task ServerToClientPingHandlingAsync(IServerStreamWriter<ServerToClientMessage> responseStream, ServerCallContext context)
        {
            var pingCount = 0;
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await responseStream.WriteAsync(new ServerToClientMessage
                {
                    Text = $"Server said hi {++pingCount} times.",
                    Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
                });
                await Task.Delay(1000);
            }
        }

        private async Task ClientToServerPingHandlingAsync(IAsyncStreamReader<ClientToServerMessage> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested)
            {
                var message = requestStream.Current;
                _logger.LogInformation("The client said: {Message}", message.Text);
            }
        }
    }
}
