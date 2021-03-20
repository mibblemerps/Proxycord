using System.IO;
using System.Threading.Tasks;

namespace Proxycord
{
    public class MinecraftHandshake
    {
        public int ProtocolVersion;
        public string ServerAddress;
        public ushort Port;
        public State NextState;

        /// <summary>
        /// When connecting to Forge servers, Forge appends "\0FML\0" to the end of the server address.
        /// This breaks our server IP rules system. This returns a version of the server address without that magic string.
        /// </summary>
        public string ServerAddressWithoutMagicString => ServerAddress.Contains('\0') ? ServerAddress.Split('\0')[0] : ServerAddress;

        public string DebugString()
        {
            return $"ProtocolVersion = {ProtocolVersion}\nServerAddress = {ServerAddress} (Port = {Port})\nNext State = {NextState}";
        }

        public async Task Write(Stream stream)
        {
            await stream.WriteVarInt(ProtocolVersion);
            await stream.WriteVarString(ServerAddress);
            await stream.WriteUShort(Port);
            await stream.WriteVarInt((int) NextState);
        }

        public static async Task<MinecraftHandshake> Read(Stream stream)
        {
            var handshake = new MinecraftHandshake();
            handshake.ProtocolVersion = await stream.ReadVarInt();
            handshake.ServerAddress = await stream.ReadVarString();
            handshake.Port = await stream.ReadUShort();
            handshake.NextState = (State) await stream.ReadVarInt();
            return handshake;
        }

        public enum State
        {
            Handshake,
            Status,
            Login,
            Play,
        }
    }
}
