using System;
using System.IO;
using TcpWireProtocol.Headers;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Packets
{
    /// <summary>
    /// Запрос, отправляемый клиентом на сервер
    /// </summary>
    public class TcpWireCommand : IPacket<TcpWireCommandHeader>
    {
        /// <inheritdoc/>
        public TcpWireCommandHeader Header { get; }

        /// <inheritdoc/>
        public byte[] Payload { get; }

        /// <inheritdoc/>
        public byte[] RawBuffer { get; }

        /// <summary>
        /// ctor
        /// </summary>
        private TcpWireCommand(TcpWireCommandHeader header, byte[] payload = default)
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
        public TcpWireCommand(short service, short command, byte[] payload = default) :
            this(new TcpWireCommandHeader(service, command, payload?.Length ?? 0), payload)
        {
        }

        /// <summary>
        /// Парсинг пакета из данных
        /// </summary>
        public static bool TryParse(byte[] data, out TcpWireCommand command)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Присвоим значение по-умолчанию
            command = default;

            // Если получилось собрать заголовок пакета и данных достаточно, чтобы собрать пакет целиком
            if (TcpWireCommandHeader.TryParse(data, out TcpWireCommandHeader header) && data.Length >= TcpWireCommandHeader.HeaderLength + header.MainHeader.PayloadLength)
            {
                // Получим полезную нагрузку
                byte[] payload = new byte[header.MainHeader.PayloadLength];
                Array.Copy(data, TcpWireCommandHeader.HeaderLength, payload, 0, header.MainHeader.PayloadLength);

                // Сформируем пакет
                command = new TcpWireCommand(header, header.MainHeader.PayloadLength > 0 ? payload : default);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Создание ответа на запрос
        /// </summary>
        public TcpWireAnswer CreateAnswer(byte[] payload = default)
        {
            return new TcpWireAnswer(Header.MainHeader.CmdId, payload);
        }

        public override string ToString()
        {
            return Header.ToString();
        }
    }
}