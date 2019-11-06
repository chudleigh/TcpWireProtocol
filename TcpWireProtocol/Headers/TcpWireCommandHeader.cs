using System;
using System.IO;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Headers
{
    /// <summary>
    /// Заголовок запроса
    /// </summary>
    public class TcpWireCommandHeader : IHeader
    {
        /// <summary>
        /// Размер заголовока
        /// </summary>
        public const int HeaderLength = MainHeader.HeaderLength + ServiceHeader.HeaderLength;

        /// <inheritdoc/>
        public MainHeader MainHeader { get; }

        /// <inheritdoc/>
        public ServiceHeader ServiceHeader { get; }

        /// <inheritdoc/>
        public byte[] RawBuffer { get; }

        /// <summary>
        /// ctor
        /// </summary>
        private TcpWireCommandHeader(int cmdId, short service, short command, int payloadLength)
        {
            if (payloadLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadLength));
            }

            MainHeader = new MainHeader(cmdId, payloadLength);
            ServiceHeader = new ServiceHeader(service, command);

            using MemoryStream ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(MainHeader.CmdId), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes(ServiceHeader.Service), 0, sizeof(short));
            ms.Write(BitConverter.GetBytes(ServiceHeader.Command), 0, sizeof(short));
            ms.Write(BitConverter.GetBytes(MainHeader.PayloadLength), 0, sizeof(int));

            RawBuffer = ms.ToArray();
        }

        /// <summary>
        /// ctor
        /// </summary>
        public TcpWireCommandHeader(short service, short command, int payloadLength) :
            this(unchecked(++_cmdId) == 0 ? ++_cmdId : _cmdId, service, command, payloadLength)
        {
        }

        /// <summary>
        /// Парсинг заголовка
        /// </summary>
        public static bool TryParse(byte[] data, out TcpWireCommandHeader header)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Присвоим значение по-умолчанию
            header = default;

            // Если данных недостаточно, чтобы собрать заголовок
            if (data.Length < HeaderLength)
            {
                return false;
            }

            int cmdId = BitConverter.ToInt32(data, 0);
            short service = BitConverter.ToInt16(data, sizeof(int));
            short command = BitConverter.ToInt16(data, sizeof(int) + sizeof(short));
            int length = BitConverter.ToInt32(data, sizeof(int) + sizeof(short) + sizeof(short));

            // Сформируем заголовок
            header = new TcpWireCommandHeader(cmdId, service, command, length);
            return true;
        }

        /// <summary>
        /// Автоинкрементируемое поле для создания уникального идентификатора запроса
        /// </summary>
        private static int _cmdId;
    }
}