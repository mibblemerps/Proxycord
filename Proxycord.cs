using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Proxycord
{
    public class Proxycord
    {
        public IPEndPoint ListenIP = new IPEndPoint(IPAddress.Any, 25565);

        public ProxyConfig Config = new ProxyConfig();

        protected TcpListener TcpListener;

        public Proxycord()
        {
            Log.Info("Loading config...");
            Config.Load();
            Log.Info($"{Config.Rules.Count} proxy rules loaded.");
        }

        public async Task Listen()
        {
            TcpListener = new TcpListener(ListenIP);
            TcpListener.Start();

            while (true)
            {
                TcpClient client = null;
                try
                {
                    // Received connection into proxy
                    client = await TcpListener.AcceptTcpClientAsync();
                    Log.Info($"Client connection from {client.Client.RemoteEndPoint}");
                }
                catch (Exception e)
                {
                    Log.Error($"Exception accepting client connection from {client?.Client.RemoteEndPoint}: {e.Message}");
                }

                if (client == null)
                    continue;

                string clientName = client.Client.RemoteEndPoint.ToString();

                _ = Task.Run(async () =>
                {
                    var session = new Session();
                    session.EndpointFailed += (sender, endpoint) =>
                    {
                        Log.Warn($"Couldn't connect client {clientName} to server {endpoint}!");
                    };
                    session.Connected += (sender, endpoint) =>
                    {
                        Log.Info($"Connected client {clientName} to server {endpoint}.");
                    };
                    session.HandshakeReceived += (sender, handshake) =>
                    {
                        Log.Info($"Received valid Minecraft handshake from {clientName}. Given IP = {handshake.ServerAddressWithoutMagicString}:{handshake.Port} (protocol version = {handshake.ProtocolVersion}).");
                    };

                    try
                    {
                        await session.Listen(client.GetStream(), ResolveEndpoint);
                    }
                    catch (Session.NoServersMatchException noServersException)
                    {
                        Log.Warn($"{clientName} tried to connect to {noServersException.Handshake.ServerAddressWithoutMagicString} (protocol version = {noServersException.Handshake.ProtocolVersion}), but no suitable servers matched.");
                    }
                    catch (Exception)
                    {
                        // Exceptions here are unimportant, just connection closes, drops, etc..
                    }

                    // Try and disconnect
                    try
                    {
                        client.Client?.Disconnect(false);
                    } catch {}

                    Log.Info($"{clientName} disconnected.");
                });
            }
        }

        private ProxyRule.ProxyEndpoint[] ResolveEndpoint(MinecraftHandshake handshake)
        {
            var endpoints = new List<ProxyRule.ProxyEndpoint>();

            foreach (ProxyRule rule in Config.Rules)
            {
                if (rule.DoesMatch(handshake.ServerAddressWithoutMagicString, handshake.ProtocolVersion))
                    endpoints.AddRange(rule.Endpoints);
            }

            return endpoints.ToArray();
        }
    }
}
