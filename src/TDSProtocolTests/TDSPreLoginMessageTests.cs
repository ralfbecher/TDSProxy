using Xunit;
using TDSProtocol;

namespace TDSProtocolTests
{
	public class TDSPreLoginMessageTests
	{
		[Fact]
		public void TestInterpretPayloadSimple()
		{
			TDSPreLoginMessage expected =
				new TDSPreLoginMessage
				{
					Version = new TDSPreLoginMessage.VersionInfo { Version = 0x09000000, SubBuild = 0x0000 },
					Encryption = TDSPreLoginMessage.EncryptionEnum.On,
					InstValidity = new byte[] { 0x00 },
					ThreadId = 0x00000DB8,
					Mars = TDSPreLoginMessage.MarsEnum.On
				};


			TDSPreLoginMessage actual = new TDSPreLoginMessage
			                            {
				                            Payload = new byte[]
				                                      {
					                                      0x00, 0x00, 0x1A, 0x00, 0x06, 0x01, 0x00, 0x20, 0x00, 0x01,
					                                      0x02, 0x00, 0x21, 0x00, 0x01, 0x03, 0x00, 0x22, 0x00, 0x04,
					                                      0x04, 0x00, 0x26, 0x00, 0x01, 0xFF, 0x09, 0x00, 0x00, 0x00,
					                                      0x00, 0x00, 0x01, 0x00, 0xB8, 0x0D, 0x00, 0x00, 0x01
				                                      }
			                            };
			actual.InterpretPayload();

			Assert.Equal(expected.Version, actual.Version);
			Assert.Equal(expected.Encryption, actual.Encryption);
			EnumerableAssert.AreEqual(expected.InstValidity, actual.InstValidity);
			Assert.Equal(expected.ThreadId, actual.ThreadId);
			Assert.Equal(expected.Mars, actual.Mars);
			Assert.Equal(expected.TraceId, actual.TraceId);
			Assert.Equal(expected.FedAuthRequired, actual.FedAuthRequired);
			EnumerableAssert.AreEqual(expected.Nonce, actual.Nonce);
		}

		[Fact]
		public void TestInterpretPayloadEmptyTrace()
		{
			TDSPreLoginMessage expected =
				new TDSPreLoginMessage
				{
					Version = new TDSPreLoginMessage.VersionInfo { Version = 0x09000000, SubBuild = 0x0000 },
					Encryption = TDSPreLoginMessage.EncryptionEnum.On,
					InstValidity = new byte[] { 0x00 },
					ThreadId = 0x00000DB8,
					Mars = TDSPreLoginMessage.MarsEnum.On
				};


			TDSPreLoginMessage actual = new TDSPreLoginMessage
			                            {
				                            Payload = new byte[]
				                                      {
					                                      0x00, 0x00, 0x1F, 0x00, 0x06, 0x01, 0x00, 0x25, 0x00, 0x01,
					                                      0x02, 0x00, 0x26, 0x00, 0x01, 0x03, 0x00, 0x27, 0x00, 0x04,
					                                      0x04, 0x00, 0x2B, 0x00, 0x01, 0x05, 0x00, 0x2C, 0x00, 0x00,
					                                      0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0xB8,
					                                      0x0D, 0x00, 0x00, 0x01
				                                      }
			                            };
			actual.InterpretPayload();

			Assert.Equal(expected.Version, actual.Version);
			Assert.Equal(expected.Encryption, actual.Encryption);
			EnumerableAssert.AreEqual(expected.InstValidity, actual.InstValidity);
			Assert.Equal(expected.ThreadId, actual.ThreadId);
			Assert.Equal(expected.Mars, actual.Mars);
			Assert.Equal(expected.TraceId, actual.TraceId);
			Assert.Equal(expected.FedAuthRequired, actual.FedAuthRequired);
			EnumerableAssert.AreEqual(expected.Nonce, actual.Nonce);
		}

		[Fact]
		public void TestGeneratePayloadSimple()
		{
			TDSPreLoginMessage msg =
				new TDSPreLoginMessage
				{
					Version = new TDSPreLoginMessage.VersionInfo { Version = 0x09000000, SubBuild = 0x0000 },
					Encryption = TDSPreLoginMessage.EncryptionEnum.On,
					InstValidity = new byte[] { 0x00 },
					ThreadId = 0x00000DB8,
					Mars = TDSPreLoginMessage.MarsEnum.On
				};
			msg.GeneratePayload();

			var expected = new byte[]
			{
				0x00, 0x00, 0x1A, 0x00, 0x06, 0x01, 0x00, 0x20, 0x00, 0x01, 0x02, 0x00, 0x21, 0x00, 0x01, 0x03,
				0x00, 0x22, 0x00, 0x04, 0x04, 0x00, 0x26, 0x00, 0x01, 0xFF, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x01, 0x00, 0xB8, 0x0D, 0x00, 0x00, 0x01
			};
			var actual = msg.Payload;

			EnumerableAssert.AreEqual(expected, actual);
		}
	}
}
