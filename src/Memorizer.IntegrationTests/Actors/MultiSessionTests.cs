using System.Collections.Concurrent;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace Memorizer.IntegrationTests.Actors;

/// <summary>
/// Test to verify multiple concurrent sessions work correctly
/// </summary>
public class MultiSessionTests
{
    private readonly ITestOutputHelper _output;

    public MultiSessionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Manual test - requires running application")]
    public async Task Multiple_Sessions_Should_Work_Concurrently()
    {
        // This test requires the application to be running
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        // Create two different sessions
        var session1 = Guid.NewGuid().ToString();
        var session2 = Guid.NewGuid().ToString();

        var messages1 = new ConcurrentBag<string>();
        var messages2 = new ConcurrentBag<string>();

        _output.WriteLine($"Session 1: {session1}");
        _output.WriteLine($"Session 2: {session2}");

        // Start SSE connections for both sessions
        var sse1Task = Task.Run(async () =>
        {
            try
            {
                using var response = await httpClient.GetStreamAsync($"/api/askbot/stream?sessionId={session1}");
                using var reader = new StreamReader(response);

                _output.WriteLine("Session 1 SSE connected");

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data: "))
                    {
                        messages1.Add(line.Substring(6));
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Session 1 SSE error: {ex.Message}");
            }
        });

        var sse2Task = Task.Run(async () =>
        {
            try
            {
                using var response = await httpClient.GetStreamAsync($"/api/askbot/stream?sessionId={session2}");
                using var reader = new StreamReader(response);

                _output.WriteLine("Session 2 SSE connected");

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("data: "))
                    {
                        messages2.Add(line.Substring(6));
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Session 2 SSE error: {ex.Message}");
            }
        });

        // Wait a bit for SSE connections to establish
        await Task.Delay(2000);

        // Send messages to both sessions concurrently
        var send1Task = httpClient.PostAsJsonAsync("/api/askbot/message", new
        {
            message = "What is 2+2?",
            sessionId = session1
        });

        var send2Task = httpClient.PostAsJsonAsync("/api/askbot/message", new
        {
            message = "What is the capital of France?",
            sessionId = session2
        });

        // Wait for both requests to complete
        var responses = await Task.WhenAll(send1Task, send2Task);

        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
        }

        _output.WriteLine("Both messages sent successfully");

        // Wait for processing
        await Task.Delay(10000);

        // Check session info
        var session1Info = await httpClient.GetFromJsonAsync<dynamic>($"/api/askbot/session/{session1}");
        var session2Info = await httpClient.GetFromJsonAsync<dynamic>($"/api/askbot/session/{session2}");

        _output.WriteLine($"Session 1 messages received: {messages1.Count}");
        _output.WriteLine($"Session 2 messages received: {messages2.Count}");

        // Both sessions should receive messages
        Assert.True(messages1.Count > 0, "Session 1 should receive messages");
        Assert.True(messages2.Count > 0, "Session 2 should receive messages");

        // Messages should be different (different questions)
        var session1Content = string.Join(" ", messages1);
        var session2Content = string.Join(" ", messages2);

        _output.WriteLine($"Session 1 content sample: {session1Content.Substring(0, Math.Min(200, session1Content.Length))}");
        _output.WriteLine($"Session 2 content sample: {session2Content.Substring(0, Math.Min(200, session2Content.Length))}");

        // Verify no cross-contamination
        Assert.DoesNotContain("capital of France", session1Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2+2", session2Content, StringComparison.OrdinalIgnoreCase);

        _output.WriteLine("Test passed: Both sessions work independently");
    }

    [Fact(Skip = "Manual test - requires running application")]
    public async Task Multiple_Connections_Same_Session_Should_Receive_Same_Messages()
    {
        // This test verifies that multiple browser tabs with same session receive the same messages
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        var sessionId = Guid.NewGuid().ToString();

        var connection1Messages = new ConcurrentBag<string>();
        var connection2Messages = new ConcurrentBag<string>();

        _output.WriteLine($"Session ID: {sessionId}");

        // Start two SSE connections for the same session (simulating two browser tabs)
        var sse1Task = Task.Run(async () =>
        {
            try
            {
                using var response = await httpClient.GetStreamAsync($"/api/askbot/stream?sessionId={sessionId}");
                using var reader = new StreamReader(response);

                _output.WriteLine("Connection 1 SSE connected");

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("event: reasoning"))
                    {
                        connection1Messages.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Connection 1 SSE error: {ex.Message}");
            }
        });

        var sse2Task = Task.Run(async () =>
        {
            try
            {
                using var response = await httpClient.GetStreamAsync($"/api/askbot/stream?sessionId={sessionId}");
                using var reader = new StreamReader(response);

                _output.WriteLine("Connection 2 SSE connected");

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("event: reasoning"))
                    {
                        connection2Messages.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Connection 2 SSE error: {ex.Message}");
            }
        });

        // Wait for connections to establish
        await Task.Delay(2000);

        // Check session info
        var sessionInfo = await httpClient.GetFromJsonAsync<dynamic>($"/api/askbot/session/{sessionId}");
        _output.WriteLine($"Session info: {sessionInfo}");

        // Send a message
        var response = await httpClient.PostAsJsonAsync("/api/askbot/message", new
        {
            message = "Hello, how are you?",
            sessionId = sessionId
        });

        response.EnsureSuccessStatusCode();
        _output.WriteLine("Message sent successfully");

        // Wait for processing
        await Task.Delay(5000);

        _output.WriteLine($"Connection 1 received {connection1Messages.Count} reasoning events");
        _output.WriteLine($"Connection 2 received {connection2Messages.Count} reasoning events");

        // Both connections should receive the same events
        Assert.True(connection1Messages.Count > 0, "Connection 1 should receive events");
        Assert.True(connection2Messages.Count > 0, "Connection 2 should receive events");

        // The count should be the same for both connections
        Assert.Equal(connection1Messages.Count, connection2Messages.Count);

        _output.WriteLine("Test passed: Multiple connections for same session work correctly");
    }
}