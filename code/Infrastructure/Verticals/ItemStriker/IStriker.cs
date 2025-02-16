using Domain.Enums;

namespace Infrastructure.Verticals.ItemStriker;

public interface IStriker
{
    Task<bool> StrikeAndCheckLimit(string hash, string itemName, ushort maxStrikes, StrikeType strikeType);
}