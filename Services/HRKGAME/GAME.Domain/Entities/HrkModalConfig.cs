namespace GAME.Domain.Entities;

public sealed class HrkModalConfig
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Kind { get; set; } = "CONTENT";
    public string Title { get; set; } = "";
    public string? MessageTemplate { get; set; }
    public string ConfirmText { get; set; } = "Xác nhận";
    public string CancelText { get; set; } = "Hủy";
    public string? IconKey { get; set; }
    public string Size { get; set; } = "MD";
    public string Tone { get; set; } = "DEFAULT";
    public bool CloseOnBackdrop { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; } = 1;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}
