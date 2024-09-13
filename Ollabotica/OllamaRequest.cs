namespace Ollabotica;

/// <summary>
/// These classes represent the request and response models for Ollama API.
/// </summary>
public class OllamaRequest
{
    public string Prompt { get; set; }
    public string Model { get; set; }
}