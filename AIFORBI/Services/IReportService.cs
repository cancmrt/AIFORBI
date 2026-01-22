using AIFORBI.Models;

namespace AIFORBI.Services;

/// <summary>
/// Service interface for AI-powered report generation.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Processes a user question and generates an appropriate response.
    /// </summary>
    /// <param name="askQ">The question and parameters.</param>
    /// <returns>The answer model with response data.</returns>
    AnswerModel AskQuestion(AskModel askQ);
}
