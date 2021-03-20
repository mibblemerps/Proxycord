using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proxycord
{
    public static class DataTypes
    {
        public static async Task<ReadIntResult> ReadVarIntAdvanced(this Stream stream)
        {
            int bytesRead = 0;
            int result = 0;
            byte b;
            do
            {
                byte[] buffer = new byte[1];
                if (await stream.ReadAsync(buffer, 0, 1) == 0)
                    throw new Exception("Unexpected end of stream while reading VarInt");

                b = buffer[0];
                int value = b & 0b01111111;
                result |= value << (7 * bytesRead);

                bytesRead++;
                if (bytesRead > 5)
                    throw new Exception("VarInt is too big");
            } while ((b & 0b10000000) != 0);

            return new ReadIntResult {Value = result, Length = bytesRead};
        }

        public static async Task<int> ReadVarInt(this Stream stream)
        {
            int bytesRead = 0;
            int result = 0;
            byte b;
            do
            {
                byte[] buffer = new byte[1];
                if (await stream.ReadAsync(buffer, 0, 1) == 0)
                    throw new Exception("Unexpected end of stream while reading VarInt");

                b = buffer[0];
                int value = b & 0b01111111;
                result |= value << (7 * bytesRead);

                bytesRead++;
                if (bytesRead > 5)
                    throw new Exception("VarInt is too big");
            } while ((b & 0b10000000) != 0);

            return result;
        }

        public static async Task<ReadLongResult> ReadVarLong(this Stream stream)
        {
            int bytesRead = 0;
            long result = 0;
            byte b;
            do
            {
                byte[] buffer = new byte[1];
                if (await stream.ReadAsync(buffer, 0, 1) == 0)
                    throw new Exception("Unexpected end of stream while reading VarInt");

                b = buffer[0];
                int value = b & 0b01111111;
                result |= value << (7 * bytesRead);

                bytesRead++;
                if (bytesRead > 10)
                    throw new Exception("VarInt is too big");
            } while ((b & 0b10000000) != 0);

            return new ReadLongResult { Value = result, Length = bytesRead };
        }

        public static async Task WriteVarInt(this Stream stream, int value)
        {
            do
            {
                byte temp = (byte)(value & 0b01111111);
                // Note: >>> means that the sign bit is shifted with the rest of the number rather than being left alone
                //value >>>= 7;
                value = (int) ((uint) value >> 7);
                if (value != 0)
                {
                    temp |= 0b10000000;
                }

                byte[] buffer = {temp};
                await stream.WriteAsync(buffer, 0, 1);
            } while (value != 0);
        }

        public static async Task<string> ReadVarString(this Stream stream)
        {
            int length = (await stream.ReadVarIntAdvanced()).Value;
            byte[] buffer = new byte[length];
            await stream.ReadAsync(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        public static async Task WriteVarString(this Stream stream, string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            await WriteVarInt(stream, bytes.Length);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public static async Task<ushort> ReadUShort(this Stream stream)
        {
            byte[] buffer = new byte[2];
            await stream.ReadAsync(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer.Reverse().ToArray());
        }

        public static async Task WriteUShort(this Stream stream, ushort num)
        {
            byte[] buffer = BitConverter.GetBytes(num);
            await stream.WriteAsync(new[] {buffer[1], buffer[0]}, 0, 2);
        }

        public struct ReadIntResult
        {
            public int Value;
            public int Length;
        }

        public struct ReadLongResult
        {
            public long Value;
            public int Length;
        }
    }
}
