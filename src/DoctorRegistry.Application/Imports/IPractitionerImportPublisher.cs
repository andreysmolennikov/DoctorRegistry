namespace DoctorRegistry.Application.Imports;

public interface IPractitionerImportPublisher
{
    Task PublishAsync(PractitionerImportMessageV1 message, CancellationToken cancellationToken = default);
}