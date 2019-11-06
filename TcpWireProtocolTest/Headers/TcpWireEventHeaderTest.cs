using NUnit.Framework;
using System;
using System.IO;
using TcpWireProtocol.Headers;

namespace TcpWireProtocolTest.Headers
{
    public class TcpWireEventHeaderTest
    {
        /// <summary>
        /// Отрицательная длина полезной нагрузки
        /// </summary>
        [Test]
        public void Constructor_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TcpWireEventHeader(10, 10, -10));
        }

        /// <summary>
        /// Данные для сбора заголовка = null
        /// </summary>
        [Test]
        public void TryParse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TcpWireEventHeader.TryParse(null, out TcpWireEventHeader header));
        }

        /// <summary>
        /// Проверка хака с длиной полезной нагрузки
        /// </summary>
        [Test]
        public void Constructor_CreateEvent()
        {
            int payloadLength = 10;

            TcpWireEventHeader tmp_evt = new TcpWireEventHeader(10, 12, payloadLength);
            TcpWireAnswerHeader tmp_ans = new TcpWireAnswerHeader(1, payloadLength);

            Assert.AreEqual(tmp_evt.MainHeader.CmdId, 0);
            Assert.AreEqual(tmp_evt.MainHeader.PayloadLength, tmp_ans.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Данных для сбора заголовка достаточно. ПОлезная нагрузка присутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithPayload()
        {
            short service = 10;
            short command = 12;
            int payloadLength = 312;

            TcpWireEventHeader tmp_header = new TcpWireEventHeader(service, command, payloadLength);

            bool result = TcpWireEventHeader.TryParse(tmp_header.RawBuffer, out TcpWireEventHeader header);

            Assert.IsTrue(result);
            Assert.IsNotNull(header);
            CollectionAssert.AreEqual(tmp_header.RawBuffer, header.RawBuffer);
            Assert.AreEqual(payloadLength, header.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Заголовок event-а должен иметь возможность быть прочитанным так же как и заголовок ответа
        /// </summary>
        [Test]
        public void TryParse_AsAnswerHeader()
        {
            short service = 10;
            short command = 12;
            int payloadLength = 312;

            TcpWireEventHeader tmp_header = new TcpWireEventHeader(service, command, payloadLength);

            // Принудительно перепишем cmdId, чтобы не словить ArgumentOutOfRangeException на cmdId == 0
            using MemoryStream ms = new MemoryStream(tmp_header.RawBuffer);
            ms.Write(new byte[] { 1, 0, 0, 0 }, 0, sizeof(int));

            bool result = TcpWireAnswerHeader.TryParse(tmp_header.RawBuffer, out TcpWireAnswerHeader ans);

            Assert.IsTrue(result);
            Assert.IsNotNull(ans);

            // При чтении event-а его сервисная часть заголовка записывается в полезную нагрузку
            Assert.AreEqual(tmp_header.MainHeader.PayloadLength + ServiceHeader.HeaderLength, ans.MainHeader.PayloadLength);
            Assert.AreEqual(tmp_header.RawBuffer.Length, ans.RawBuffer.Length + ServiceHeader.HeaderLength);
        }

        /// <summary>
        /// Данных для сбора заголовка достаточно. ПОлезная нагрузка отсутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithoutPayload()
        {
            short service = 10;
            short command = 12;
            int payloadLength = 0;

            TcpWireEventHeader tmp_header = new TcpWireEventHeader(service, command, payloadLength);

            bool result = TcpWireEventHeader.TryParse(tmp_header.RawBuffer, out TcpWireEventHeader header);

            Assert.IsTrue(result);
            Assert.IsNotNull(header);
            CollectionAssert.AreEqual(tmp_header.RawBuffer, header.RawBuffer);
            Assert.AreEqual(payloadLength, header.MainHeader.PayloadLength);
        }

        /// <summary>
        /// Данных для сбора заголовка не достаточно
        /// </summary>
        [Test]
        public void TryParse_NotEnoughData()

        {
            bool result = TcpWireEventHeader.TryParse(new byte[] { 10, 20, 30 }, out TcpWireEventHeader header);

            Assert.IsFalse(result);
            Assert.IsNull(header);
        }
    }
}