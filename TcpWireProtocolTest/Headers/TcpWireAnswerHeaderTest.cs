using NUnit.Framework;
using System;
using TcpWireProtocol.Headers;

namespace TcpWireProtocolTest.Headers
{
    public class TcpWireAnswerHeaderTest
    {
        /// <summary>
        /// Отрицательная длина полезной нагрузки
        /// </summary>
        [Test]
        public void Constructor_ArgumentOutOfRangeException_PayloadLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TcpWireAnswerHeader(1, -10));
        }

        /// <summary>
        /// Данные для сбора заголовка = null
        /// </summary>
        [Test]
        public void TryParse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TcpWireAnswerHeader.TryParse(null, out TcpWireAnswerHeader header));
        }

        /// <summary>
        /// Данных для сбора заголовка достаточно. Полезная нагрузка присутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithPayload()
        {
            int cmdId = 3151;
            int payloadLength = 812;
            TcpWireAnswerHeader tmp_header = new TcpWireAnswerHeader(cmdId, payloadLength);

            bool result = TcpWireAnswerHeader.TryParse(tmp_header.RawBuffer, out TcpWireAnswerHeader header);

            Assert.IsTrue(result);
            Assert.IsNotNull(header);
            CollectionAssert.AreEqual(tmp_header.RawBuffer, header.RawBuffer);
            Assert.AreEqual(cmdId, header.MainHeader.CmdId);
            Assert.AreEqual(payloadLength, header.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Данных для сбора заголовка достаточно. Полезная нагрузка отсутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithoutPayload()
        {
            int cmdId = 3151;
            int payloadLength = 0;

            TcpWireAnswerHeader tmp_header = new TcpWireAnswerHeader(cmdId, payloadLength);

            bool result = TcpWireAnswerHeader.TryParse(tmp_header.RawBuffer, out TcpWireAnswerHeader header);

            Assert.IsTrue(result);
            Assert.IsNotNull(header);
            CollectionAssert.AreEqual(tmp_header.RawBuffer, header.RawBuffer);
            Assert.AreEqual(cmdId, header.MainHeader.CmdId);
            Assert.AreEqual(payloadLength, header.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Данных для сбора заголовка не достаточно
        /// </summary>
        [Test]
        public void TryParse_NotEnoughData()
        {
            bool result = TcpWireAnswerHeader.TryParse(new byte[] { 10, 20, 30 }, out TcpWireAnswerHeader header);

            Assert.IsFalse(result);
            Assert.IsNull(header);
        }
    }
}