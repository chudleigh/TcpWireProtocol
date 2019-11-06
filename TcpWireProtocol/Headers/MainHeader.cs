namespace TcpWireProtocol.Headers
{
    public class MainHeader
    {
        /// <summary>
        /// Размер заголовока
        /// </summary>
        public const int HeaderLength = sizeof(int) + sizeof(int);

        /// <summary>
        /// Идентификатор запроса
        /// </summary>
        public int CmdId { get; }

        /// <summary>
        /// Длина полезной нагрузки
        /// </summary>
        public int PayloadLength { get; }

        /// <summary>
        /// ctor
        /// </summary>
        public MainHeader(int cmdId, int payloadLength)
        {
            CmdId = cmdId;
            PayloadLength = payloadLength;
        }
    }
}