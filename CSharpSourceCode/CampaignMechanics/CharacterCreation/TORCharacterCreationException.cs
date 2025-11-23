using System;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    public class TORCharacterCreationException : Exception
    {
        public TORCharacterCreationException(string message) : base(message)
        {
        }

        public TORCharacterCreationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class TORCCXmlLoadException(string filePath, Exception innerException)
        : TORCharacterCreationException(
            $"Failed to load character creation XML from '{filePath}'. " +
            $"Ensure the file exists, is valid XML, and follows the TOR character creation schema. " +
            $"Error: {innerException?.Message}",
            innerException)
    {
        public string FilePath { get; } = filePath;
    }

    public class TORCCSpecializationStageLoadException(string message, Exception innerException)
        : TORCharacterCreationException(
            $"Failed to initialize specialization stage: {message}. " +
            $"Check that the CharacterCreationManager is properly initialized and the stage XML is valid. " +
            $"Error: {innerException?.Message}",
            innerException)
    {
    }

    public class TORCCReflectionException(string fieldName, Exception innerException)
        : TORCharacterCreationException(
            $"Failed to access CharacterCreationManager field '{fieldName}' via reflection. " +
            $"This may indicate a Bannerlord version incompatibility or the field name has changed. " +
            $"Error: {innerException?.Message}",
            innerException)
    {
        public string FieldName { get; } = fieldName;
    }

    public class TORCCEquipmentUpdateException(string equipmentSetId, Exception innerException)
        : TORCharacterCreationException(
            $"Failed to update equipment with roster '{equipmentSetId}'. " +
            $"Ensure the equipment roster exists in your module's XML files and is properly formatted. " +
            $"Error: {innerException?.Message}",
            innerException)
    {
        public string EquipmentSetId { get; } = equipmentSetId;
    }

    public class TORCCInvalidOptionTypeException(Type expectedType, Type actualType)
        : TORCharacterCreationException(
            $"Expected option data type '{expectedType.Name}', but received '{actualType?.Name ?? "null"}'. " +
            $"This is likely a programming error in the character creation option setup.")
    {
        public Type ExpectedType { get; } = expectedType;
        public Type ActualType { get; } = actualType;
    }
}
