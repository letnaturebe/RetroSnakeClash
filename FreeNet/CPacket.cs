using System.Text;

namespace FreeNet
{
    public class CPacket
    {
        public IPeer owner { get; private set; }
        public byte[] buffer { get; private set; }
        public int position { get; private set; }

        public Int16 protocol_id { get; private set; }

        public static CPacket create(Int16 protocol_id)
        {
            CPacket packet = CPacketBufferManager.pop();
            packet.set_protocol(protocol_id);
            return packet;
        }

        public CPacket(byte[] buffer, IPeer owner)
        {
            this.buffer = buffer;
            position = Defines.HEADER_SIZE;
            this.owner = owner;
        }

        public static void Destroy(CPacket packet)
        {
            CPacketBufferManager.push(packet);
        }

        public CPacket()
        {
            buffer = new byte[1024];
        }

        public Int16 pop_protocol_id()
        {
            return pop_int16();
        }

        public void copy_to(CPacket target)
        {
            target.set_protocol(this.protocol_id);
            target.Overwrite(this.buffer, this.position);
        }

        private void Overwrite(byte[] source, int position)
        {
            Array.Copy(source, this.buffer, source.Length);
            this.position = position;
        }

        public byte pop_byte()
        {
            byte data = (byte)BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(byte);
            return data;
        }

        public Int16 pop_int16()
        {
            Int16 data = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);
            return data;
        }

        public Int32 pop_int32()
        {
            Int32 data = BitConverter.ToInt32(this.buffer, this.position);
            this.position += sizeof(Int32);
            return data;
        }

        public float pop_float()
        {
            float data = BitConverter.ToSingle(this.buffer, this.position);
            this.position += sizeof(float);
            return data;
        }


        public string pop_string()
        {
            // 문자열 길이는 최대 2바이트 까지. 0 ~ 32767
            Int16 len = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);

            // 인코딩은 utf8로 통일한다.
            string data = System.Text.Encoding.UTF8.GetString(this.buffer, this.position, len);
            this.position += len;

            return data;
        }

        public void set_protocol(Int16 protocol_id)
        {
            this.protocol_id = protocol_id;
            position = Defines.HEADER_SIZE;
            push_int16(protocol_id);
        }

        public void record_size()
        {
            Int16 body_size = (Int16)(this.position - Defines.HEADER_SIZE);
            byte[] header = BitConverter.GetBytes(body_size);
            header.CopyTo(this.buffer, 0);
        }

        public void push_int16(Int16 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }

        public void push(byte data)
        {
            if (this.position + sizeof(byte) > this.buffer.Length)
            {
                throw new InvalidOperationException("Buffer is full");
            }

            this.buffer[this.position] = data;
            this.position += sizeof(byte);
        }

        public void push(float data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += sizeof(float);
        }

        public void push(Int16 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }

        public void push(Int32 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }

        public void push(string data)
        {
            byte[] temp_buffer = Encoding.UTF8.GetBytes(data);

            Int16 len = (Int16)temp_buffer.Length;
            byte[] len_buffer = BitConverter.GetBytes(len);
            len_buffer.CopyTo(this.buffer, this.position);
            this.position += sizeof(Int16);

            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }
    }
}