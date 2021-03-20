namespace Proxycord
{
    public class Packet
    {
        public int Length;
        public int PacketId;
        public byte[] Body;

        public Packet(int packetId, int length, byte[] body)
        {
            PacketId = packetId;
            Length = length;
            Body = body;
        }
    }
}
