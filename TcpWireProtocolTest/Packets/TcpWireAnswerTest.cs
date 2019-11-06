using NUnit.Framework;
using System;
using TcpWireProtocol.Packets;

namespace TcpWireProtocolTest.Packets
{
    public class TcpWireAnswerTest
    {
        /// <summary>
        /// Данные для сбора пакета = null
        /// </summary>
        [Test]
        public void TryParse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TcpWireAnswer.TryParse(null, out TcpWireAnswer ans));
        }

        /// <summary>
        /// Данных для сбора пакета достаточно. Полезная нагрузка присутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithPayload()
        {
            int cmdId = 3151;
            byte[] payload = new byte[] { 10, 20, 30 };
            TcpWireAnswer tmp_ans = new TcpWireAnswer(cmdId, payload);

            bool result = TcpWireAnswer.TryParse(tmp_ans.RawBuffer, out TcpWireAnswer ans);

            Assert.IsTrue(result);
            Assert.IsNotNull(ans);
            Assert.AreEqual(cmdId, ans.Header.MainHeader.CmdId);
            CollectionAssert.AreEqual(tmp_ans.RawBuffer, ans.RawBuffer);
            CollectionAssert.AreEqual(payload, ans.Payload);
        }

        /// <summary>
        /// Данных для сбора пакета достаточно. Полезная нагрузка отсутствует
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithoutPayload()
        {
            int cmdId = 3151;
            byte[] payload = null;
            TcpWireAnswer tmp_ans = new TcpWireAnswer(cmdId, payload);

            bool result = TcpWireAnswer.TryParse(tmp_ans.RawBuffer, out TcpWireAnswer ans);

            Assert.IsTrue(result);
            Assert.IsNotNull(ans);
            Assert.AreEqual(cmdId, ans.Header.MainHeader.CmdId);
            CollectionAssert.AreEqual(tmp_ans.RawBuffer, ans.RawBuffer);
            CollectionAssert.AreEqual(payload, ans.Payload);
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