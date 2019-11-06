using NUnit.Framework;
using System;
using TcpWireProtocol.Packets;

namespace TcpWireProtocolTest.Packets
{
    public class TcpWireCommandTest
    {
        /// <summary>
        /// Данные для сбора пакета = null
        /// </summary>
        [Test]
        public void TryParse_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TcpWireCommand.TryParse(null, out TcpWireCommand cmd));
        }

        /// <summary>
        /// Данных для сбора пакета достаточно
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = new byte[] { 10, 20, 30 };
            TcpWireCommand tmp_cmd = new TcpWireCommand(service, command, payload);

            bool result = TcpWireCommand.TryParse(tmp_cmd.RawBuffer, out TcpWireCommand cmd);

            Assert.IsTrue(result);
            Assert.IsNotNull(cmd);
            CollectionAssert.AreEqual(tmp_cmd.RawBuffer, cmd.RawBuffer);
            CollectionAssert.AreEqual(tmp_cmd.Payload, cmd.Payload);
        }

        /// <summary>
        /// Данных для сбора пакета достаточно
        /// </summary>
        [Test]
        public void TryParse_EnoughDataWithoutPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = null;
            TcpWireCommand tmp_cmd = new TcpWireCommand(service, command, payload);

            bool result = TcpWireCommand.TryParse(tmp_cmd.RawBuffer, out TcpWireCommand cmd);

            Assert.IsTrue(result);
            Assert.IsNotNull(cmd);
            CollectionAssert.AreEqual(tmp_cmd.RawBuffer, cmd.RawBuffer);
            CollectionAssert.AreEqual(tmp_cmd.Payload, cmd.Payload);
        }

        /// <summary>
        /// Данных для сбора пакета не достаточно
        /// </summary>
        [Test]
        public void TryParse_NotEnoughData()
        {
            bool result = TcpWireCommand.TryParse(new byte[] { 10, 20, 30 }, out TcpWireCommand cmd);

            Assert.IsFalse(result);
            Assert.IsNull(cmd);
        }

        /// <summary>
        /// Создание ответа на запрос. Полезная нагрузка присутствует
        /// </summary>
        [Test]
        public void CreateAnswer_WithPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = new byte[] { 10, 20, 30 };

            TcpWireCommand tmp_cmd = new TcpWireCommand(service, command);
            TcpWireAnswer tmp_ans = tmp_cmd.CreateAnswer(payload);

            Assert.IsNotNull(tmp_ans);
            Assert.AreEqual(tmp_cmd.Header.MainHeader.CmdId, tmp_ans.Header.MainHeader.CmdId);
            CollectionAssert.AreEqual(payload, tmp_ans.Payload);
        }

        /// <summary>
        /// Создание ответа за запрос. Полезная нагрузка отсутствует
        /// </summary>
        [Test]
        public void CreateAnswer_WithoutPayload()
        {
            short service = 10;
            short command = 12;
            byte[] payload = null;

            TcpWireCommand tmp_cmd = new TcpWireCommand(service, command);
            TcpWireAnswer tmp_ans = tmp_cmd.CreateAnswer(payload);

            Assert.IsNotNull(tmp_ans);
            Assert.AreEqual(tmp_cmd.Header.MainHeader.CmdId, tmp_ans.Header.MainHeader.CmdId);
            CollectionAssert.AreEqual(payload, tmp_ans.Payload);
        }
    }
}