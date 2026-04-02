namespace Prepr.Models;

public record DuplicateBlock(string[] Lines, List<FileLocation> Locations);
