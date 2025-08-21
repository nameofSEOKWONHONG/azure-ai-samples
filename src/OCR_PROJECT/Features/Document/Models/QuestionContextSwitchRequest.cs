namespace Document.Intelligence.Agent.Features.Document.Models;

public sealed record SearchDocumentContextSwitchRequest(
    string PreviousQuestion,
    float[] PreviousQuestionVector,
    string PreviousPlanJson,
    string[] PreviousChunkIds,
    
    string CurrentQuestion,
    float[] CurrentQuestionVector,
    string CurrentPlanJson,
    string[] CurrentChunkIds);