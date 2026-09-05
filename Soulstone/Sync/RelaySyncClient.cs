using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Soulstone.Sync
{
    public sealed class RelaySyncClient : IAsyncDisposable
    {
        private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
        private readonly SemaphoreSlim connectionLock = new(1, 1);
        private readonly SemaphoreSlim sendLock = new(1, 1);
        private ClientWebSocket? socket;
        private CancellationTokenSource? connectionCancellation;
        private Task? receiveTask;

        public bool IsConnected => socket?.State == WebSocketState.Open;
        public string Status { get; private set; } = "Disconnected";

        public event Action<string>? MessageReceived;
        public event Action<string>? StatusChanged;

        public async Task<RelaySessionResponse> CreateSessionAsync(string serverUrl, CancellationToken cancellationToken = default)
        {
            Uri baseUri = ValidateServerUrl(serverUrl);
            using var response = await HttpClient.PostAsync(new Uri(baseUri, "/api/sessions"), null, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var session = await response.Content.ReadFromJsonAsync<RelaySessionResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return session ?? throw new InvalidDataException("The relay returned an empty session response.");
        }

        public async Task RegisterInviteAsync(
            string serverUrl,
            string sessionId,
            string hostToken,
            string inviteId,
            string payload,
            CancellationToken cancellationToken = default)
        {
            Uri baseUri = ValidateServerUrl(serverUrl);
            using var request = new HttpRequestMessage(HttpMethod.Put,
                new Uri(baseUri, $"/api/sessions/{Uri.EscapeDataString(sessionId)}/invite"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hostToken);
            request.Content = JsonContent.Create(new RelayInviteRegistration { InviteId = inviteId, Payload = payload });
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> ResolveInviteAsync(
            string serverUrl,
            string inviteId,
            CancellationToken cancellationToken = default)
        {
            Uri baseUri = ValidateServerUrl(serverUrl);
            using var response = await HttpClient.GetAsync(
                new Uri(baseUri, $"/api/invites/{Uri.EscapeDataString(inviteId)}"), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var resolution = await response.Content.ReadFromJsonAsync<RelayInviteResolution>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return resolution?.Payload ?? throw new InvalidDataException("The relay returned an empty invite response.");
        }

        public async Task ConnectAsync(string serverUrl, string sessionId, string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("A relay session and access token are required.");

            await connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await DisconnectCoreAsync().ConfigureAwait(false);
                Uri baseUri = ValidateServerUrl(serverUrl);
                var builder = new UriBuilder(baseUri)
                {
                    Scheme = baseUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
                    Path = $"/api/sessions/{Uri.EscapeDataString(sessionId)}/connect",
                    Query = string.Empty
                };

                SetStatus("Connecting");
                socket = new ClientWebSocket();
                socket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
                socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
                connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                await socket.ConnectAsync(builder.Uri, connectionCancellation.Token).ConfigureAwait(false);
                SetStatus("Connected");
                receiveTask = ReceiveLoopAsync(socket, connectionCancellation.Token);
            }
            catch
            {
                SetStatus("Connection failed");
                await DisconnectCoreAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                connectionLock.Release();
            }
        }

        public async Task SendAsync(RelayEnvelope envelope, CancellationToken cancellationToken = default)
        {
            ClientWebSocket activeSocket = socket ?? throw new InvalidOperationException("Soulstone is not connected to a relay.");
            if (activeSocket.State != WebSocketState.Open) throw new InvalidOperationException("Soulstone is not connected to a relay.");

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(envelope);
            if (data.Length > 64 * 1024) throw new InvalidDataException("The synchronization message exceeds the relay limit.");

            await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await activeSocket.SendAsync(data, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                sendLock.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await connectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisconnectCoreAsync().ConfigureAwait(false);
            }
            finally
            {
                connectionLock.Release();
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket activeSocket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[8192];
            try
            {
                while (!cancellationToken.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
                {
                    using var message = new MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await activeSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close) return;
                        message.Write(buffer, 0, result.Count);
                        if (message.Length > 64 * 1024) throw new InvalidDataException("The relay sent an oversized message.");
                    }
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        MessageReceived?.Invoke(Encoding.UTF8.GetString(message.ToArray()));
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Plugin.Log?.Warning(ex, "Soulstone relay receive loop stopped");
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested) SetStatus("Disconnected");
            }
        }

        private async Task DisconnectCoreAsync()
        {
            connectionCancellation?.Cancel();
            if (socket is { State: WebSocketState.Open or WebSocketState.CloseReceived })
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None).ConfigureAwait(false);
                }
                catch { }
            }
            socket?.Dispose();
            socket = null;
            connectionCancellation?.Dispose();
            connectionCancellation = null;
            receiveTask = null;
            SetStatus("Disconnected");
        }

        private static Uri ValidateServerUrl(string serverUrl)
        {
            if (!Uri.TryCreate(serverUrl?.TrimEnd('/') + "/", UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException("Enter a valid HTTP(S) Soulstone relay URL.");

            bool local = uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase);
            if (!local && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Remote Soulstone relays must use HTTPS/WSS.");
            return uri;
        }

        private void SetStatus(string status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync().ConfigureAwait(false);
            connectionLock.Dispose();
            sendLock.Dispose();
        }
    }
}