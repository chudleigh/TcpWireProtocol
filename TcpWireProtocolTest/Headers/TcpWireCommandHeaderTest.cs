using NUnit.Framework;
using System;
using TcpWireProtocol.Headers;

namespace TcpWireProtocolTest.Headers
{
    public class TcpWireCommandHeaderTest
    {
        /// <summary>
        /// Отрицательная длина полезной нагрузки
        /// </summary>
        [Test]
        public void Constructor_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TcpWireCommandHeader(10, 10, -10));
        }

        /// <summary>
        /// Данные для сбора заголовка = null
        /// </summary>
        [Test]
        public void TryParse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TcpWireCommandHeader.TryParse(null, out TcpWireCommandHeader header));
        }

        /// <summary>
        /// Данных для сбора заголовка достаточно. Полезная нагрузка присутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = new byte[] { 10, 20, 30 };

            TcpWireCommandHeader tmp_header = new TcpWireCommandHeader(service, command, payload.Length);

            bool result = TcpWireCommandHeader.TryParse(tmp_header.RawBuffer, out TcpWireCommandHeader header);

            Assert.IsTrue(result);
            Assert.IsNotNull(header);
            CollectionAssert.AreEqual(tmp_header.RawBuffer, header.RawBuffer);
            Assert.AreEqual(service, header.ServiceHeader.Service);
            Assert.AreEqual(command, header.ServiceHeader.Command);
            Assert.AreEqual(tmp_header.MainHeader.CmdId, header.MainHeader.CmdId);
            Assert.AreEqual(tmp_header.MainHeader.PayloadLength, header.MainHeader.PayloadLength);
            Assert.AreEqual(payload.Length, header.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Данных для сбора заголовка достаточно. Полезная нагрузка отсутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithoutPayload()
        {
            short service = 10;
            short command = 12;
            int payloadLength = 0;

            TcpWireCommandHeader tmp_header = new TcpWireCommandHeader(service, command, payloadLength);

            bool result = TcpWireCommandHeader.TryParse(tmp_header.RawBuffer, out TcpWireCommandHeader header);

            Assert.IsTrue(result);
            Assert.IsNotNull(header);
            CollectionAssert.AreEqual(tmp_header.RawBuffer, header.RawBuffer);
            Assert.AreEqual(service, header.ServiceHeader.Service);
            Assert.AreEqual(command, header.ServiceHeader.Command);
            Assert.AreEqual(tmp_header.MainHeader.CmdId, header.MainHeader.CmdId);
            Assert.AreEqual(tmp_header.MainHeader.PayloadLength, header.MainHeader.PayloadLength);
            Assert.AreEqual(payloadLength, header.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Данных для сбора заголовка не достаточно
        /// </summary>
        [Test]
        public void TryParse_NotEnoughData()
        {
            bool result = TcpWireCommandHeader.TryParse(new byte[] { 10, 20, 30 }, out TcpWireCommandHeader header);

            Assert.IsFalse(result);
            Assert.IsNull(header);
        }
    }
}