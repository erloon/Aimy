namespace Aimy.Core.Application.DTOs.KnowledgeBase;

/// <summary>
/// Request model for creating a new note
/// </summary>
public class CreateNoteRequest
{
    /// <summary>
    /// Folder to create the note in
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid FolderId { get; set; }

    /// <summary>
    /// Title of the note
    /// </summary>
    /// <example>Meeting Notes</example>
    public required string Title { get; set; }

    /// <summary>
    /// Markdown content for the note
    /// </summary>
    /// <example># Meeting Notes\n- Item 1\n- Item 2</example>
    public string? Content { get; set; }

    /// <summary>
    /// JSON object containing metadata
    /// </summary>
    /// <example>{"category":"meeting","tags":["project-x"]}</example>
    public string? Metadata { get; set; }
}
