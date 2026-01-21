using System;

namespace AIFORBI.Models;

public class AskModel
{
    public string Question { get; set; }
    public string? SessionId { get; set; }
    public bool DrawGraphic { get; set; }
    public string? UserDesireDetection { get; set; }

}
