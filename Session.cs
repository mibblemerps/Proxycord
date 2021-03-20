using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Proxycord
{
    public class Session
    {
        /// <summary>
        /// Handles resolving an incoming Minecraft handshake to a server (or list of server's) it should connect to.
        /// </summary>
        /// <param name="handshake"></param>
        /// <returns></returns>
        public delegate ProxyRule.ProxyEndpoint[] ResolveToServerEndpoints(MinecraftHandshake handshake);

        public event EventHandler<ProxyRule.ProxyEndpoint> EndpointFailed;
        public event EventHandler<ProxyRule.ProxyEndpoint> Connected;
        public event EventHandler<MinecraftHandshake> HandshakeReceived;

        public async Task Listen(Stream client, ResolveToServerEndpoints resolveDelegate)
        {
            // Try and read Minecraft packet structure
            int packetLength = await client.ReadVarInt();

            // Read packet body
            byte[] buffer = new byte[packetLength];
            await client.ReadAsync(buffer, 0, packetLength);
            var packetStream = new MemoryStream(buffer);

            int packetId = await packetStream.ReadVarInt();
            if (packetId != 0)
            {
                // Not handshake packet
                throw new ProtocolException("Illegal packet ID for handshake");
            }

            // Read handshake packet
            var handshake = await MinecraftHandshake.Read(packetStream);

            HandshakeReceived?.Invoke(this, handshake);

            // Resolve to a server endpoint based on handshake
            ProxyRule.ProxyEndpoint[] endpoints = resolveDelegate(handshake);
            if (endpoints == null || endpoints.Length == 0)
            {
                await WriteDisconnectPacket(client, $"Proxy server has no server for this hostname.", "red");
                throw new NoServersMatchException(handshake);
            }

            // Try to connect to an endpoint
            NetworkStream tunnel = null;
            ProxyRule.ProxyEndpoint connectedEndpoint = null;
            foreach (var endpointObj in endpoints)
            {
                try
                {
                    // This will just grab the endpoint, or do a DNS lookup if needed
                    IPEndPoint endpoint = await endpointObj.GetEndpoint();

                    var tunnelClient = new TcpClient();
                    await tunnelClient.ConnectAsync(endpoint.Address, endpoint.Port);
                    tunnel = tunnelClient.GetStream();

                    connectedEndpoint = endpointObj;
                    Connected?.Invoke(this, endpointObj);
                    break;
                }
                catch (Exception e)
                {
                    EndpointFailed?.Invoke(this, endpointObj);
                }
            }

            if (tunnel == null || connectedEndpoint == null)
            {
                await WriteDisconnectPacket(client, $"Proxy server cannot connect to origin server.", "red");
                throw new Exception("No server endpoints are accessible.");
            }

            // Modify handshake before we resend it to appear as if we connected directly to the target server
            bool fmlMagicStringPresent = handshake.ServerAddress.EndsWith("\0FML\0");
            if (connectedEndpoint.DnsHostname == null)
                handshake.ServerAddress = connectedEndpoint.Endpoint.Address.ToString();
            else
                handshake.ServerAddress = connectedEndpoint.DnsHostname;

            // Reappend FML magic string
            if (fmlMagicStringPresent)
                handshake.ServerAddress += "\0FML\0";

            handshake.Port = (ushort) connectedEndpoint.Port;

            try
            {
                // Resend handshake packet
                var resendHandshakeStream = new MemoryStream();
                await resendHandshakeStream.WriteVarInt(0); // Packet ID 0 (handshake)
                await handshake.Write(resendHandshakeStream);
                byte[] resendHandshakeBytes = resendHandshakeStream.ToArray();

                await tunnel.WriteVarInt(resendHandshakeBytes.Length); // Packet length
                await tunnel.WriteAsync(resendHandshakeBytes, 0, resendHandshakeBytes.Length); // Packet body

                // Proxy traffic backwards and forwards
                Task.WaitAny(client.CopyToAsync(tunnel), tunnel.CopyToAsync(client));
            }
            catch {}
        }

        private async Task WriteDisconnectPacket(Stream stream, string message, string color = "white")
        {
            message = message.Replace("\\", "\\\\");
            message = message.Replace("\"", "\\\"");

            var memory = new MemoryStream();
            await memory.WriteVarInt(0); // Packet ID 0
            await memory.WriteVarString("{\"text\":\"" + message + "\",\"color\":\"" + color + "\"}");
            byte[] bytes = memory.ToArray();

            await stream.WriteVarInt(bytes.Length); // Packet length
            await stream.WriteAsync(bytes, 0, bytes.Length); // Packet body
        }

        public class NoServersMatchException : Exception
        {
            public readonly MinecraftHandshake Handshake;

            public NoServersMatchException(MinecraftHandshake handshake) : base($"No servers for {handshake.ServerAddress}.")
            {
                Handshake = handshake;
            }
        }
    }
}
