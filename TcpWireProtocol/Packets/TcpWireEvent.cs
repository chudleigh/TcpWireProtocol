using System;
using System.IO;
using TcpWireProtocol.Headers;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Packets
{
    /// <summary>
    /// Пакет, посылаемый по инициативе сервера
    /// </summary>
    public class TcpWireEvent : IPacket<TcpWireEventHeader>
    {
        /// <inheritdoc/>
        public TcpWireEventHeader Header { get; }

        /// <inheritdoc/>
        public byte[] Payload { get; }

        /// <inheritdoc/>
        public byte[] RawBuffer { get; }

        /// <summary>
        /// ctor
        /// </summary>
        private TcpWireEvent(TcpWireEventHeader header, byte[] payload = default)
        {
            Payload = payload;
            Header = header;

            using MemoryStream ms = new MemoryStream();
            ms.Write(header.RawBuffer, 0, header.RawBuffer.Length);
            if (payload != default)
            {
                ms.Write(payload, 0, payload.Length);
            }
            RawBuffer = ms.ToArray();
        }

        /// <summary>
        /// ctor
        /// </summary>
        public TcpWireEvent(short service, short command, byte[] payload = default) :
            this(new TcpWireEventHeader(service, command, payload?.Length ?? 0), payload)
        {
        }

        /// <summary>
        /// Парсинг пакета из данных
        /// </summary>
        public static bool TryParse(byte[] data, out TcpWireEvent evt)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Присвоим значение по-умолчанию
            evt = default;

            // Если получилось собрать заголовок пакета и данных достаточно, чтобы собрать пакет целиком
            if (TcpWireEventHeader.TryParse(data, out TcpWireEventHeader header) && data.Length >= TcpWireAnswerHeader.HeaderLength + header.MainHeader.PayloadLength + sizeof(short) + sizeof(short))
            {
                byte[] payload = new byte[header.MainHeader.PayloadLength];
                Array.Copy(data, TcpWireAnswerHeader.HeaderLength + sizeof(short) + sizeof(short), payload, 0, header.MainHeader.PayloadLength);

                evt = new TcpWireEvent(header, header.MainHeader.PayloadLength > 0 ? payload : default);
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return Header.ToString();
        }
    }
}