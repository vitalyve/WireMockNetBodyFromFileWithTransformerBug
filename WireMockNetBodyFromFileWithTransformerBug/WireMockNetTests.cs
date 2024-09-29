using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace WireMockNetBodyFromFileWithTransformerBug
{
	public class WireMockNetTests : IDisposable
	{
		private WireMockServer _stub;
		private const string request =
"""
<xml>
	<Contact FirstName = "Ivan" />
</xml>
""";

		public WireMockNetTests()
		{
			_stub = WireMockServer.Start();

			var responseFilePath = Path.Combine(Environment.CurrentDirectory, "responses", "responseWithTransformer.xml");

			_stub.Given(
				Request.Create()
					.WithPath("/withbody")
					.UsingPost())
				.RespondWith(Response.Create()
					.WithSuccess()
					.WithTransformer()
					.WithBody(File.ReadAllText(responseFilePath)));

			_stub.Given(
				Request.Create()
					.WithPath("/withbodyfromfile")
					.UsingPost())
				.RespondWith(Response.Create()
					.WithSuccess()
					.WithTransformer(transformContentFromBodyAsFile: true)
					.WithBodyFromFile(responseFilePath));

			_stub.Given(
				Request.Create()
					.WithPath("/withbodyfromfile2")
					.UsingPost())
				.RespondWith(Response.Create()
					.WithSuccess()
					.WithBodyFromFile(responseFilePath)
					// Wrong usage! WithTransformer() should be used before WithBodyFromFile()!
					.WithTransformer(transformContentFromBodyAsFile: true));
		}

		public void Dispose()
		{
			if (_stub != null)
			{
				_stub.Stop();
				_stub.Dispose();
			}
		}

		[Fact]
		public async Task WithBodyShouldWorkWithTransformer()
		{
			var response = await GetResponse("/withbody");
			Assert.Contains("Hello, Ivan!", response);
		}

		[Fact]
		public async Task WithBodyFromFileShouldWorkWithTransformer()
		{
			var response = await GetResponse("/withbodyfromfile");
			Assert.Contains("Hello, Ivan!", response);
		}

		[Fact]
		public async Task WithBodyFromFileFailsWithTransformerAfterWithBodyFromFile()
		{
			var response = await GetResponse("/withbodyfromfile2");
			Assert.Empty(response);
		}

		private async Task<string> GetResponse(string relativePath)
		{
			using HttpClient httpClient = new HttpClient();
			httpClient.BaseAddress = new Uri(_stub.Urls[0]);

			using var requestContent = new StringContent(request);
			using var responseMsg = await httpClient.PostAsync(relativePath, requestContent);
			var responseContent = await responseMsg.Content.ReadAsStringAsync();
			
			return responseContent;
		}
	}
}