using System;
using System.IO;
using TcpWireProtocol.Interfaces;

namespace TcpWireProtocol.Headers
{
    /// <summary>
    /// Заголовок ивента
    /// </summary>
    public class TcpWireEventHeader : TcpWireAnswerHeader
    {
        /// <summary>
        /// ctor
        /// </summary>
        public TcpWireEventHeader(short service, short command, int payloadLength) : base(0, payloadLength)
        {
            if (payloadLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(payloadLength));
            }

            ServiceHeader = new ServiceHeader(service, command);

            using MemoryStream ms = new MemoryStream();
            ms.Seek(sizeof(int), SeekOrigin.Begin);

            // Хак: Чтобы соотвествовать формату ответа, будет считать сервисный заголовок частью полезной нагрузки
            ms.Write(BitConverter.GetBytes(payloadLength + ServiceHeader.HeaderLength), 0, sizeof(int));
            ms.Write(BitConverter.GetBytes(ServiceHeader.Service), 0, sizeof(short));
            ms.Write(BitConverter.GetBytes(ServiceHeader.Command), 0, sizeof(short));

            RawBuffer = ms.ToArray();
        }

        /// <summary>
        /// Парсинг заголовка
        /// </summary>
        public static bool TryParse(byte[] data, out TcpWireEventHeader header)
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

            // Вычислим реальную длину полезной нагрузки
            int length = BitConverter.ToInt32(data, sizeof(int)) - ServiceHeader.HeaderLength;

            // Считаем сервисную часть
            short service = BitConverter.ToInt16(data, MainHeader.HeaderLength);
            short command = BitConverter.ToInt16(data, MainHeader.HeaderLength + sizeof(short));

            // Сформируем заголовок
            header = new TcpWireEventHeader(service, command, length);
            return true;
        }
    }
}