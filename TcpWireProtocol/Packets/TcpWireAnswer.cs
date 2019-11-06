using System;
using System.IO;
using TcpWireProtocol.Headers;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Packets
{
    /// <summary>
    /// Ответ на запрос клиента
    /// </summary>
    public class TcpWireAnswer : IPacket<TcpWireAnswerHeader>
    {
        /// <inheritdoc/>
        public TcpWireAnswerHeader Header { get; }

        /// <inheritdoc/>
        public byte[] Payload { get; }

        /// <inheritdoc/>
        public byte[] RawBuffer { get; }

        /// <summary>
        /// ctor
        /// </summary>
        private TcpWireAnswer(TcpWireAnswerHeader header, byte[] payload = default)
        {
            Payload = payload;
            Header = header;

            using MemoryStream ms = new MemoryStream();
            ms.Write(Header.RawBuffer, 0, Header.RawBuffer.Length);
            if (payload != default)
            {
                ms.Write(payload, 0, payload.Length);
            }

            RawBuffer = ms.ToArray();
        }

        /// <summary>
        /// ctor
        /// </summary>
        public TcpWireAnswer(int cmdId, byte[] payload = default) :
            this(new TcpWireAnswerHeader(cmdId, payload?.Length ?? 0), payload)
        {
        }

        /// <summary>
        /// Парсинг пакета из данных
        /// </summary>
        public static bool TryParse(byte[] data, out TcpWireAnswer answer)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Присвоим значение по-умолчанию
            answer = default;

            // Если получилось собрать заголовок пакета и данных достаточно, чтобы собрать пакет целиком
            if (TcpWireAnswerHeader.TryParse(data, out TcpWireAnswerHeader header) && data.Length >= TcpWireAnswerHeader.HeaderLength + header.MainHeader.PayloadLength)
            {
                byte[] payload = new byte[header.MainHeader.PayloadLength];
                Array.Copy(data, TcpWireAnswerHeader.HeaderLength, payload, 0, header.MainHeader.PayloadLength);

                answer = new TcpWireAnswer(header, header.MainHeader.PayloadLength > 0 ? payload : default);
                return true;
            }

            return false;
        }
    }
}