using TcpWireProtocol.Headers;

namespace TcpWireProtocol.Interfaces
{
    /// <summary>
    /// Заголовок пакета
    /// </summary>
    public interface IHeader
    {
        /// <summary>
        /// Основной заголовок пакета
        /// </summary>
        MainHeader MainHeader { get; }

        /// <summary>
        /// Сервисный заголовок
        /// </summary>
        ServiceHeader ServiceHeader { get; }

        /// <summary>
        /// Сырые данные заголовка
        /// </summary>
        byte[] RawBuffer { get; }
    }
}