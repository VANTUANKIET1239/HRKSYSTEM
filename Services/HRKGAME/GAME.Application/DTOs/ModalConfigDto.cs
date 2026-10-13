namespace GAME.Application.DTOs;

public sealed record ModalConfigDto(string Code, string Kind, string Title, string? MessageTemplate,
    string ConfirmText, string CancelText, string? IconKey, string Size, string Tone,
    bool CloseOnBackdrop, bool CloseOnEscape, int Revision);
