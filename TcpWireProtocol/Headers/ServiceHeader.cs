namespace TcpWireProtocol.Headers
{
    public class ServiceHeader
    {
        /// <summary>
        /// Размер заголовка
        /// </summary>
        public const int HeaderLength = sizeof(short) + sizeof(short);

        /// <summary>
        /// Сервис, отвечающий за обработку запроса
        /// </summary>
        public short Service { get; }

        /// <summary>
        /// Команда, определяющая обработчик запроса для сервиса
        /// </summary>
        public short Command { get; }

        /// <summary>
        /// ctor
        /// </summary>
        public ServiceHeader(short service, short command)
        {
            Service = service;
            Command = command;
        }
    }
}