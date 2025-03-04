namespace MyLittleCMS.ApiService.ApiModels;

public record User
{
    public required Guid UserId { get; init; }
    public required string UserName { get; init; }
    public required string Name { get; init; }
    public required bool Active { get; init; }
}