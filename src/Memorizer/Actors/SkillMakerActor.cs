using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using System.Text;
using System.Text.Json;

namespace Memorizer.Actors;

/// <summary>
/// Agent-Actor for conversational Claude Code Skill creation
/// State machine: WaitingForCategory → WaitingForSubSkill → GatheringInfo → GeneratingSkill → Complete
/// Uses Task.Run + Self.Tell pattern for async operations (no async void)
/// </summary>
public class SkillMakerActor : ReceiveActor, IWithTimers
{
    private readonly string _sessionId;
    private readonly ILlmExService _llmExService;
    private readonly IActorRef _sseBridge;
    private readonly ILoggingAdapter _logger;

    // Timer for session timeout
    private const string SessionTimerKey = "skill-session-timeout";
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromDays(3);

    // State
    private string _selectedCategory = string.Empty;
    private string _selectedSkillName = string.Empty;
    private readonly List<SkillConversationEntry> _conversationEntries = new();
    private string _generatedSkillContent = string.Empty;
    private int _questionCount = 0;
    private const int MaxQuestions = 5;

    public ITimerScheduler Timers { get; set; } = null!;

    // Internal messages for async operation results (Task.Run → Self.Tell pattern)
    private sealed record SubSkillSuggestionsResult(string LlmResponse);
    private sealed record SubSkillSuggestionsError(string ErrorMessage);
    private sealed record FollowUpQuestionResult(string LlmResponse);
    private sealed record FollowUpQuestionError(string ErrorMessage);
    private sealed record SkillChunkResult(string Chunk);
    private sealed record SkillGenerationCompleted(string FullContent);
    private sealed record SkillGenerationError(string ErrorMessage);
    private sealed record UsageGuideResult(string SkillContent, string Guide);
    private sealed record UsageGuideError(string SkillContent);

    public SkillMakerActor(
        string sessionId,
        ILlmExService llmExService,
        IActorRef sseBridge)
    {
        _sessionId = sessionId;
        _llmExService = llmExService;
        _sseBridge = sseBridge;
        _logger = Context.GetLogger();

        // Initial state
        Context.Become(WaitingForCategory());

        // Start session timer
        ResetSessionTimer();

        _logger.Info("SkillMakerActor created for session {0}", _sessionId);
    }

    // ===== State: WaitingForCategory =====

    private Receive WaitingForCategory()
    {
        return message =>
        {
            switch (message)
            {
                case SkillMakerUserMessage msg when msg.MessageType == SkillMakerMessageType.Category:
                    HandleCategorySelection(msg);
                    return true;
                case SkillMakerUserMessage msg when msg.MessageType == SkillMakerMessageType.CustomInput:
                    HandleCategorySelection(new SkillMakerUserMessage
                    {
                        SessionId = msg.SessionId,
                        Message = msg.Message,
                        MessageType = SkillMakerMessageType.Category
                    });
                    return true;
                case SkillMakerUserMessage:
                    SendWelcomeMessage();
                    return true;
                case SkillMakerSessionTimeout timeout:
                    HandleSessionTimeout(timeout);
                    return true;
                default:
                    return false;
            }
        };
    }

    private void SendWelcomeMessage()
    {
        var welcomeMsg = SkillMakerPrompts.GetWelcomeMessage();
        var options = SkillMakerPrompts.GetCategoryOptions();

        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Options,
            Content = welcomeMsg,
            Options = options,
            NextMessageType = "category"
        });
    }

    private void HandleCategorySelection(SkillMakerUserMessage msg)
    {
        _selectedCategory = msg.Message;
        _logger.Info("Category selected for session {0}: {1}", _sessionId, _selectedCategory);

        _conversationEntries.Add(new SkillConversationEntry
        {
            Role = "user",
            Content = $"직군 선택: {_selectedCategory}",
            MessageType = SkillMakerMessageType.Category
        });

        // Transition to WaitingForSubSkill BEFORE starting async work
        Context.Become(WaitingForSubSkill());

        // Kick off async LLM call
        StartSubSkillSuggestions();
    }

    // ===== State: WaitingForSubSkill =====

    private Receive WaitingForSubSkill()
    {
        return message =>
        {
            switch (message)
            {
                // Async result from LLM
                case SubSkillSuggestionsResult result:
                    HandleSubSkillSuggestionsResult(result);
                    return true;
                case SubSkillSuggestionsError error:
                    SendError(error.ErrorMessage);
                    return true;

                // User messages
                case SkillMakerUserMessage msg when msg.MessageType == SkillMakerMessageType.SubSkill
                    || msg.MessageType == SkillMakerMessageType.CustomInput:
                    HandleSubSkillSelection(msg);
                    return true;
                case SkillMakerUserMessage msg when msg.MessageType == SkillMakerMessageType.Category:
                    // Client sent wrong messageType, treat as SubSkill selection
                    _logger.Warning("Received Category message in WaitingForSubSkill state, treating as SubSkill for session {0}", _sessionId);
                    HandleSubSkillSelection(new SkillMakerUserMessage
                    {
                        SessionId = msg.SessionId,
                        Message = msg.Message,
                        MessageType = SkillMakerMessageType.SubSkill
                    });
                    return true;

                case SkillMakerSessionTimeout timeout:
                    HandleSessionTimeout(timeout);
                    return true;
                default:
                    return false;
            }
        };
    }

    private void StartSubSkillSuggestions()
    {
        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Phase,
            Content = $"'{_selectedCategory}' 직군에 맞는 추천 스킬을 생성하고 있습니다..."
        });

        var self = Self;
        var category = _selectedCategory;
        var prompt = SkillMakerPrompts.GetSubSkillSuggestionPrompt(category);

        Task.Run(async () =>
        {
            try
            {
                var response = await _llmExService.CompleteAsync(prompt);
                self.Tell(new SubSkillSuggestionsResult(response));
            }
            catch (Exception ex)
            {
                self.Tell(new SubSkillSuggestionsError($"스킬 추천 생성 중 오류가 발생했습니다: {ex.Message}"));
            }
        });
    }

    private void HandleSubSkillSuggestionsResult(SubSkillSuggestionsResult result)
    {
        var options = ParseSubSkillOptions(result.LlmResponse);
        options.Add("직접입력");

        _conversationEntries.Add(new SkillConversationEntry
        {
            Role = "assistant",
            Content = $"추천 스킬: {string.Join(", ", options)}"
        });

        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Options,
            Content = $"'{_selectedCategory}' 직군에 적합한 스킬을 추천합니다. 원하는 스킬을 선택하거나 직접 입력해주세요.",
            Options = options,
            NextMessageType = "subskill"
        });
    }

    private List<string> ParseSubSkillOptions(string llmResponse)
    {
        var options = new List<string>();
        try
        {
            // Clean response
            var json = llmResponse.Trim();
            if (json.StartsWith("```json")) json = json.Substring(7);
            if (json.StartsWith("```")) json = json.Substring(3);
            if (json.EndsWith("```")) json = json.Substring(0, json.Length - 3);
            json = json.Trim();

            // Extract JSON array
            var arrStart = json.IndexOf('[');
            var arrEnd = json.LastIndexOf(']');
            if (arrStart >= 0 && arrEnd > arrStart)
            {
                json = json.Substring(arrStart, arrEnd - arrStart + 1);
            }

            var skills = JsonSerializer.Deserialize<List<JsonElement>>(json);
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    if (skill.TryGetProperty("name", out var nameProp))
                    {
                        var name = nameProp.GetString();
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            options.Add(name);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse sub-skill JSON response, using fallback options");
        }

        if (options.Count == 0)
        {
            // Fallback options
            options.AddRange(new[]
            {
                "코드 리뷰 자동화",
                "테스트 코드 생성",
                "문서화 자동화",
                "리팩토링 도우미",
                "배포 자동화"
            });
        }

        return options;
    }

    private void HandleSubSkillSelection(SkillMakerUserMessage msg)
    {
        _selectedSkillName = msg.Message;
        _logger.Info("Sub-skill selected for session {0}: {1}", _sessionId, _selectedSkillName);

        _conversationEntries.Add(new SkillConversationEntry
        {
            Role = "user",
            Content = $"스킬 선택: {_selectedSkillName}",
            MessageType = SkillMakerMessageType.SubSkill
        });

        // Transition to GatheringInfo BEFORE starting async work
        Context.Become(GatheringInfo());

        // Start gathering information
        StartFollowUpQuestion();
    }

    // ===== State: GatheringInfo =====

    private Receive GatheringInfo()
    {
        return message =>
        {
            switch (message)
            {
                // Async result from LLM
                case FollowUpQuestionResult result:
                    HandleFollowUpQuestionResult(result);
                    return true;
                case FollowUpQuestionError error:
                    _logger.Warning("Follow-up question error for session {0}: {1}", _sessionId, error.ErrorMessage);
                    // On error, proceed to generation with what we have
                    TransitionToGeneratingSkill();
                    return true;

                // User messages
                case SkillMakerUserMessage msg when msg.MessageType == SkillMakerMessageType.Answer
                    || msg.MessageType == SkillMakerMessageType.CustomInput:
                    HandleAnswer(msg);
                    return true;

                case SkillMakerSessionTimeout timeout:
                    HandleSessionTimeout(timeout);
                    return true;
                default:
                    return false;
            }
        };
    }

    private void HandleAnswer(SkillMakerUserMessage msg)
    {
        _conversationEntries.Add(new SkillConversationEntry
        {
            Role = "user",
            Content = msg.Message,
            MessageType = SkillMakerMessageType.Answer
        });

        _questionCount++;
        ResetSessionTimer();

        // Check if we have enough info or generate next question
        StartFollowUpQuestion();
    }

    private void StartFollowUpQuestion()
    {
        var self = Self;
        var history = BuildConversationHistory();
        var prompt = SkillMakerPrompts.GetFollowUpQuestionPrompt(_selectedCategory, _selectedSkillName, history);

        Task.Run(async () =>
        {
            try
            {
                var response = await _llmExService.CompleteAsync(prompt);
                self.Tell(new FollowUpQuestionResult(response));
            }
            catch (Exception ex)
            {
                self.Tell(new FollowUpQuestionError(ex.Message));
            }
        });
    }

    private void HandleFollowUpQuestionResult(FollowUpQuestionResult result)
    {
        var trimmed = result.LlmResponse.Trim();

        // Check if LLM says READY or we've reached max questions
        if (trimmed.Equals("READY", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("READY")
            || _questionCount >= MaxQuestions)
        {
            _logger.Info("Enough info gathered for session {0}, starting skill generation", _sessionId);

            _conversationEntries.Add(new SkillConversationEntry
            {
                Role = "assistant",
                Content = "충분한 정보가 수집되었습니다. 스킬 생성을 시작합니다."
            });

            TransitionToGeneratingSkill();
        }
        else
        {
            // Try to parse JSON response with question/insight/example
            string question = trimmed;
            string? insight = null;
            string? example = null;

            try
            {
                // Remove code fence if present (```json ... ```)
                var jsonText = trimmed;
                if (jsonText.StartsWith("```json")) jsonText = jsonText.Substring(7);
                if (jsonText.StartsWith("```")) jsonText = jsonText.Substring(3);
                if (jsonText.EndsWith("```")) jsonText = jsonText.Substring(0, jsonText.Length - 3);
                jsonText = jsonText.Trim();

                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                if (root.TryGetProperty("question", out var questionProp))
                {
                    question = questionProp.GetString() ?? trimmed;
                }
                if (root.TryGetProperty("insight", out var insightProp))
                {
                    insight = insightProp.GetString();
                }
                if (root.TryGetProperty("example", out var exampleProp))
                {
                    example = exampleProp.GetString();
                }
            }
            catch (JsonException)
            {
                // Fallback: use raw text as question, insight/example remain null
                _logger.Debug("Follow-up question response is not JSON for session {0}, using as plain text", _sessionId);
            }

            // Store only question text in conversation history (exclude insight/example to save tokens)
            _conversationEntries.Add(new SkillConversationEntry
            {
                Role = "assistant",
                Content = question
            });

            _sseBridge.Tell(new SkillMakerStreamingUpdate
            {
                SessionId = _sessionId,
                UpdateType = SkillMakerUpdateType.Question,
                Content = question,
                Insight = insight,
                Example = example
            });
        }
    }

    private void TransitionToGeneratingSkill()
    {
        // Transition state BEFORE starting async work (Context is available here)
        Context.Become(GeneratingSkill());
        StartSkillGeneration();
    }

    // ===== State: GeneratingSkill =====

    private Receive GeneratingSkill()
    {
        return message =>
        {
            switch (message)
            {
                case SkillChunkResult chunk:
                    _sseBridge.Tell(new SkillMakerStreamingUpdate
                    {
                        SessionId = _sessionId,
                        UpdateType = SkillMakerUpdateType.Chunk,
                        Content = chunk.Chunk
                    });
                    return true;
                case SkillGenerationCompleted completed:
                    HandleSkillGenerationCompleted(completed);
                    return true;
                case SkillGenerationError error:
                    _logger.Error("Skill generation error for session {0}: {1}", _sessionId, error.ErrorMessage);
                    SendError("스킬 생성 중 오류가 발생했습니다. 다시 시도해주세요.");
                    return true;
                case UsageGuideResult guide:
                    SendCompletion(guide.SkillContent, guide.Guide);
                    return true;
                case UsageGuideError fallback:
                    SendCompletion(fallback.SkillContent, null);
                    return true;
                case SkillMakerSessionTimeout timeout:
                    HandleSessionTimeout(timeout);
                    return true;
                default:
                    return false;
            }
        };
    }

    private void StartSkillGeneration()
    {
        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Phase,
            Content = "스킬을 생성하고 있습니다..."
        });

        var self = Self;
        var collectedInfo = BuildCollectedInfo();
        var prompt = SkillMakerPrompts.GetSkillGenerationPrompt(_selectedCategory, _selectedSkillName, collectedInfo);

        Task.Run(async () =>
        {
            try
            {
                var sb = new StringBuilder();
                await foreach (var chunk in _llmExService.CompleteStreamingAsync(prompt))
                {
                    sb.Append(chunk);
                    self.Tell(new SkillChunkResult(chunk));
                }
                self.Tell(new SkillGenerationCompleted(sb.ToString()));
            }
            catch (Exception ex)
            {
                self.Tell(new SkillGenerationError(ex.Message));
            }
        });
    }

    private void HandleSkillGenerationCompleted(SkillGenerationCompleted completed)
    {
        _generatedSkillContent = completed.FullContent.Trim();

        // Clean up: remove markdown wrapping if present
        if (_generatedSkillContent.StartsWith("```markdown"))
        {
            _generatedSkillContent = _generatedSkillContent.Substring(11);
        }
        if (_generatedSkillContent.StartsWith("```"))
        {
            _generatedSkillContent = _generatedSkillContent.Substring(3);
        }
        if (_generatedSkillContent.EndsWith("```"))
        {
            _generatedSkillContent = _generatedSkillContent.Substring(0, _generatedSkillContent.Length - 3);
        }
        _generatedSkillContent = _generatedSkillContent.Trim();

        _logger.Info("Skill generation completed for session {0}, content length: {1}",
            _sessionId, _generatedSkillContent.Length);

        // Generate usage guide via LLM before sending completion
        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Phase,
            Content = "활용 가이드를 생성하고 있습니다..."
        });

        var self = Self;
        var skillContent = _generatedSkillContent;
        var prompt = SkillMakerPrompts.GetUsageGuidePrompt(skillContent);

        Task.Run(async () =>
        {
            try
            {
                var guide = await _llmExService.CompleteAsync(prompt);
                self.Tell(new UsageGuideResult(skillContent, guide.Trim()));
            }
            catch
            {
                self.Tell(new UsageGuideError(skillContent));
            }
        });
    }

    private void SendCompletion(string skillContent, string? usageGuide)
    {
        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Complete,
            Content = "스킬이 생성되었습니다!",
            SkillContent = skillContent,
            UsageGuide = usageGuide,
            IsComplete = true
        });
    }

    // ===== Common methods =====

    private string BuildConversationHistory()
    {
        var sb = new StringBuilder();
        foreach (var entry in _conversationEntries)
        {
            sb.AppendLine($"{entry.Role}: {entry.Content}");
        }
        return sb.ToString();
    }

    private string BuildCollectedInfo()
    {
        var sb = new StringBuilder();
        foreach (var entry in _conversationEntries.Where(e => e.Role == "user"))
        {
            sb.AppendLine($"- {entry.Content}");
        }
        return sb.ToString();
    }

    public string GetGeneratedSkillContent() => _generatedSkillContent;
    public string GetSelectedCategory() => _selectedCategory;
    public string GetSelectedSkillName() => _selectedSkillName;
    public List<SkillConversationEntry> GetConversationEntries() => new(_conversationEntries);

    private void SendError(string message)
    {
        _sseBridge.Tell(new SkillMakerStreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = SkillMakerUpdateType.Error,
            Content = message
        });
    }

    private void ResetSessionTimer()
    {
        Timers.StartSingleTimer(
            SessionTimerKey,
            new SkillMakerSessionTimeout { SessionId = _sessionId },
            SessionTimeout);
    }

    private void HandleSessionTimeout(SkillMakerSessionTimeout message)
    {
        _logger.Info("SkillMaker session {0} timed out", _sessionId);
        _conversationEntries.Clear();
        Context.Stop(Self);
    }

    protected override void PostStop()
    {
        _logger.Info("SkillMakerActor for session {0} stopped", _sessionId);
        base.PostStop();
    }

    public static Props Props(string sessionId, ILlmExService llmExService, IActorRef sseBridge)
    {
        return Akka.Actor.Props.Create(() => new SkillMakerActor(sessionId, llmExService, sseBridge));
    }
}

/// <summary>
/// Bridge actor to forward SkillMaker streaming updates to SSE
/// </summary>
public sealed class SkillMakerSSEBridgeActor : ReceiveActor
{
    private readonly string _sessionId;
    private readonly Func<SkillMakerStreamingUpdate, Task> _forwardUpdate;
    private readonly ILoggingAdapter _logger;

    public SkillMakerSSEBridgeActor(string sessionId, Func<SkillMakerStreamingUpdate, Task> forwardUpdate)
    {
        _sessionId = sessionId;
        _forwardUpdate = forwardUpdate;
        _logger = Context.GetLogger();

        ReceiveAsync<SkillMakerStreamingUpdate>(HandleStreamingUpdate);
    }

    private async Task HandleStreamingUpdate(SkillMakerStreamingUpdate update)
    {
        try
        {
            await _forwardUpdate(update);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error forwarding SkillMaker update for session {0}", _sessionId);
        }
    }

    public static Props Props(string sessionId, Func<SkillMakerStreamingUpdate, Task> forwardUpdate)
    {
        return Akka.Actor.Props.Create(() => new SkillMakerSSEBridgeActor(sessionId, forwardUpdate));
    }
}
