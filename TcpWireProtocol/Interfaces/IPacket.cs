namespace TcpWireProtocol.Interfaces
{
    /// <summary>
    /// Пакет
    /// </summary>
    public interface IPacket<THeader>
        where THeader : IHeader
    {
        /// <summary>
        /// Заголовок
        /// </summary>
        THeader Header { get; }

        /// <summary>
        /// Полезная нагрузка
        /// </summary>
        byte[] Payload { get; }

        /// <summary>
        /// Сырые данные
        /// </summary>
        byte[] RawBuffer { get; }
    }
}