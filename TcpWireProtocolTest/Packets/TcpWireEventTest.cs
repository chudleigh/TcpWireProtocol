using NUnit.Framework;
using System;
using TcpWireProtocol.Packets;

namespace TcpWireProtocolTest.Packets
{
    public class TcpWireEventTest
    {
        /// <summary>
        /// Данные для сбора пакета = null
        /// </summary>
        [Test]
        public void TryParse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TcpWireEvent.TryParse(null, out TcpWireEvent evt));
        }

        /// <summary>
        /// Данных для сбора пакета достаточно. Полезная нагрузка присутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = new byte[] { 10, 20, 30 };
            TcpWireEvent tmp_evt = new TcpWireEvent(service, command, payload);

            bool result = TcpWireEvent.TryParse(tmp_evt.RawBuffer, out TcpWireEvent evt);

            Assert.IsTrue(result);
            Assert.IsNotNull(evt);
            CollectionAssert.AreEqual(tmp_evt.RawBuffer, evt.RawBuffer);
            CollectionAssert.AreEqual(tmp_evt.Payload, evt.Payload);
            CollectionAssert.AreEqual(evt.Payload, payload);
        }

        /// <summary>
        /// Данных для сбора пакета достаточно. ПОлезная нагрузка отсутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithoutPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = null;
            TcpWireEvent tmp_evt = new TcpWireEvent(service, command, payload);

            bool result = TcpWireEvent.TryParse(tmp_evt.RawBuffer, out TcpWireEvent evt);

            Assert.IsTrue(result);
            Assert.IsNotNull(evt);
            CollectionAssert.AreEqual(tmp_evt.RawBuffer, evt.RawBuffer);
            CollectionAssert.AreEqual(tmp_evt.Payload, evt.Payload);
            CollectionAssert.AreEqual(evt.Payload, payload);
        }

        /// <summary>
        /// Данных для сбора пакета не достаточно
        /// </summary>
        [Test]
        public void TryParse_NotEnoughData()
        {
            bool result = TcpWireAnswer.TryParse(new byte[] { 10, 20, 30 }, out TcpWireAnswer ans);

            Assert.IsFalse(result);
            Assert.IsNull(ans);
        }
    }
}