using bashGPT.Core.Models.Storage;
using bashGPT.Core.Storage;

namespace bashGPT.Core.Tests.Storage;

public sealed class SessionRequestStoreTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"bashgpt-test-{Guid.NewGuid()}");

    private string SessionsDir => Path.Combine(_tempDir, "sessions");

    public SessionRequestStoreTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SessionRequestStore CreateStore() => new(SessionsDir);

    [Theory]
    [InlineData("..")]
    [InlineData("../evil")]
    public async Task SaveRequestAsync_InvalidSessionId_ThrowsArgumentException(string invalidId)
    {
        var store = CreateStore();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            store.SaveRequestAsync(invalidId, MakeRequest("2026-03-08T15:30:00.000Z")));
    }

    [Fact]
    public async Task SaveRequestAsync_CreatesRequestFileWithTimestampName()
    {
        var store = CreateStore();

        await store.SaveRequestAsync("s1", MakeRequest("2026-03-08T15:30:00.000Z"));

        var files = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json");
        Assert.Single(files);
        Assert.Contains("2026-03-08T15-30-00.000Z.json", Path.GetFileName(files[0]));
    }

    [Fact]
    public async Task SaveLlmRequestAsync_PreservesRawContent()
    {
        var store = CreateStore();
        const string llmJson = "{\"model\":\"llama3\",\"messages\":[]}";

        await store.SaveLlmRequestAsync("s1", "2026-03-08T15:30:00.000Z", llmJson);

        var file = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*-llm-request.json").Single();
        var content = await File.ReadAllTextAsync(file);
        Assert.Equal(llmJson, content);
    }

    [Fact]
    public async Task SaveLlmResponseAsync_PreservesRawContent()
    {
        var store = CreateStore();
        const string raw = "data: {\"choices\":[{\"delta\":{\"content\":\"Hallo\"}}]}\ndata: [DONE]";

        await store.SaveLlmResponseAsync("s1", "2026-03-08T15:30:00.000Z", raw);

        var file = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*-llm-response.json").Single();
        var content = await File.ReadAllTextAsync(file);
        Assert.Equal(raw, content);
    }

    [Fact]
    public async Task SaveRequestArtifacts_UseSeparateFiles()
    {
        var store = CreateStore();

        const string timestamp = "2026-03-08T15:30:00.000Z";
        await store.SaveRequestAsync("s1", MakeRequest(timestamp));
        await store.SaveLlmRequestAsync("s1", timestamp, "{\"model\":\"test\"}");
        await store.SaveLlmResponseAsync("s1", timestamp, "data: {}");

        var files = Directory.GetFiles(Path.Combine(SessionsDir, "s1", "requests"), "*.json");
        Assert.Equal(3, files.Length);
        Assert.Single(files, f => !f.Contains("-llm"));
        Assert.Single(files, f => f.EndsWith("-llm-request.json"));
        Assert.Single(files, f => f.EndsWith("-llm-response.json"));
    }

    private static SessionRequestRecord MakeRequest(string timestamp) => new()
    {
        Timestamp = timestamp,
        Request = new SessionRequestData { Prompt = "Test prompt" },
        Response = new SessionResponseData { Content = "Test response" },
    };
}
