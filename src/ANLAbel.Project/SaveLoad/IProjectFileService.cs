using ANLAbel.Core.Models;

namespace ANLAbel.Project.SaveLoad;

public interface IProjectFileService
{
    Task SaveAsync(LabelTemplate template, string filePath, CancellationToken cancellationToken = default);
    Task<LabelTemplate> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
