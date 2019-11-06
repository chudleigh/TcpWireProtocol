using System;
using System.IO;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Headers
{
    /// <summary>
    /// Заголовок ответа
    /// </summary>
    public class TcpWireAnswerHeader : IHeader
    {
        /// <summary>
        /// Размер заголовока
        /// </summary>
        public const int HeaderLength = MainHeader.HeaderLength;

        /// <inheritdoc/>
        public MainHeader MainHeader { get; }

        /// <inheritdoc/>
        public ServiceHeader ServiceHeader { get; protected set; } = default;

        /// <inheritdoc/>
        public byte[] RawBuffer { get; protected set; }

        /// <summary>
        /// ctor
        /// </summary>
        public TcpWireAnswerHeader(int cmdId, int payloadLength)
        {
            if (payloadLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadLength));
            }

            MainHeader = new MainHeader(cmdId, payloadLength);

            using MemoryStream ms = new MemoryStream();
            ms.Write(BitConverter.GetBytes(MainHeader.CmdId), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes(MainHeader.PayloadLength), 0, sizeof(int));

            RawBuffer = ms.ToArray();
        }

        /// <summary>
        /// Парсинг заголовка
        /// </summary>
        public static bool TryParse(byte[] data, out TcpWireAnswerHeader header)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Присвоим знаечние по-умолчанию
            header = default;
            if (data.Length < HeaderLength)
            {
                return false;
            }

            int cmdId = BitConverter.ToInt32(data, 0);
            int length = BitConverter.ToInt32(data, sizeof(int));

            // Сформируем заголовок
            header = new TcpWireAnswerHeader(cmdId, length);
            return true;
        }
    }
}